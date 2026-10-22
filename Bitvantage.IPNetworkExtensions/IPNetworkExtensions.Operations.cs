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

namespace Bitvantage.InternetProtocol;

public static partial class IPNetworkExtensions
{
    /// <param name="network">The network.</param>
    extension(IPNetwork network)
    {
        /// <summary>
        ///     Enumerates every address in the network, from the network address to the last address.
        /// </summary>
        /// <remarks>
        ///     The addresses are produced as they are enumerated, so this works for networks of any size, but enumerating a large
        ///     IPv6 network to the end is not practical. Use <see cref="GetAddress" /> to go straight to one address.
        /// </remarks>
        /// <returns>Every address in the network, in ascending order.</returns>
        public IEnumerable<IPAddress> Addresses()
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            return EnumerateAddresses(networkBits, networkBits.BaseAddressBits, networkBits.LastAddressBits);
        }

        /// <summary>
        ///     Enumerates the subnets of a given prefix length that are free: inside this network and outside every allocated
        ///     network. For example, the first free /24 in an address plan is <c>plan.AvailableSubnets(24, allocated).First()</c>.
        /// </summary>
        /// <remarks>
        ///     Allocated networks may overlap each other, lie partly or wholly outside this network, or be of the other IP version;
        ///     only the addresses that they share with this network count as allocated. The free space is worked out when this
        ///     method is called, and the subnets are produced as they are enumerated.
        /// </remarks>
        /// <param name="prefixLength">
        ///     The prefix length of the subnets, from this network's prefix length to 32 for IPv4 or 128 for IPv6.
        /// </param>
        /// <param name="allocatedNetworks">The networks that are already in use.</param>
        /// <returns>Every free subnet of the given length, in ascending order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="allocatedNetworks" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="prefixLength" /> is out of range.</exception>
        public IEnumerable<IPNetwork> AvailableSubnets(int prefixLength, IEnumerable<IPNetwork> allocatedNetworks)
        {
            ArgumentNullException.ThrowIfNull(allocatedNetworks);
            var networkBits = NetworkBits.FromIPNetwork(network);

            // validated here rather than inside the iterator, so that an invalid length throws when the method is called
            if (prefixLength < networkBits.PrefixLength || prefixLength > networkBits.AddressBitLength)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a subnet of {network} must be from {networkBits.PrefixLength} to {networkBits.AddressBitLength}.");

            var freeBlocks = Subtract([networkBits], SummarizeBits(allocatedNetworks.Select(NetworkBits.FromIPNetwork)));
            return EnumerateAvailableSubnets(freeBlocks, prefixLength);
        }

        /// <summary>
        ///     Computes the set difference of this network and another: the fewest networks that cover every address of this
        ///     network that is not in <paramref name="networkToExclude" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, excluding <c>10.0.0.0/26</c> from <c>10.0.0.0/24</c> leaves <c>10.0.0.64/26</c> and
        ///         <c>10.0.0.128/25</c>.
        ///     </para>
        ///     <para>
        ///         The result is empty when <paramref name="networkToExclude" /> contains or equals this network, and holds just
        ///         this network when the two do not overlap, including when they are different IP versions. The result is sorted
        ///         in the order of <c>IPNetwork.Comparer</c>.
        ///     </para>
        /// </remarks>
        /// <param name="networkToExclude">The network to remove from this one.</param>
        /// <returns>The networks that remain, sorted.</returns>
        public IReadOnlyList<IPNetwork> Exclude(IPNetwork networkToExclude)
        {
            return ToIPNetworks(Subtract([NetworkBits.FromIPNetwork(network)], [NetworkBits.FromIPNetwork(networkToExclude)]));
        }

