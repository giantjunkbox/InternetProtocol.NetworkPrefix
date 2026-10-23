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
using BenchmarkDotNet.Attributes;
using Bitvantage.InternetProtocol;
using Bitvantage.InternetProtocol.Converters;

namespace Benchmarks;

/// <summary>
///     Serializing and deserializing networks and addresses with the JSON converters.
/// </summary>
[MemoryDiagnoser]
public class JsonBenchmarks
{
    private const int ValueCount = 1_000;

    private static readonly JsonSerializerOptions ConverterOptions = new() { Converters = { new IPNetworkJsonConverter(), new IPAddressJsonConverter() } };

    private IPAddress[] addresses = [];
    private string addressesJson = "";
    private IPNetwork[] networks = [];
    private string networksJson = "";

    [GlobalSetup]
    public void Setup()
    {
        // a fixed seed gives every run the same values; every other value is IPv6
        var random = new Random(20_240_628);

        addresses = Enumerable.Range(0, ValueCount)
            .Select(valueIndex => valueIndex % 2 == 0
                ? IPAddress.FromAddressBits((uint)random.Next(), IPVersion.IPv4)
                : IPAddress.FromAddressBits(new UInt128(0x2001_0db8_0000_0000ul | (uint)random.Next(), (ulong)random.NextInt64()), IPVersion.IPv6))
            .ToArray();

        networks = addresses
            .Select((address, valueIndex) => new IPNetwork(address, valueIndex % 2 == 0 ? 24 : 64))
            .ToArray();

        addressesJson = JsonSerializer.Serialize(addresses, ConverterOptions);
        networksJson = JsonSerializer.Serialize(networks, ConverterOptions);
    }

    [Benchmark]
    public string Serialize_Addresses() => JsonSerializer.Serialize(addresses, ConverterOptions);

    [Benchmark]
    public IPAddress[]? Deserialize_Addresses() => JsonSerializer.Deserialize<IPAddress[]>(addressesJson, ConverterOptions);

    [Benchmark]
    public string Serialize_Networks() => JsonSerializer.Serialize(networks, ConverterOptions);

    [Benchmark]
    public IPNetwork[]? Deserialize_Networks() => JsonSerializer.Deserialize<IPNetwork[]>(networksJson, ConverterOptions);
}
