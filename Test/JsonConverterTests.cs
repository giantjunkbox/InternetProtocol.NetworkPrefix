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

    [Test]
    public void JsonConverter_01()
    {
        Assert.That(JsonSerializer.Serialize(Network("10.20.30.40"), ConverterOptions), Is.EqualTo("\"10.20.30.40/32\""));
        Assert.That(JsonSerializer.Deserialize<IPNetwork>("\"10.20.30.40/32\"", ConverterOptions), Is.EqualTo(Network("10.20.30.40")));
    }

    [Test]
    public void JsonConverter_02()
    {
        var networks = Networks("10.20.30.40", "5.10.15.20", "2001:db8::/32");
        var json = JsonSerializer.Serialize(networks, ConverterOptions);

        Assert.That(json, Is.EqualTo("""["10.20.30.40/32","5.10.15.20/32","2001:db8::/32"]"""));
        Assert.That(JsonSerializer.Deserialize<List<IPNetwork>>(json, ConverterOptions), Is.EqualTo(networks));
    }

    [Test]
    public void JsonConverter_03()
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
    public void JsonConverter_04()
    {
        // every format that ParseStrict accepts can be read, and the nonstandard IPv4 forms that it rejects are rejected
        Assert.That(JsonSerializer.Deserialize<IPNetwork>("\"10.0.0.0 255.0.0.0\"", ConverterOptions), Is.EqualTo(Network("10.0.0.0/8")));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("\"010.0.0.0/8\"", ConverterOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("\"10.0.0.0/33\"", ConverterOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPNetwork>("42", ConverterOptions));
    }

    [Test]
    public void JsonConverter_05()
    {
        // a nullable network uses the converter for its value, and null stays null
        Assert.That(JsonSerializer.Deserialize<IPNetwork?>("null", ConverterOptions), Is.Null);
        Assert.That(JsonSerializer.Deserialize<IPNetwork?>("\"10.0.0.0/8\"", ConverterOptions), Is.EqualTo(Network("10.0.0.0/8")));
    }

    [Test]
    public void JsonConverter_06()
    {
        // the converter can be applied to a single property instead of being registered
        var route = new Route(Network("10.0.0.0/8"), "192.0.2.1");
        var json = JsonSerializer.Serialize(route);

        Assert.That(json, Is.EqualTo("""{"Destination":"10.0.0.0/8","NextHop":"192.0.2.1"}"""));
        Assert.That(JsonSerializer.Deserialize<Route>(json), Is.EqualTo(route));
    }

    /// <summary>
    ///     A type with a network property that names its own converter.
    /// </summary>
    private sealed record Route([property: JsonConverter(typeof(IPNetworkJsonConverter))] IPNetwork Destination, string NextHop);
}