        /// <summary>
        ///     Computes the set difference of this network and a set of networks: the fewest networks that cover every address of
        ///     this network that is in none of <paramref name="networksToExclude" />.
        /// </summary>
        /// <remarks>
        ///     The networks to exclude may overlap each other, lie partly or wholly outside this network, or be of the other IP
        ///     version. The result is sorted in the order of <c>IPNetwork.Comparer</c>.
        /// </remarks>
        /// <param name="networksToExclude">The networks to remove from this one.</param>
        /// <returns>The networks that remain, sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="networksToExclude" /> is <see langword="null" />.</exception>
        public IReadOnlyList<IPNetwork> Exclude(IEnumerable<IPNetwork> networksToExclude)
        {
            ArgumentNullException.ThrowIfNull(networksToExclude);

            return ToIPNetworks(Subtract([NetworkBits.FromIPNetwork(network)], SummarizeBits(networksToExclude.Select(NetworkBits.FromIPNetwork))));
        }

        /// <summary>
        ///     Returns the address at an offset from the start of the network. Offset 0 is the network address, and the last offset
        ///     is the last address. For example, offset 10 of <c>10.0.0.0/24</c> is <c>10.0.0.10</c>.
        /// </summary>
        /// <param name="offset">The offset, from 0 to the number of addresses in the network less one.</param>
        /// <returns>The address.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset" /> is past the last address.</exception>
        public IPAddress GetAddress(UInt128 offset)
        {
            var networkBits = NetworkBits.FromIPNetwork(network);

            // the host mask is the offset of the last address
            if (offset > networkBits.HostMaskBits)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"The offset of an address in {network} must be from 0 to {networkBits.HostMaskBits}.");

