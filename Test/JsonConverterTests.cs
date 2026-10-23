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

using Bitvantage.InternetProtocol;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bitvantage.InternetProtocol.Converters;
using static Test.TestNetworks;

namespace Test;

public class JsonConverterTests
{
    /// <summary>
    ///     Serializer options with the network converter registered.
    /// </summary>
    private static readonly JsonSerializerOptions ConverterOptions = new() { Converters = { new IPNetworkJsonConverter() } };

    /// <summary>
    ///     Serializer options with the address converter registered.
    /// </summary>
    private static readonly JsonSerializerOptions AddressConverterOptions = new() { Converters = { new IPAddressJsonConverter() } };

    [Test]
    public void AddressJsonConverter_01()
    {
        foreach (var addressText in new[] { "10.20.30.40", "2001:db8::1", "::ffff:10.0.0.1", "fe80::1%3" })
        {
            var json = JsonSerializer.Serialize(Address(addressText), AddressConverterOptions);

            Assert.That(json, Is.EqualTo($"\"{addressText}\""));
            Assert.That(JsonSerializer.Deserialize<IPAddress>(json, AddressConverterOptions), Is.EqualTo(Address(addressText)));
        }
    }

    [Test]
    public void AddressJsonConverter_02()
    {
        // addresses can be the elements of a list and the keys of a dictionary
        var addresses = new List<IPAddress> { Address("10.20.30.40"), Address("2001:db8::1") };
        var addressesJson = JsonSerializer.Serialize(addresses, AddressConverterOptions);

        Assert.That(addressesJson, Is.EqualTo("""["10.20.30.40","2001:db8::1"]"""));
        Assert.That(JsonSerializer.Deserialize<List<IPAddress>>(addressesJson, AddressConverterOptions), Is.EqualTo(addresses));

        var namesByAddress = new Dictionary<IPAddress, string>
        {
            [Address("192.0.2.1")] = "gateway",
            [Address("fe80::1%3")] = "link"
        };
        var namesJson = JsonSerializer.Serialize(namesByAddress, AddressConverterOptions);

        Assert.That(namesJson, Is.EqualTo("""{"192.0.2.1":"gateway","fe80::1%3":"link"}"""));
        Assert.That(JsonSerializer.Deserialize<Dictionary<IPAddress, string>>(namesJson, AddressConverterOptions), Is.EqualTo(namesByAddress));
    }

