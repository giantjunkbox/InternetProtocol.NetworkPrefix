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
///     Subnet, supernet and overlap tests, measured against the built-in test of whether a network contains an address.
/// </summary>
[MemoryDiagnoser]
public class ContainmentBenchmarks
{
    private readonly IPAddress ipv4Address = IPAddress.Parse("10.20.30.40");
    private readonly IPNetwork ipv4Network = IPNetwork.Parse("10.20.0.0/16");
    private readonly IPNetwork ipv4Subnet = IPNetwork.Parse("10.20.30.0/24");
    private readonly IPNetwork ipv6Network = IPNetwork.Parse("2001:db8::/32");
    private readonly IPNetwork ipv6Subnet = IPNetwork.Parse("2001:db8:1::/48");

    [Benchmark(Baseline = true)]
    public bool BuiltInContainsAddress_IPv4() => ipv4Network.Contains(ipv4Address);

    [Benchmark]
    public bool IsSupernetOf_IPv4() => ipv4Network.IsSupernetOf(ipv4Subnet);

    [Benchmark]
    public bool IsSupernetOf_IPv6() => ipv6Network.IsSupernetOf(ipv6Subnet);

    [Benchmark]
    public bool IsProperSubnetOf_IPv4() => ipv4Subnet.IsProperSubnetOf(ipv4Network);

    [Benchmark]
    public bool Overlaps_IPv4() => ipv4Subnet.Overlaps(ipv4Network);

    [Benchmark]
    public bool Overlaps_IPv6() => ipv6Subnet.Overlaps(ipv6Network);
}
