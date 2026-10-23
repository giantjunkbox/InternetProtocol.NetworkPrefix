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
///     Properties and comparisons, which convert the network to integers on each call. The members that return an
///     <see cref="IPAddress" /> also create one.
/// </summary>
[MemoryDiagnoser]
public class PropertyBenchmarks
{
    private readonly IPNetwork ipv4Network = IPNetwork.Parse("10.20.0.0/16");
    private readonly IPNetwork ipv4Subnet = IPNetwork.Parse("10.20.30.0/24");
    private readonly IPNetwork ipv6Network = IPNetwork.Parse("2001:db8::/32");

    [Benchmark]
    public UInt128 BaseAddressBits_IPv4() => ipv4Network.BaseAddressBits;

    [Benchmark]
    public UInt128 BaseAddressBits_IPv6() => ipv6Network.BaseAddressBits;

    [Benchmark]
    public UInt128 TotalHosts_IPv4() => ipv4Network.TotalHosts;

    [Benchmark]
    public IPAddress NetworkMask_IPv4() => ipv4Network.NetworkMask;

    [Benchmark]
    public IPAddress BroadcastAddress_IPv4() => ipv4Network.BroadcastAddress;

    [Benchmark]
    public int CompareTo_IPv4() => ipv4Network.CompareTo(ipv4Subnet);

    [Benchmark]
    public AddressAllocation Allocation_IPv4() => ipv4Network.Allocation;

    [Benchmark]
    public AddressAllocation Allocation_IPv6() => ipv6Network.Allocation;
}
