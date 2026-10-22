/*
   Bitvantage.InternetProtocol.NetworkPrefix
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

public sealed partial class NetworkPrefix
{
    /// <summary>
    ///     Enumerates every address in the network, from the network address to the last address.
    /// </summary>
    /// <remarks>
    ///     The addresses are produced as they are enumerated, so this works for networks of any size, but enumerating a large IPv6
    ///     network to the end is not practical.
    /// </remarks>
    /// <returns>Every address in the network, in ascending order.</returns>
    public IEnumerable<IPAddress> Addresses()
    {
        return EnumerateAddresses(NetworkAddressBits, LastAddressBits);
    }

    /// <summary>
    ///     Computes the set difference of this prefix and another: the smallest list of prefixes that covers every address of this
    ///     prefix that is not in <paramref name="networkToExclude" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For example, excluding <c>10.0.0.0/26</c> from <c>10.0.0.0/24</c> leaves <c>10.0.0.128/25</c> and <c>10.0.0.64/26</c>.
    ///     </para>
    ///     <para>
    ///         The result is empty when <paramref name="networkToExclude" /> contains or equals this prefix, and holds just this
    ///         prefix when the two do not overlap, including when they are different IP versions. Otherwise the result is ordered
    ///         from the largest network to the smallest.
    ///     </para>
    /// </remarks>
    /// <param name="networkToExclude">The prefix to remove from this one.</param>
    /// <returns>The prefixes that remain.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="networkToExclude" /> is <see langword="null" />.</exception>
    public IReadOnlyList<NetworkPrefix> Exclude(NetworkPrefix networkToExclude)
    {
        ArgumentNullException.ThrowIfNull(networkToExclude);

        // nothing is left when the excluded network covers all of this network
        if (networkToExclude.ContainsOrEqual(this))
            return [];

        // nothing is removed when the two networks have no address in common
        if (!Contains(networkToExclude))
            return [this];

        // walk from this network towards the excluded network, one prefix length at a time. At each step the current network is
        // split in half: the half that holds the excluded network is split again on the next step, and the other half, which is
        // the complementary prefix of the first, lies wholly outside the excluded network and is kept
        var remainingNetworks = new List<NetworkPrefix>(networkToExclude.PrefixLength - PrefixLength);
        for (var halfPrefixLength = PrefixLength + 1; halfPrefixLength <= networkToExclude.PrefixLength; halfPrefixLength++)
        {
            var halfHoldingExcludedNetwork = new NetworkPrefix(networkToExclude.NetworkAddressBits, halfPrefixLength, Version);
            remainingNetworks.Add(halfHoldingExcludedNetwork.ComplementaryPrefix);
        }

        return remainingNetworks;
    }

    /// <summary>
    ///     Computes the smallest prefix that contains both this prefix and another. See
    ///     <see cref="GetContainingNetwork(NetworkPrefix, NetworkPrefix)" />.
    /// </summary>
    /// <param name="other">The other prefix.</param>
    /// <returns>The smallest prefix that contains both prefixes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the prefixes are different IP versions.</exception>
    public NetworkPrefix GetContainingNetwork(NetworkPrefix other)
    {
        return GetContainingNetwork(this, other);
    }

    /// <summary>
    ///     Computes the smallest prefix that contains two prefixes. The result may also contain addresses that are in neither prefix.
    /// </summary>
    /// <remarks>
    ///     For example, the containing network of <c>10.0.0.0/24</c> and <c>10.0.1.0/24</c> is <c>10.0.0.0/23</c>, and the
    ///     containing network of <c>10.0.0.0/24</c> and <c>10.0.2.0/24</c> is <c>10.0.0.0/22</c>.
    /// </remarks>
    /// <param name="first">The first prefix.</param>
    /// <param name="second">The second prefix.</param>
    /// <returns>The smallest prefix that contains both prefixes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either prefix is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the prefixes are different IP versions.</exception>
    public static NetworkPrefix GetContainingNetwork(NetworkPrefix first, NetworkPrefix second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Version != second.Version)
            throw new ArgumentException($"Both prefixes must be the same IP version, but {first} is {first.Version} and {second} is {second.Version}.", nameof(second));

        // XOR sets a one in every bit where the two network addresses differ, for example
        //   10.20.30.1  00001010.00010100.00011110.00000001
        //   10.20.30.2  00001010.00010100.00011110.00000010
        //   XOR         00000000.00000000.00000000.00000011
        // the leading zeros are the bits the two addresses share, 30 in this example. IPv4 addresses occupy only the low 32 bits
        // of the UInt128, so the 96 unused high bits, which are always zero, are not counted
        var differingBits = first.NetworkAddressBits ^ second.NetworkAddressBits;
        var unusedHighBitCount = AddressBits.IPv6BitLength - first.AddressBitLength;
        var sharedPrefixLength = (int)UInt128.LeadingZeroCount(differingBits) - unusedHighBitCount;

        // the result can't be more specific than either network. This matters when one network contains the other, because the
        // two addresses can then differ in a bit that lies within the host part of the larger network
        var containingPrefixLength = Math.Min(sharedPrefixLength, Math.Min(first.PrefixLength, second.PrefixLength));

        return new NetworkPrefix(first.NetworkAddressBits, containingPrefixLength, first.Version);
    }

    /// <summary>
    ///     Computes the smallest prefix that contains every one of a set of prefixes. The result may also contain addresses that
    ///     are in none of them.
    /// </summary>
    /// <param name="networks">The prefixes, which must all be the same IP version.</param>
    /// <returns>The smallest prefix that contains every prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="networks" /> or one of its elements is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="networks" /> is empty or mixes IP versions.</exception>
    public static NetworkPrefix GetContainingNetwork(IEnumerable<NetworkPrefix> networks)
    {
        ArgumentNullException.ThrowIfNull(networks);

        // grow the containing network one network at a time
        NetworkPrefix? containingNetwork = null;
        foreach (var network in networks)
        {
            ArgumentNullException.ThrowIfNull(network, nameof(networks));

            containingNetwork = containingNetwork is null ? network : GetContainingNetwork(containingNetwork, network);
        }

        return containingNetwork ?? throw new ArgumentException("At least one prefix is required.", nameof(networks));
    }

    /// <summary>
    ///     Returns the supernet of this prefix: the prefix that is one bit shorter and contains this prefix and its
    ///     <see cref="ComplementaryPrefix" />. For example, the supernet of <c>10.0.1.0/24</c> is <c>10.0.0.0/23</c>.
    /// </summary>
    /// <returns>The supernet.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the prefix length is 0, since such a prefix has no supernet.</exception>
    public NetworkPrefix GetSupernet()
    {
        if (PrefixLength == 0)
            throw new InvalidOperationException($"{this} spans every address of its version and has no supernet.");

        return GetSupernet(PrefixLength - 1);
    }

    /// <summary>
    ///     Returns the prefix of a shorter length that contains this prefix. For example, the /16 supernet of <c>10.1.2.0/24</c>
    ///     is <c>10.1.0.0/16</c>.
    /// </summary>
    /// <param name="prefixLength">The prefix length of the supernet, which must be shorter than this prefix's length.</param>
    /// <returns>The supernet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="prefixLength" /> is negative, or is not shorter than this prefix's length.
    /// </exception>
    public NetworkPrefix GetSupernet(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength >= PrefixLength)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a supernet of {this} must be from 0 to {PrefixLength - 1}.");

        // the shorter prefix length clears the extra network bits
        return new NetworkPrefix(NetworkAddressBits, prefixLength, Version);
    }

    /// <summary>
    ///     Enumerates every address in the network that can be assigned to a host, from <see cref="FirstHost" /> to <see cref="LastHost" />.
    /// </summary>
    /// <remarks>
    ///     The addresses are produced as they are enumerated, so this works for networks of any size, but enumerating a large IPv6
    ///     network to the end is not practical.
    /// </remarks>
    /// <returns>Every host address in the network, in ascending order.</returns>
    public IEnumerable<IPAddress> Hosts()
    {
        return EnumerateAddresses(FirstHostBits, LastHostBits);
    }

    /// <summary>
    ///     Splits this prefix into its two halves, the prefixes one bit longer. For example, <c>10.0.0.0/24</c> splits into
    ///     <c>10.0.0.0/25</c> and <c>10.0.0.128/25</c>.
    /// </summary>
    /// <returns>The two halves, lower half first.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this is a host prefix, which can't be split.</exception>
    public IEnumerable<NetworkPrefix> Split()
    {
        if (IsHostPrefix)
            throw new InvalidOperationException($"{this} is a host prefix and can't be split.");

        return Split(PrefixLength + 1);
    }

    /// <summary>
    ///     Splits this prefix into every subnet of a longer prefix length. For example, <c>10.0.0.0/24</c> split to /26 is
    ///     <c>10.0.0.0/26</c>, <c>10.0.0.64/26</c>, <c>10.0.0.128/26</c> and <c>10.0.0.192/26</c>.
    /// </summary>
    /// <remarks>
    ///     The subnets are produced as they are enumerated, so this works however many subnets there are.
    /// </remarks>
    /// <param name="prefixLength">
    ///     The prefix length of the subnets, which must be longer than this prefix's length and no longer than
    ///     <see cref="AddressBitLength" />.
    /// </param>
    /// <returns>Every subnet, in ascending order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="prefixLength" /> is out of range.</exception>
    public IEnumerable<NetworkPrefix> Split(int prefixLength)
    {
        // validated here rather than inside the iterator, so that an invalid length throws when Split is called instead of
        // later, when the result is first enumerated
        if (prefixLength <= PrefixLength || prefixLength > AddressBitLength)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, $"The prefix length of a subnet of {this} must be from {PrefixLength + 1} to {AddressBitLength}.");

        return EnumerateSubnets(prefixLength);
    }

    /// <summary>
    ///     Summarizes a set of prefixes into the smallest equivalent set: the fewest prefixes that cover exactly the same addresses.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Duplicates are removed, a prefix that lies inside another prefix is dropped, and the two halves of a network are
    ///         merged into that network, repeatedly. For example, <c>10.0.0.0/25</c>, <c>10.0.0.128/26</c> and
    ///         <c>10.0.0.192/26</c> summarize to <c>10.0.0.0/24</c>.
    ///     </para>
    ///     <para>
    ///         The result is sorted in the order of <see cref="CompareTo(NetworkPrefix)" />, and may mix IPv4 and IPv6 prefixes.
    ///     </para>
    /// </remarks>
    /// <param name="networks">The prefixes to summarize.</param>
    /// <returns>The summarized prefixes, sorted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="networks" /> or one of its elements is <see langword="null" />.</exception>
    public static IReadOnlyList<NetworkPrefix> Summarize(IEnumerable<NetworkPrefix> networks)
    {
        ArgumentNullException.ThrowIfNull(networks);

        // after sorting, a network always comes directly after any network that contains it, and the lower half of a
        // network comes before the upper half
        var sortedNetworks = networks.ToList();
        foreach (var network in sortedNetworks)
            ArgumentNullException.ThrowIfNull(network, nameof(networks));

        sortedNetworks.Sort();

        // the summarized list is kept sorted and free of overlaps as it is built, so its last element is the only one that can
        // contain or pair up with the next network
        var summarizedNetworks = new List<NetworkPrefix>(sortedNetworks.Count);
        foreach (var network in sortedNetworks)
        {
            // drop a duplicate, or a network that lies inside one that has already been kept
            if (summarizedNetworks.Count > 0 && summarizedNetworks[^1].ContainsOrEqual(network))
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
                summarizedNetworks.Add(lowerHalf.GetSupernet());
            }
        }

        return summarizedNetworks;
    }

    /// <summary>
    ///     Determines whether two prefixes are the two halves of the same supernet.
    /// </summary>
    /// <param name="lowerHalf">The prefix that might be the lower half.</param>
    /// <param name="upperHalf">The prefix that might be the upper half.</param>
    /// <returns><see langword="true" /> when the prefixes are complementary halves.</returns>
    private static bool AreHalvesOfSameSupernet(NetworkPrefix lowerHalf, NetworkPrefix upperHalf)
    {
        // halves have the same version and length; a /0 has no supernet, so it can't be a half
        if (lowerHalf.Version != upperHalf.Version || lowerHalf.PrefixLength != upperHalf.PrefixLength || lowerHalf.PrefixLength == 0)
            return false;

        return lowerHalf.ComplementaryPrefix == upperHalf;
    }

    /// <summary>
    ///     Enumerates the addresses of this prefix's IP version in a range.
    /// </summary>
    /// <param name="firstAddressBits">The first address in integer form.</param>
    /// <param name="lastAddressBits">The last address in integer form, which must not be lower than the first.</param>
    /// <returns>Every address from the first to the last, inclusive.</returns>
    private IEnumerable<IPAddress> EnumerateAddresses(UInt128 firstAddressBits, UInt128 lastAddressBits)
    {
        var currentAddressBits = firstAddressBits;
        while (true)
        {
            yield return ToIPAddress(currentAddressBits);

            // stop on the last address rather than incrementing past it and testing; the last IPv6 address is UInt128.MaxValue,
            // and incrementing that wraps around to zero, which would restart the sequence instead of ending it
            if (currentAddressBits == lastAddressBits)
                yield break;

            currentAddressBits++;
        }
    }

    /// <summary>
    ///     Enumerates the subnets of this prefix that have a longer prefix length.
    /// </summary>
    /// <param name="subnetPrefixLength">The prefix length of the subnets, which the caller has validated.</param>
    /// <returns>Every subnet, in ascending order.</returns>
    private IEnumerable<NetworkPrefix> EnumerateSubnets(int subnetPrefixLength)
    {
        // each subnet spans 2^(subnet host bits) addresses. A subnet has at least one more network bit than this prefix, so it
        // has at most 127 host bits and the span always fits in a UInt128
        var subnetSpan = UInt128.One << (AddressBitLength - subnetPrefixLength);

        // the last subnet is the one that holds the last address of this network
        var lastSubnetAddressBits = LastAddressBits & AddressBits.GetNetworkMaskBits(Version, subnetPrefixLength);

        var currentSubnetAddressBits = NetworkAddressBits;
        while (true)
        {
            yield return new NetworkPrefix(currentSubnetAddressBits, subnetPrefixLength, Version);

            // stop on the last subnet rather than counting subnets; there are 2^128 /128 subnets of ::/0, which is one more
            // than a UInt128 can count
            if (currentSubnetAddressBits == lastSubnetAddressBits)
                yield break;

            currentSubnetAddressBits += subnetSpan;
        }
    }
}
