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
        ///     IPv6 network to the end is not practical.
        /// </remarks>
        /// <returns>Every address in the network, in ascending order.</returns>
        public IEnumerable<IPAddress> Addresses()
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            return EnumerateAddresses(networkBits, networkBits.BaseAddressBits, networkBits.LastAddressBits);
        }

        /// <summary>
        ///     Computes the set difference of this network and another: the fewest networks that cover every address of this
        ///     network that is not in <paramref name="networkToExclude" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, excluding <c>10.0.0.0/26</c> from <c>10.0.0.0/24</c> leaves <c>10.0.0.128/25</c> and
        ///         <c>10.0.0.64/26</c>.
        ///     </para>
        ///     <para>
        ///         The result is empty when <paramref name="networkToExclude" /> contains or equals this network, and holds just
        ///         this network when the two do not overlap, including when they are different IP versions. Otherwise the result
        ///         is ordered from the largest network to the smallest.
        ///     </para>
        /// </remarks>
        /// <param name="networkToExclude">The network to remove from this one.</param>
        /// <returns>The networks that remain.</returns>
        public IReadOnlyList<IPNetwork> Exclude(IPNetwork networkToExclude)
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            var excludedBits = NetworkBits.FromIPNetwork(networkToExclude);

            // nothing is left when the excluded network covers all of this network
            if (excludedBits.IsSupernetOf(networkBits))
                return [];

            // nothing is removed when the two networks have no address in common
            if (!networkBits.IsSupernetOf(excludedBits))
                return [networkBits.ToIPNetwork()];

            // walk from this network towards the excluded network, one prefix length at a time. At each step the current network
            // is split in half: the half that holds the excluded network is split again on the next step, and the other half,
            // which is the complementary network of the first, lies wholly outside the excluded network and is kept
            var remainingNetworks = new List<IPNetwork>(excludedBits.PrefixLength - networkBits.PrefixLength);
            for (var halfPrefixLength = networkBits.PrefixLength + 1; halfPrefixLength <= excludedBits.PrefixLength; halfPrefixLength++)
            {
                var halfHoldingExcludedNetwork = excludedBits.WithPrefixLength(halfPrefixLength);
                remainingNetworks.Add(halfHoldingExcludedNetwork.ComplementaryNetwork.ToIPNetwork());
            }

            return remainingNetworks;
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
        ///     Returns the network of a shorter prefix length that contains this network. For example, the /16 supernet of
        ///     <c>10.1.2.0/24</c> is <c>10.1.0.0/16</c>.
        /// </summary>
        /// <param name="prefixLength">The prefix length of the supernet, which must be shorter than this network's prefix length.</param>
        /// <returns>The supernet.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="prefixLength" /> is negative, or is not shorter than this network's prefix length.
        /// </exception>
        public IPNetwork GetSupernet(int prefixLength)
        {
            if (prefixLength < 0 || prefixLength >= network.PrefixLength)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a supernet of {network} must be from 0 to {network.PrefixLength - 1}.");

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
        ///     Splits this network into every subnet of a longer prefix length. For example, <c>10.0.0.0/24</c> split to /26 is
        ///     <c>10.0.0.0/26</c>, <c>10.0.0.64/26</c>, <c>10.0.0.128/26</c> and <c>10.0.0.192/26</c>.
        /// </summary>
        /// <remarks>
        ///     The subnets are produced as they are enumerated, so this works however many subnets there are.
        /// </remarks>
        /// <param name="prefixLength">
        ///     The prefix length of the subnets, which must be longer than this network's prefix length, and no longer than 32 for
        ///     IPv4 or 128 for IPv6.
        /// </param>
        /// <returns>Every subnet, in ascending order.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="prefixLength" /> is out of range.</exception>
        public IEnumerable<IPNetwork> Split(int prefixLength)
        {
            var networkBits = NetworkBits.FromIPNetwork(network);

            // validated here rather than inside the iterator, so that an invalid length throws when Split is called instead of
            // later, when the result is first enumerated
            if (prefixLength <= networkBits.PrefixLength || prefixLength > networkBits.AddressBitLength)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a subnet of {network} must be from {networkBits.PrefixLength + 1} to {networkBits.AddressBitLength}.");

            return EnumerateSubnets(networkBits, prefixLength);
        }
    }

    extension(IPNetwork)
    {
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

            // after sorting, a network always comes directly after any network that contains it, and the lower half of a
            // network comes before the upper half
            var sortedNetworks = networks.Select(NetworkBits.FromIPNetwork).ToList();
            sortedNetworks.Sort();

            // the summarized list is kept sorted and free of overlaps as it is built, so its last element is the only one that
            // can contain or pair up with the next network
            var summarizedNetworks = new List<NetworkBits>(sortedNetworks.Count);
            foreach (var network in sortedNetworks)
            {
                // drop a duplicate, or a network that lies inside one that has already been kept
                if (summarizedNetworks.Count > 0 && summarizedNetworks[^1].IsSupernetOf(network))
                    continue;

                summarizedNetworks.Add(network);

                // when the last two networks are the lower and upper halves of the same supernet, replace them with that
                // supernet. The supernet can itself be the upper half of an earlier network, so repeat until no more halves pair up
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

            return summarizedNetworks.Select(summarizedNetwork => summarizedNetwork.ToIPNetwork()).ToArray();
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
    ///     Enumerates the subnets of a network that have a longer prefix length.
    /// </summary>
    /// <param name="network">The network to split.</param>
    /// <param name="subnetPrefixLength">The prefix length of the subnets, which the caller has validated.</param>
    /// <returns>Every subnet, in ascending order.</returns>
    private static IEnumerable<IPNetwork> EnumerateSubnets(NetworkBits network, int subnetPrefixLength)
    {
        // each subnet spans 2^(subnet host bits) addresses. A subnet has at least one more network bit than the network, so it
        // has at most 127 host bits and the span always fits in a UInt128
        var subnetSpan = UInt128.One << (network.AddressBitLength - subnetPrefixLength);

        // the last subnet is the one that holds the last address of the network
        var lastSubnetAddressBits = network.LastAddressBits & AddressBits.GetNetworkMaskBits(network.Version, subnetPrefixLength);

        var currentSubnetAddressBits = network.BaseAddressBits;
        while (true)
        {
            yield return new NetworkBits(network.Version, currentSubnetAddressBits, subnetPrefixLength).ToIPNetwork();

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
        var unusedHighBitCount = AddressBits.IPv6BitLength - first.AddressBitLength;
        var sharedPrefixLength = (int)UInt128.LeadingZeroCount(differingBits) - unusedHighBitCount;

        // the result can't be more specific than either network. This matters when one network contains the other, because the
        // two addresses can then differ in a bit that lies within the host part of the larger network
        var containingPrefixLength = Math.Min(sharedPrefixLength, Math.Min(first.PrefixLength, second.PrefixLength));

        return first.WithPrefixLength(containingPrefixLength);
    }
}
