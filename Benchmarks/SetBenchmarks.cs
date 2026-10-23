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
///     Operations on sets of networks, which run in time proportional to the number of networks.
/// </summary>
[MemoryDiagnoser]
public class SetBenchmarks
{
    private readonly IPNetwork privateNetwork = IPNetwork.Parse("10.0.0.0/8");

    private IPNetwork[] allocatedSubnets = [];
    private IPNetwork[] hostNetworks = [];
    private IPNetwork[] shuffledSubnets = [];

    [Params(1_000, 100_000)]
    public int NetworkCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // a fixed seed gives every run the same networks
        var random = new Random(20_240_628);

        // random host addresses in 10.0.0.0/8, some of them repeated
        hostNetworks = Enumerable.Range(0, NetworkCount)
            .Select(_ => new IPNetwork(IPAddress.FromAddressBits(0x0A00_0000u + (uint)random.Next(0, 1 << 24), IPVersion.IPv4), 32))
            .ToArray();

        // the first /24s of 10.0.0.0/8 in order, with a gap at the end, so that the first free /24 comes after all of them
        allocatedSubnets = Enumerable.Range(0, Math.Min(NetworkCount, 65_000))
            .Select(subnetIndex => new IPNetwork(IPAddress.FromAddressBits(0x0A00_0000u + ((uint)subnetIndex << 8), IPVersion.IPv4), 24))
            .ToArray();

        // the same /24s in random order, which every set operation has to sort
        shuffledSubnets = allocatedSubnets.ToArray();
        random.Shuffle(shuffledSubnets);
    }

    [Benchmark]
    public IReadOnlyList<IPNetwork> Exclude_HostsFromSlash8() => privateNetwork.Exclude(hostNetworks);

    [Benchmark]
    public IReadOnlyList<IPNetwork> Summarize_Hosts() => IPNetwork.Summarize(hostNetworks);

    [Benchmark]
    public IReadOnlyList<IPNetwork> Summarize_ShuffledSubnets() => IPNetwork.Summarize(shuffledSubnets);

    [Benchmark]
    public IPNetwork AvailableSubnets_FirstFreeSlash24() => privateNetwork.AvailableSubnets(24, shuffledSubnets).First();

    [Benchmark]
    public IPNetwork[] Sort_ShuffledSubnets()
    {
        var sortedSubnets = shuffledSubnets.ToArray();
        Array.Sort(sortedSubnets, IPNetwork.Comparer);

        return sortedSubnets;
    }
}