    [Test]
    public void AddressJsonConverter_03()
    {
        // the nonstandard IPv4 forms that ParseStrict rejects are rejected, and so is anything that is not a string
        foreach (var json in new[] { "\"010.0.0.1\"", "\"10.1\"", "\"167772161\"", "\"10.0.0.0/8\"", "\"\"", "42", "true", "{}" })
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPAddress>(json, AddressConverterOptions), json);

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<IPAddress, int>>("""{"010.0.0.1":1}""", AddressConverterOptions));
    }

    [Test]
    public void AddressJsonConverter_04()
    {
        // null stays null in both directions
        Assert.That(JsonSerializer.Deserialize<IPAddress>("null", AddressConverterOptions), Is.Null);
        Assert.That(JsonSerializer.Serialize<IPAddress?>(null, AddressConverterOptions), Is.EqualTo("null"));
    }

    [Test]
    public void AddressJsonConverter_05()
    {
        // a value with escape sequences is read through a string rather than from its raw bytes
        Assert.That(JsonSerializer.Deserialize<IPAddress>("\"\\u0031\\u0030.0.0.1\"", AddressConverterOptions), Is.EqualTo(Address("10.0.0.1")));
        Assert.That(JsonSerializer.Deserialize<Dictionary<IPAddress, int>>("{\"\\u0031\\u0030.0.0.1\":1}", AddressConverterOptions)?.Keys.Single(), Is.EqualTo(Address("10.0.0.1")));
    }

    [Test]
    public void AddressJsonConverter_06()
    {
        // the converter can be applied to a single property instead of being registered, and works beside the network converter
        var host = new Host(Address("10.0.0.5"), Network("10.0.0.0/24"));
        var json = JsonSerializer.Serialize(host);

        Assert.That(json, Is.EqualTo("""{"Address":"10.0.0.5","Network":"10.0.0.0/24"}"""));
        Assert.That(JsonSerializer.Deserialize<Host>(json), Is.EqualTo(host));
    }

    [Test]
    public void NetworkJsonConverter_01()
    {
        Assert.That(JsonSerializer.Serialize(Network("10.20.30.40"), ConverterOptions), Is.EqualTo("\"10.20.30.40/32\""));
        Assert.That(JsonSerializer.Deserialize<IPNetwork>("\"10.20.30.40/32\"", ConverterOptions), Is.EqualTo(Network("10.20.30.40")));
    }

    [Test]
    public void NetworkJsonConverter_02()
    {
        var networks = Networks("10.20.30.40", "5.10.15.20", "2001:db8::/32");
        var json = JsonSerializer.Serialize(networks, ConverterOptions);

        Assert.That(json, Is.EqualTo("""["10.20.30.40/32","5.10.15.20/32","2001:db8::/32"]"""));
        Assert.That(JsonSerializer.Deserialize<List<IPNetwork>>(json, ConverterOptions), Is.EqualTo(networks));
    }

    [Test]
    public void NetworkJsonConverter_03()
    {
        // networks can be the keys of a dictionary
        var namesByNetwork = new Dictionary<IPNetwork, string>
        {
            [Network("10.0.0.0/8")] = "private",
            [Network("2001:db8::/32")] = "documentation"
        };

        var json = JsonSerializer.Serialize(namesByNetwork, ConverterOptions);

        Assert.That(json, Is.EqualTo("""{"10.0.0.0/8":"private","2001:db8::/32":"documentation"}"""));
        Assert.That(JsonSerializer.Deserialize<Dictionary<IPNetwork, string>>(json, ConverterOptions), Is.EqualTo(namesByNetwork));
    }

    [Test]
    public void NetworkJsonConverter_04()
    {
        // every format that ParseStrict accepts can be read, and the nonstandard IPv4 forms that it rejects are rejected
        Assert.That(JsonSerializer.Deserialize<IPNetwork>("\"10.0.0.0 255.0.0.0\"", ConverterOptions), Is.EqualTo(Network("10.0.0.0/8")));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("\"010.0.0.0/8\"", ConverterOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("\"10.0.0.0/33\"", ConverterOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("42", ConverterOptions));
    }

    [Test]
    public void NetworkJsonConverter_05()
    {
        // a nullable network uses the converter for its value, and null stays null
        Assert.That(JsonSerializer.Deserialize<IPNetwork?>("null", ConverterOptions), Is.Null);
        Assert.That(JsonSerializer.Deserialize<IPNetwork?>("\"10.0.0.0/8\"", ConverterOptions), Is.EqualTo(Network("10.0.0.0/8")));
    }

    [Test]
    public void NetworkJsonConverter_06()
    {
        // the converter can be applied to a single property instead of being registered
        var route = new Route(Network("10.0.0.0/8"), "192.0.2.1");
        var json = JsonSerializer.Serialize(route);

        Assert.That(json, Is.EqualTo("""{"Destination":"10.0.0.0/8","NextHop":"192.0.2.1"}"""));
        Assert.That(JsonSerializer.Deserialize<Route>(json), Is.EqualTo(route));
    }

    [Test]
    public void NetworkJsonConverter_07()
    {
        // a value with escape sequences is read through a string rather than from its raw bytes
        Assert.That(JsonSerializer.Deserialize<IPNetwork>("\"\\u0031\\u0030.0.0.0/8\"", ConverterOptions), Is.EqualTo(Network("10.0.0.0/8")));
        Assert.That(JsonSerializer.Deserialize<Dictionary<IPNetwork, int>>("{\"\\u0031\\u0030.0.0.0/8\":1}", ConverterOptions)?.Keys.Single(), Is.EqualTo(Network("10.0.0.0/8")));
    }

    /// <summary>
    ///     A type with a network property that names its own converter.
    /// </summary>
    private sealed record Route([property: JsonConverter(typeof(IPNetworkJsonConverter))] IPNetwork Destination, string NextHop);

    /// <summary>
    ///     A type with an address property and a network property that name their own converters.
    /// </summary>
    private sealed record Host(
        [property: JsonConverter(typeof(IPAddressJsonConverter))] IPAddress Address,
        [property: JsonConverter(typeof(IPNetworkJsonConverter))] IPNetwork Network);

    [Test]
    public void NetworkJsonConverter_08()
    {
        // networks are written straight to UTF-8, in the same text as ToString
        foreach (var network in Networks("0.0.0.0/0", "10.0.0.0/8", "255.255.255.255", "::/0", "::ffff:10.0.0.0/104", "2001:db8:1234:5678:9abc:def0:1234:5678", "ffff:ffff:ffff:ffff:ffff:ffff:255.255.255.255/128").Append(default))
            Assert.That(JsonSerializer.Serialize(network, ConverterOptions), Is.EqualTo($"\"{network.ToString(NetworkFormat.AddressAndPrefix)}\""), network.ToString());

        // a scope ID is left out, as ToString and ParseStrict leave it out
        var scopedNetwork = new IPNetwork(Address("fe80::%3"), 64);
        Assert.That(JsonSerializer.Serialize(scopedNetwork, ConverterOptions), Is.EqualTo("\"fe80::/64\""));
        Assert.That(JsonSerializer.Serialize(new Dictionary<IPNetwork, int> { [scopedNetwork] = 1 }, ConverterOptions), Is.EqualTo("""{"fe80::/64":1}"""));
    }
}
