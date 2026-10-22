/*
   Bitvantage.InternetProtocol.Network
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

using System.Net;
using System.Numerics;
using System.Text.Json;
using Bitvantage.InternetProtocol;
using Bitvantage.InternetProtocol.Converters;

namespace Test;

public class IPNetworkExtensionsTests
{
    [Test]
    public void Addresses_IPv4_01()
    {
        var addresses = IPNetwork.Parse("10.0.0.0/30").Addresses().ToList();

        var expectedResult = Enumerable
            .Range(0, 4)
            .Select(lastOctet => IPAddress.Parse($"10.0.0.{lastOctet}"))
            .ToList();

        Assert.That(addresses, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Classification_IPv4_01()
    {
        Assert.That(IPNetwork.Parse("10.20.0.0/16").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(IPNetwork.Parse("10.20.0.0/16").Class, Is.EqualTo(NetworkClass.A));
    }

    [Test]
    public void Comparer_IPv4_01()
    {
        // IPNetwork does not implement IComparable, so the comparer is what makes a list of networks sortable
        var networks = new[]
        {
            IPNetwork.Parse("2001:db8::/32"),
            IPNetwork.Parse("10.0.1.0/24"),
            IPNetwork.Parse("10.0.0.0/24"),
            IPNetwork.Parse("10.0.0.0/16")
        };

        var expectedResult = new[]
        {
            IPNetwork.Parse("10.0.0.0/16"),
            IPNetwork.Parse("10.0.0.0/24"),
            IPNetwork.Parse("10.0.1.0/24"),
            IPNetwork.Parse("2001:db8::/32")
        };

        Assert.That(networks.Order(IPNetwork.Comparer), Is.EqualTo(expectedResult));
    }

    [Test]
    public void ComplementaryPrefix_IPv4_01()
    {
        Assert.That(IPNetwork.Parse("10.1.2.0/25").ComplementaryPrefix, Is.EqualTo(IPNetwork.Parse("10.1.2.128/25")));
    }

    [Test]
    public void Contains_IPv4_01()
    {
        var network = IPNetwork.Parse("10.20.0.0/16");
        var subnet = IPNetwork.Parse("10.20.30.0/24");

        Assert.That(network.Contains(subnet), Is.True);
        Assert.That(network.Contains(network), Is.False);
        Assert.That(network.ContainsOrEqual(network), Is.True);
        Assert.That(subnet.ContainedBy(network), Is.True);
        Assert.That(subnet.ContainedByOrEqual(subnet), Is.True);
        Assert.That(network.Overlaps(subnet), Is.True);
        Assert.That(network.Overlaps(IPNetwork.Parse("10.21.0.0/16")), Is.False);

        // the built-in Contains for an address is still the one that is called
        Assert.That(network.Contains(IPAddress.Parse("10.20.1.1")), Is.True);
    }

    [Test]
    public void Exclude_IPv4_01()
    {
        var remainingNetworks = IPNetwork.Parse("10.0.0.0/24").Exclude(IPNetwork.Parse("10.0.0.0/26"));

        Assert.That(remainingNetworks, Is.EqualTo(new[] { IPNetwork.Parse("10.0.0.128/25"), IPNetwork.Parse("10.0.0.64/26") }));
    }

    [Test]
    public void GetContainingNetwork_IPv4_01()
    {
        Assert.That(IPNetwork.GetContainingNetwork(IPNetwork.Parse("10.0.0.0/24"), IPNetwork.Parse("10.0.1.0/24")), Is.EqualTo(IPNetwork.Parse("10.0.0.0/23")));

        var networks = new[] { IPNetwork.Parse("10.0.0.0/24"), IPNetwork.Parse("10.0.3.0/24") };
        Assert.That(IPNetwork.GetContainingNetwork(networks), Is.EqualTo(IPNetwork.Parse("10.0.0.0/22")));
    }

    [Test]
    public void GetSupernet_IPv4_01()
    {
        var network = IPNetwork.Parse("10.0.1.0/24");

        Assert.That(network.GetSupernet(), Is.EqualTo(IPNetwork.Parse("10.0.0.0/23")));
        Assert.That(network.GetSupernet(8), Is.EqualTo(IPNetwork.Parse("10.0.0.0/8")));
    }

    [Test]
    public void Hosts_IPv4_01()
    {
        var hosts = IPNetwork.Parse("10.0.0.0/30").Hosts().ToList();

        Assert.That(hosts, Is.EqualTo(new[] { IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.2") }));
    }

    [Test]
    public void JsonConverter_IPv4_01()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IPNetworkJsonConverter());

        var networks = new List<IPNetwork> { IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("2001:db8::/32") };
        var json = JsonSerializer.Serialize(networks, options);

        Assert.That(json, Is.EqualTo("""["10.0.0.0/8","2001:db8::/32"]"""));
        Assert.That(JsonSerializer.Deserialize<List<IPNetwork>>(json, options), Is.EqualTo(networks));
    }

    [Test]
    public void JsonConverter_IPv4_02()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IPNetworkJsonConverter());

        // networks can be the keys of a dictionary, and every format that NetworkPrefix parses is accepted
        var namesByNetwork = new Dictionary<IPNetwork, string> { [IPNetwork.Parse("10.0.0.0/8")] = "private" };
        var json = JsonSerializer.Serialize(namesByNetwork, options);

        Assert.That(json, Is.EqualTo("""{"10.0.0.0/8":"private"}"""));
        Assert.That(JsonSerializer.Deserialize<Dictionary<IPNetwork, string>>(json, options), Is.EqualTo(namesByNetwork));
        Assert.That(JsonSerializer.Deserialize<IPNetwork>("\"10.0.0.0 255.0.0.0\"", options), Is.EqualTo(IPNetwork.Parse("10.0.0.0/8")));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("\"10.0.0.0/33\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("42", options));
    }

    [Test]
    public void Properties_IPv4_01()
    {
        var network = IPNetwork.Parse("10.20.30.0/24");

        Assert.That(network.Version, Is.EqualTo(IPVersion.IPv4));
        Assert.That(network.AddressBitLength, Is.EqualTo(32));
        Assert.That(network.HostBitLength, Is.EqualTo(8));
        Assert.That(network.IsHostPrefix, Is.False);
        Assert.That(network.NetworkMask, Is.EqualTo(IPAddress.Parse("255.255.255.0")));
        Assert.That(network.HostMask, Is.EqualTo(IPAddress.Parse("0.0.0.255")));
        Assert.That(network.BroadcastAddress, Is.EqualTo(IPAddress.Parse("10.20.30.255")));
        Assert.That(network.LastAddress, Is.EqualTo(IPAddress.Parse("10.20.30.255")));
        Assert.That(network.FirstHost, Is.EqualTo(IPAddress.Parse("10.20.30.1")));
        Assert.That(network.LastHost, Is.EqualTo(IPAddress.Parse("10.20.30.254")));
        Assert.That(network.TotalAddresses, Is.EqualTo(new BigInteger(256)));
        Assert.That(network.TotalHosts, Is.EqualTo((UInt128)254));
    }

    [Test]
    public void Properties_IPv6_01()
    {
        var network = IPNetwork.Parse("2001:db8::/127");

        Assert.That(network.Version, Is.EqualTo(IPVersion.IPv6));
        Assert.That(network.AddressBitLength, Is.EqualTo(128));
        Assert.That(network.FirstHost, Is.EqualTo(IPAddress.Parse("2001:db8::")));
        Assert.That(network.LastHost, Is.EqualTo(IPAddress.Parse("2001:db8::1")));
        Assert.That(IPNetwork.Parse("2001:db8::1/128").IsHostPrefix, Is.True);
    }

    [Test]
    public void Split_IPv4_01()
    {
        var network = IPNetwork.Parse("10.0.0.0/24");

        Assert.That(network.Split(), Is.EqualTo(new[] { IPNetwork.Parse("10.0.0.0/25"), IPNetwork.Parse("10.0.0.128/25") }));
        Assert.That(network.Split(26).Count(), Is.EqualTo(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(24));
    }

    [Test]
    public void Summarize_IPv4_01()
    {
        var networks = new[] { IPNetwork.Parse("10.0.0.128/25"), IPNetwork.Parse("10.0.0.0/25"), IPNetwork.Parse("10.0.0.7/32") };

        Assert.That(IPNetwork.Summarize(networks), Is.EqualTo(new[] { IPNetwork.Parse("10.0.0.0/24") }));
    }

    [Test]
    public void ToNetworkPrefix_IPv4_01()
    {
        var network = IPNetwork.Parse("10.0.0.0/8");

        Assert.That(network.ToNetworkPrefix(), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/8")));
        Assert.That(network.ToString(NetworkFormat.AddressAndMask), Is.EqualTo("10.0.0.0 255.0.0.0"));
    }
}
