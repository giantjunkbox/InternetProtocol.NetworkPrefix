/*
   Bitvantage.IPNetworkExtensions
   Copyright (C) 2024 Michael Crino
   
   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.
   
   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.
   
   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.ComponentModel;
using System.Net;
using System.Text;
using Bitvantage.InternetProtocol.Converters;
using Microsoft.Extensions.Configuration;
using static Test.TestNetworks;

namespace Test;

public class TypeConverterTests
{
    [OneTimeSetUp]
    public void RegisterConverters()
    {
        // registration is for the whole process, and a second call has no effect
        IPNetworkTypeConverter.Register();
        IPNetworkTypeConverter.Register();
        IPAddressTypeConverter.Register();
    }

    [Test]
    public void ConfigurationBinding_01()
    {
        var configuration = BuildConfiguration("""{"Trusted":"10.0.0.0/8","Gateway":"10.0.0.1","Known":["192.168.0.0/16","2001:db8::/32"],"Proxies":["10.1.1.1","::1"]}""");
        var options = configuration.Get<FirewallOptions>();

        Assert.That(options?.Trusted, Is.EqualTo(Network("10.0.0.0/8")));
        Assert.That(options?.Gateway, Is.EqualTo(Address("10.0.0.1")));
        Assert.That(options?.Known, Is.EqualTo(Networks("192.168.0.0/16", "2001:db8::/32")));
        Assert.That(options?.Proxies, Is.EqualTo(new[] { Address("10.1.1.1"), Address("::1") }));
    }

    [Test]
    public void ConfigurationBinding_02()
    {
        // invalid text, including the nonstandard IPv4 forms that ParseStrict rejects, is an error rather than a silent default
        foreach (var json in new[] { """{"Trusted":"010.0.0.0/8"}""", """{"Trusted":"10.0.0.0/33"}""", """{"Gateway":"10.1"}""" })
        {
            var configuration = BuildConfiguration(json);
            Assert.Throws<InvalidOperationException>(() => configuration.Get<FirewallOptions>(), json);
        }
    }

    [Test]
    public void ConfigurationBinding_03()
    {
        // the binder skips a list element that it can't convert, without an error, which the README warns about
        var options = BuildConfiguration("""{"Known":["10.0.0.0/8","010.0.0.0/8"],"Proxies":["10.1"]}""").Get<FirewallOptions>();

        Assert.That(options?.Known, Is.EqualTo(Networks("10.0.0.0/8")));
        Assert.That(options?.Proxies, Is.Empty);
    }

    [Test]
    public void TypeDescriptor_01()
    {
        var networkConverter = TypeDescriptor.GetConverter(typeof(IPNetwork));
        var addressConverter = TypeDescriptor.GetConverter(typeof(IPAddress));

        Assert.That(networkConverter, Is.InstanceOf<IPNetworkTypeConverter>());
        Assert.That(addressConverter, Is.InstanceOf<IPAddressTypeConverter>());

        Assert.That(networkConverter.ConvertFromInvariantString("10.0.0.5 255.255.255.0"), Is.EqualTo(Network("10.0.0.0/24")));
        Assert.That(networkConverter.ConvertToInvariantString(Network("2001:db8::/32")), Is.EqualTo("2001:db8::/32"));
        Assert.That(networkConverter.IsValid("10.0.0.0/8"), Is.True);
        Assert.That(networkConverter.IsValid("010.0.0.0/8"), Is.False);

        Assert.That(addressConverter.ConvertFromInvariantString("fe80::1%3"), Is.EqualTo(Address("fe80::1%3")));
        Assert.That(addressConverter.ConvertToInvariantString(Address("10.0.0.1")), Is.EqualTo("10.0.0.1"));
        Assert.That(addressConverter.IsValid("10.0.0.1"), Is.True);
        Assert.That(addressConverter.IsValid("10.1"), Is.False);
    }

    /// <summary>
    ///     Builds configuration from JSON text.
    /// </summary>
    private static IConfiguration BuildConfiguration(string json)
    {
        return new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();
    }

    /// <summary>
    ///     Options of the kind an application binds from configuration.
    /// </summary>
    private sealed class FirewallOptions
    {
        public IPNetwork Trusted { get; set; }
        public IPAddress? Gateway { get; set; }
        public List<IPNetwork> Known { get; set; } = [];
        public List<IPAddress> Proxies { get; set; } = [];
    }
}