            return networkBits.ToIPAddress(networkBits.BaseAddressBits + offset);
        }

        /// <summary>
        ///     Returns the offset of an address from the start of the network, the inverse of <see cref="GetAddress" />. For
        ///     example, the offset of <c>10.0.0.10</c> in <c>10.0.0.0/24</c> is 10.
        /// </summary>
        /// <remarks>
        ///     An IPv4-mapped IPv6 address, such as <c>::ffff:10.0.0.10</c>, is not in an IPv4 network; convert it with
        ///     <c>Unmap</c> first.
        /// </remarks>
        /// <param name="address">An address in the network.</param>
        /// <returns>The offset of the address.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is not in the network.</exception>
        public UInt128 GetOffset(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);
            var networkBits = NetworkBits.FromIPNetwork(network);

            if (!AddressMath.TryGetVersion(address.AddressFamily, out var addressVersion) || addressVersion != networkBits.Version)
                throw new ArgumentException($"{address} is not in {network}, which is {networkBits.Version}.", nameof(address));

            var addressBits = AddressMath.FromIPAddress(address);
            if ((addressBits & networkBits.NetworkMaskBits) != networkBits.BaseAddressBits)
                throw new ArgumentException($"{address} is not in {network}.", nameof(address));

            return addressBits - networkBits.BaseAddressBits;
        }

        /// <summary>
        ///     Returns the supernet of this network: the network that is one bit shorter and contains this network and its
        ///     complementary network. For example, the supernet of <c>10.0.1.0/24</c> is <c>10.0.0.0/23</c>.
        /// </summary>
        /// <returns>The supernet.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the prefix length is 0, since such a network has no supernet.</exception>
        public IPNetwork GetSupernet()
        {
            if (network.PrefixLength == 0)
                throw new InvalidOperationException($"{network} spans every address of its version and has no supernet.");

            return network.GetSupernet(network.PrefixLength - 1);
        }

        /// <summary>
        ///     Returns the network of a given prefix length that contains this network. For example, the /16 supernet of
        ///     <c>10.1.2.0/24</c> is <c>10.1.0.0/16</c>. A prefix length equal to this network's returns this network.
        /// </summary>
        /// <param name="prefixLength">The prefix length of the supernet, from 0 to this network's prefix length.</param>
        /// <returns>The supernet.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="prefixLength" /> is negative, or is longer than this network's prefix length.
        /// </exception>
        public IPNetwork GetSupernet(int prefixLength)
        {
            if (prefixLength < 0 || prefixLength > network.PrefixLength)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a supernet of {network} must be from 0 to {network.PrefixLength}.");

            // the shorter prefix length clears the extra network bits
            return NetworkBits.FromIPNetwork(network).WithPrefixLength(prefixLength).ToIPNetwork();
        }

        /// <summary>
        ///     Enumerates every address in the network that can be assigned to a host, from <c>FirstHost</c> to <c>LastHost</c>.
        ///     For IPv4 this leaves out the network and broadcast addresses, and for IPv6 it leaves out the subnet-router anycast
        ///     address.
        /// </summary>
        /// <remarks>
        ///     The addresses are produced as they are enumerated, so this works for networks of any size, but enumerating a large
        ///     IPv6 network to the end is not practical.
        /// </remarks>
        /// <returns>Every host address in the network, in ascending order.</returns>
        public IEnumerable<IPAddress> Hosts()
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            return EnumerateAddresses(networkBits, networkBits.FirstHostBits, networkBits.LastHostBits);
        }

        /// <summary>
        ///     Splits this network into its two halves, the networks one bit longer. For example, <c>10.0.0.0/24</c> splits into
        ///     <c>10.0.0.0/25</c> and <c>10.0.0.128/25</c>.
        /// </summary>
        /// <returns>The two halves, lower half first.</returns>
        /// <exception cref="InvalidOperationException">Thrown when this is a host network, which can't be split.</exception>
        public IEnumerable<IPNetwork> Split()
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            if (networkBits.IsHostNetwork)
                throw new InvalidOperationException($"{network} is a host network and can't be split.");

            return network.Split(networkBits.PrefixLength + 1);
        }

        /// <summary>
        ///     Splits this network into every subnet of a given prefix length. For example, <c>10.0.0.0/24</c> split to /26 is
        ///     <c>10.0.0.0/26</c>, <c>10.0.0.64/26</c>, <c>10.0.0.128/26</c> and <c>10.0.0.192/26</c>. A prefix length equal to this
        ///     network's produces just this network.
        /// </summary>
        /// <remarks>
        ///     The subnets are produced as they are enumerated, so this works however many subnets there are.
        /// </remarks>
        /// <param name="prefixLength">
        ///     The prefix length of the subnets, from this network's prefix length to 32 for IPv4 or 128 for IPv6.
        /// </param>
        /// <returns>Every subnet, in ascending order.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="prefixLength" /> is out of range.</exception>
        public IEnumerable<IPNetwork> Split(int prefixLength)
        {
            var networkBits = NetworkBits.FromIPNetwork(network);

            // validated here rather than inside the iterator, so that an invalid length throws when Split is called instead of
            // later, when the result is first enumerated
            if (prefixLength < networkBits.PrefixLength || prefixLength > networkBits.AddressBitLength)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a subnet of {network} must be from {networkBits.PrefixLength} to {networkBits.AddressBitLength}.");

            return EnumerateSubnets(networkBits, prefixLength).Select(subnet => subnet.ToIPNetwork());
        }
    }

    extension(IPNetwork)
    {
        /// <summary>
        ///     Computes the set difference of two sets of networks: the fewest networks that cover every address in
        ///     <paramref name="networks" /> that is in none of <paramref name="networksToExclude" />.
        /// </summary>
        /// <remarks>
        ///     Either set may hold overlapping networks, and may mix IPv4 and IPv6. The result is sorted in the order of
        ///     <c>IPNetwork.Comparer</c>.
        /// </remarks>
        /// <param name="networks">The networks to remove addresses from.</param>
        /// <param name="networksToExclude">The networks whose addresses are removed.</param>
        /// <returns>The networks that remain, sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
        public static IReadOnlyList<IPNetwork> Exclude(IEnumerable<IPNetwork> networks, IEnumerable<IPNetwork> networksToExclude)
        {
            ArgumentNullException.ThrowIfNull(networks);
            ArgumentNullException.ThrowIfNull(networksToExclude);

            var summarizedNetworks = SummarizeBits(networks.Select(NetworkBits.FromIPNetwork));
            var summarizedExclusions = SummarizeBits(networksToExclude.Select(NetworkBits.FromIPNetwork));

            return ToIPNetworks(Subtract(summarizedNetworks, summarizedExclusions));
        }

        /// <summary>
        ///     Computes the smallest network that contains two networks. The result may also contain addresses that are in
        ///     neither network.
        /// </summary>
        /// <remarks>
        ///     For example, the containing network of <c>10.0.0.0/24</c> and <c>10.0.1.0/24</c> is <c>10.0.0.0/23</c>, and the
        ///     containing network of <c>10.0.0.0/24</c> and <c>10.0.2.0/24</c> is <c>10.0.0.0/22</c>.
        /// </remarks>
        /// <param name="first">The first network.</param>
        /// <param name="second">The second network.</param>
        /// <returns>The smallest network that contains both networks.</returns>
        /// <exception cref="ArgumentException">Thrown when the networks are different IP versions.</exception>
        public static IPNetwork GetContainingNetwork(IPNetwork first, IPNetwork second)
        {
            return GetContainingNetwork(NetworkBits.FromIPNetwork(first), NetworkBits.FromIPNetwork(second)).ToIPNetwork();
        }

        /// <summary>
        ///     Computes the smallest network that contains every one of a set of networks. The result may also contain addresses
        ///     that are in none of them.
        /// </summary>
        /// <param name="networks">The networks, which must all be the same IP version.</param>
        /// <returns>The smallest network that contains every network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="networks" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="networks" /> is empty or mixes IP versions.</exception>
        public static IPNetwork GetContainingNetwork(IEnumerable<IPNetwork> networks)
        {
            ArgumentNullException.ThrowIfNull(networks);

            // grow the containing network one network at a time
            NetworkBits? containingNetwork = null;
            foreach (var network in networks)
            {
                var networkBits = NetworkBits.FromIPNetwork(network);
                containingNetwork = containingNetwork is null ? networkBits : GetContainingNetwork(containingNetwork.Value, networkBits);
            }

            if (containingNetwork is null)
                throw new ArgumentException("At least one network is required.", nameof(networks));

            return containingNetwork.Value.ToIPNetwork();
        }

        /// <summary>
        ///     Summarizes a set of networks into the smallest equivalent set: the fewest networks that cover exactly the same
        ///     addresses.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Duplicates are removed, a network that lies inside another network is dropped, and the two halves of a
        ///         network are merged into that network, repeatedly. For example, <c>10.0.0.0/25</c>, <c>10.0.0.128/26</c> and
        ///         <c>10.0.0.192/26</c> summarize to <c>10.0.0.0/24</c>.
        ///     </para>
        ///     <para>
        ///         The result is sorted in the order of <c>IPNetwork.Comparer</c>, and may mix IPv4 and IPv6 networks.
        ///     </para>
        /// </remarks>
        /// <param name="networks">The networks to summarize.</param>
        /// <returns>The summarized networks, sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="networks" /> is <see langword="null" />.</exception>
        public static IReadOnlyList<IPNetwork> Summarize(IEnumerable<IPNetwork> networks)
        {
            ArgumentNullException.ThrowIfNull(networks);

            return ToIPNetworks(SummarizeBits(networks.Select(NetworkBits.FromIPNetwork)));
        }
    }

    /// <summary>
    ///     Determines whether two networks are the two halves of the same supernet.
    /// </summary>
    /// <param name="lowerHalf">The network that might be the lower half.</param>
    /// <param name="upperHalf">The network that might be the upper half.</param>
    /// <returns><see langword="true" /> when the networks are complementary halves.</returns>
    private static bool AreHalvesOfSameSupernet(NetworkBits lowerHalf, NetworkBits upperHalf)
    {
        // halves have the same version and length; a /0 has no supernet, so it can't be a half
        if (lowerHalf.Version != upperHalf.Version || lowerHalf.PrefixLength != upperHalf.PrefixLength || lowerHalf.PrefixLength == 0)
            return false;

        return lowerHalf.ComplementaryNetwork == upperHalf;
    }

    /// <summary>
    ///     Enumerates the addresses of a network's IP version in a range.
    /// </summary>
    /// <param name="network">The network that the addresses belong to.</param>
    /// <param name="firstAddressBits">The first address in integer form.</param>
    /// <param name="lastAddressBits">The last address in integer form, which must not be lower than the first.</param>
    /// <returns>Every address from the first to the last, inclusive.</returns>
    private static IEnumerable<IPAddress> EnumerateAddresses(NetworkBits network, UInt128 firstAddressBits, UInt128 lastAddressBits)
    {
        var currentAddressBits = firstAddressBits;
        while (true)
        {
            yield return network.ToIPAddress(currentAddressBits);

            // stop on the last address rather than incrementing past it and testing; the last IPv6 address is UInt128.MaxValue,
            // and incrementing that wraps around to zero, which would restart the sequence instead of ending it
            if (currentAddressBits == lastAddressBits)
                yield break;

            currentAddressBits++;
        }
    }

    /// <summary>
    ///     Enumerates the free subnets of a given length in a set of free blocks.
    /// </summary>
    /// <param name="freeBlocks">The free blocks, sorted and free of overlaps.</param>
    /// <param name="prefixLength">The prefix length of the subnets.</param>
    /// <returns>Every subnet of the given length that fits in a free block, in ascending order.</returns>
    private static IEnumerable<IPNetwork> EnumerateAvailableSubnets(List<NetworkBits> freeBlocks, int prefixLength)
    {
        foreach (var freeBlock in freeBlocks)
        {
            // a free block that is smaller than the subnets can't hold one
            if (freeBlock.PrefixLength > prefixLength)
                continue;

            foreach (var subnet in EnumerateSubnets(freeBlock, prefixLength))
                yield return subnet.ToIPNetwork();
        }
    }

    /// <summary>
    ///     Enumerates the subnets of a network that have a given prefix length.
    /// </summary>
    /// <param name="network">The network to split.</param>
    /// <param name="subnetPrefixLength">The prefix length of the subnets, which the caller has validated.</param>
    /// <returns>Every subnet, in ascending order.</returns>
    private static IEnumerable<NetworkBits> EnumerateSubnets(NetworkBits network, int subnetPrefixLength)
    {
        // each subnet spans 2^(subnet host bits) addresses. That is 2^128 only when ::/0 is "split" into itself, which does not fit
        // in a UInt128, but then there is just one subnet and the span is never used
        var subnetHostBitLength = network.AddressBitLength - subnetPrefixLength;
        var subnetSpan = subnetHostBitLength == AddressMath.IPv6BitLength ? UInt128.Zero : UInt128.One << subnetHostBitLength;

        // the last subnet is the one that holds the last address of the network
        var lastSubnetAddressBits = network.LastAddressBits & AddressMath.GetNetworkMaskBits(network.Version, subnetPrefixLength);

        var currentSubnetAddressBits = network.BaseAddressBits;
        while (true)
        {
            yield return new NetworkBits(network.Version, currentSubnetAddressBits, subnetPrefixLength);

            // stop on the last subnet rather than counting subnets; there are 2^128 /128 subnets of ::/0, which is one more
            // than a UInt128 can count
            if (currentSubnetAddressBits == lastSubnetAddressBits)
                yield break;

            currentSubnetAddressBits += subnetSpan;
        }
    }

    /// <summary>
    ///     Computes the smallest network that contains two networks, in integer form.
    /// </summary>
    /// <param name="first">The first network.</param>
    /// <param name="second">The second network.</param>
    /// <returns>The smallest network that contains both networks.</returns>
    /// <exception cref="ArgumentException">Thrown when the networks are different IP versions.</exception>
    private static NetworkBits GetContainingNetwork(NetworkBits first, NetworkBits second)
    {
        if (first.Version != second.Version)
            throw new ArgumentException($"Both networks must be the same IP version, but one is {first.Version} and the other is {second.Version}.", nameof(second));

        // XOR sets a one in every bit where the two base addresses differ, for example
        //   10.20.30.1  00001010.00010100.00011110.00000001
        //   10.20.30.2  00001010.00010100.00011110.00000010
        //   XOR         00000000.00000000.00000000.00000011
        // the leading zeros are the bits the two addresses share, 30 in this example. IPv4 addresses occupy only the low 32 bits
        // of the UInt128, so the 96 unused high bits, which are always zero, are not counted
        var differingBits = first.BaseAddressBits ^ second.BaseAddressBits;
        var unusedHighBitCount = AddressMath.IPv6BitLength - first.AddressBitLength;
        var sharedPrefixLength = (int)UInt128.LeadingZeroCount(differingBits) - unusedHighBitCount;

        // the result can't be more specific than either network. This matters when one network contains the other, because the
        // two addresses can then differ in a bit that lies within the host part of the larger network
        var containingPrefixLength = Math.Min(sharedPrefixLength, Math.Min(first.PrefixLength, second.PrefixLength));

        return first.WithPrefixLength(containingPrefixLength);
    }

    /// <summary>
    ///     Determines whether one network lies wholly before another in sorted order, with no address in common.
    /// </summary>
    /// <param name="earlierNetwork">The network that might come first.</param>
    /// <param name="laterNetwork">The network that might come after it.</param>
    /// <returns><see langword="true" /> when every address of <paramref name="earlierNetwork" /> sorts before <paramref name="laterNetwork" />.</returns>
    private static bool IsWhollyBefore(NetworkBits earlierNetwork, NetworkBits laterNetwork)
    {
        // IPv4 sorts before IPv6, and within a version the earlier network ends before the later one starts
        if (earlierNetwork.Version != laterNetwork.Version)
            return earlierNetwork.Version < laterNetwork.Version;

        return earlierNetwork.LastAddressBits < laterNetwork.BaseAddressBits;
    }

    /// <summary>
    ///     Removes a set of networks from another set of networks.
    /// </summary>
    /// <remarks>
    ///     Both sets must be summarized: sorted, and free of overlaps. The result is summarized too: it is sorted, and no two of
    ///     its networks are halves of the same supernet.
    /// </remarks>
    /// <param name="networks">The summarized networks to remove addresses from.</param>
    /// <param name="exclusions">The summarized networks whose addresses are removed.</param>
    /// <returns>The networks that remain.</returns>
    private static List<NetworkBits> Subtract(List<NetworkBits> networks, List<NetworkBits> exclusions)
    {
        var remainingNetworks = new List<NetworkBits>();

        // both sets are sorted, so a single pass over the exclusions serves every network
        var firstCandidateIndex = 0;
        foreach (var network in networks)
        {
            // skip the exclusions that end before this network starts. They end before every later network too, since the
            // networks are sorted. An exclusion that overlaps this network is kept, because a large exclusion can overlap the
            // next network as well
            while (firstCandidateIndex < exclusions.Count && IsWhollyBefore(exclusions[firstCandidateIndex], network))
                firstCandidateIndex++;

            // the exclusions that overlap this network come next, and end at the first exclusion that starts after it
            var overlapCount = 0;
            while (firstCandidateIndex + overlapCount < exclusions.Count && !IsWhollyBefore(network, exclusions[firstCandidateIndex + overlapCount]))
                overlapCount++;

            SubtractFrom(network, exclusions, firstCandidateIndex, overlapCount, remainingNetworks);
        }

        return remainingNetworks;
    }

    /// <summary>
    ///     Removes the exclusions that overlap one network from it, and adds what remains to a list.
    /// </summary>
    /// <param name="network">The network to remove addresses from.</param>
    /// <param name="exclusions">The summarized exclusions.</param>
    /// <param name="firstExclusionIndex">The index of the first exclusion that overlaps the network.</param>
    /// <param name="exclusionCount">The number of exclusions that overlap the network.</param>
    /// <param name="remainingNetworks">The list to add the remaining networks to, in ascending order.</param>
    private static void SubtractFrom(NetworkBits network, List<NetworkBits> exclusions, int firstExclusionIndex, int exclusionCount, List<NetworkBits> remainingNetworks)
    {
        // a network that no exclusion overlaps remains whole
        if (exclusionCount == 0)
        {
            remainingNetworks.Add(network);
            return;
        }

        // two networks that overlap are nested, so an overlapping exclusion either covers this whole network, in which case
        // nothing remains, or lies inside it. The exclusions do not overlap each other, so one that covers the network is the
        // only overlapping exclusion
        if (exclusions[firstExclusionIndex].IsSupernetOf(network))
            return;

        // every exclusion lies inside the network, so split the network in half, and remove each exclusion from the half that
        // holds it. The exclusions are sorted, so those in the lower half come first. A half that holds no exclusion remains
        // whole, which keeps the result as few networks as possible
        var lowerHalf = network.WithPrefixLength(network.PrefixLength + 1);
        var upperHalf = lowerHalf.ComplementaryNetwork;

        var lowerHalfExclusionCount = 0;
        while (lowerHalfExclusionCount < exclusionCount && exclusions[firstExclusionIndex + lowerHalfExclusionCount].BaseAddressBits <= lowerHalf.LastAddressBits)
            lowerHalfExclusionCount++;

        SubtractFrom(lowerHalf, exclusions, firstExclusionIndex, lowerHalfExclusionCount, remainingNetworks);
        SubtractFrom(upperHalf, exclusions, firstExclusionIndex + lowerHalfExclusionCount, exclusionCount - lowerHalfExclusionCount, remainingNetworks);
    }

    /// <summary>
    ///     Summarizes a set of networks in integer form. See <c>IPNetwork.Summarize</c>.
    /// </summary>
    /// <param name="networks">The networks to summarize.</param>
    /// <returns>The summarized networks: sorted, free of overlaps, and with no two that are halves of the same supernet.</returns>
    private static List<NetworkBits> SummarizeBits(IEnumerable<NetworkBits> networks)
    {
        // after sorting, a network always comes directly after any network that contains it, and the lower half of a network
        // comes before the upper half
        var sortedNetworks = networks.ToList();
        sortedNetworks.Sort();

        // the summarized list is kept sorted and free of overlaps as it is built, so its last element is the only one that can
        // contain or pair up with the next network
        var summarizedNetworks = new List<NetworkBits>(sortedNetworks.Count);
        foreach (var network in sortedNetworks)
        {
            // drop a duplicate, or a network that lies inside one that has already been kept
            if (summarizedNetworks.Count > 0 && summarizedNetworks[^1].IsSupernetOf(network))
                continue;

            summarizedNetworks.Add(network);

            // when the last two networks are the lower and upper halves of the same supernet, replace them with that supernet.
            // The supernet can itself be the upper half of an earlier network, so repeat until no more halves pair up
            while (summarizedNetworks.Count >= 2)
            {
                var upperHalf = summarizedNetworks[^1];
                var lowerHalf = summarizedNetworks[^2];

                if (!AreHalvesOfSameSupernet(lowerHalf, upperHalf))
                    break;

                summarizedNetworks.RemoveRange(summarizedNetworks.Count - 2, 2);
                summarizedNetworks.Add(lowerHalf.WithPrefixLength(lowerHalf.PrefixLength - 1));
            }
        }

        return summarizedNetworks;
    }

    /// <summary>
    ///     Converts networks in integer form to <see cref="IPNetwork" /> values.
    /// </summary>
    /// <param name="networks">The networks.</param>
    /// <returns>The networks, in the same order.</returns>
    private static IPNetwork[] ToIPNetworks(List<NetworkBits> networks)
    {
        var ipNetworks = new IPNetwork[networks.Count];
        for (var networkIndex = 0; networkIndex < networks.Count; networkIndex++)
            ipNetworks[networkIndex] = networks[networkIndex].ToIPNetwork();

        return ipNetworks;
    }
}
