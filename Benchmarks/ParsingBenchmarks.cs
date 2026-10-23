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
using BenchmarkDotNet.Attributes;
using Bitvantage.InternetProtocol;

namespace Benchmarks;

/// <summary>
///     The strict parsers, measured against the built-in parsers, which accept nonstandard IPv4 forms.
/// </summary>
[MemoryDiagnoser]
public class ParsingBenchmarks
{
    private const string IPv4NetworkText = "192.168.100.0/24";
    private const string IPv6NetworkText = "2001:db8:1234:5678::/64";
    private const string IPv4AddressText = "192.168.100.200";

    private static readonly byte[] IPv4NetworkUtf8 = "192.168.100.0/24"u8.ToArray();

    [Benchmark(Baseline = true)]
    public IPNetwork BuiltInParse_IPv4Network() => IPNetwork.Parse(IPv4NetworkText);

    [Benchmark]
    public IPNetwork ParseStrict_IPv4Network() => IPNetwork.ParseStrict(IPv4NetworkText);

    [Benchmark]
    public IPNetwork ParseStrict_IPv4NetworkUtf8() => IPNetwork.ParseStrict(IPv4NetworkUtf8);

    [Benchmark]
    public IPNetwork ParseStrict_IPv4NetworkWithMask() => IPNetwork.ParseStrict("192.168.100.0 255.255.255.0");

    [Benchmark]
    public IPNetwork BuiltInParse_IPv6Network() => IPNetwork.Parse(IPv6NetworkText);

    [Benchmark]
    public IPNetwork ParseStrict_IPv6Network() => IPNetwork.ParseStrict(IPv6NetworkText);

    [Benchmark]
    public IPAddress BuiltInParse_IPv4Address() => IPAddress.Parse(IPv4AddressText);

    [Benchmark]
    public IPAddress ParseStrict_IPv4Address() => IPAddress.ParseStrict(IPv4AddressText);

    [Benchmark]
    public IReadOnlyList<IPNetwork> ParseRange_IPv4() => IPNetwork.ParseRange("10.0.0.1-10.0.255.254");
}
