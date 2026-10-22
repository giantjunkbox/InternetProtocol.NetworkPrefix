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
using System.Numerics;

namespace Bitvantage.InternetProtocol;

/// <summary>
///     Adds the functionality of <see cref="NetworkPrefix" /> to <see cref="IPNetwork" />.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IPNetwork" /> holds a network address and a prefix length and can test whether an address is a member, but
///         has no masks, host ranges, containment between networks, splitting, summarization or sort order. These extension
///         members provide them, with the same names and behavior as on <see cref="NetworkPrefix" />, so that code which already
///         uses <see cref="IPNetwork" /> does not need to convert.
///     </para>
///     <code>
///     var network = IPNetwork.Parse("10.0.0.0/24");
///     var mask = network.NetworkMask;                         // 255.255.255.0
///     var halves = network.Split();                           // 10.0.0.0/25, 10.0.0.128/25
///     var sorted = networks.Order(IPNetwork.Comparer);
///     </code>
/// </remarks>
public static class IPNetworkExtensions
{
    /// <param name="network">The network.</param>
    extension(IPNetwork network)
    {
        /// <inheritdoc cref="NetworkPrefix.AddressBitLength" />
        public int AddressBitLength => network.ToNetworkPrefix().AddressBitLength;

        /// <inheritdoc cref="NetworkPrefix.Allocation" />
        public AddressAllocation Allocation => network.ToNetworkPrefix().Allocation;

        /// <inheritdoc cref="NetworkPrefix.BroadcastAddress" />
        public IPAddress BroadcastAddress => network.ToNetworkPrefix().BroadcastAddress;

        /// <inheritdoc cref="NetworkPrefix.Class" />
        public NetworkClass Class => network.ToNetworkPrefix().Class;

        /// <inheritdoc cref="NetworkPrefix.ComplementaryPrefix" />
        public IPNetwork ComplementaryPrefix => network.ToNetworkPrefix().ComplementaryPrefix;

        /// <inheritdoc cref="NetworkPrefix.FirstHost" />
        public IPAddress FirstHost => network.ToNetworkPrefix().FirstHost;

        /// <inheritdoc cref="NetworkPrefix.HostBitLength" />
        public int HostBitLength => network.ToNetworkPrefix().HostBitLength;

        /// <inheritdoc cref="NetworkPrefix.HostMask" />
        public IPAddress HostMask => network.ToNetworkPrefix().HostMask;

        /// <inheritdoc cref="NetworkPrefix.IsHostPrefix" />
        public bool IsHostPrefix => network.ToNetworkPrefix().IsHostPrefix;

        /// <inheritdoc cref="NetworkPrefix.LastAddress" />
        public IPAddress LastAddress => network.ToNetworkPrefix().LastAddress;

        /// <inheritdoc cref="NetworkPrefix.LastHost" />
        public IPAddress LastHost => network.ToNetworkPrefix().LastHost;

        /// <inheritdoc cref="NetworkPrefix.NetworkMask" />
        public IPAddress NetworkMask => network.ToNetworkPrefix().NetworkMask;

        /// <inheritdoc cref="NetworkPrefix.TotalAddresses" />
        public BigInteger TotalAddresses => network.ToNetworkPrefix().TotalAddresses;

        /// <inheritdoc cref="NetworkPrefix.TotalHosts" />
        public UInt128 TotalHosts => network.ToNetworkPrefix().TotalHosts;

        /// <inheritdoc cref="NetworkPrefix.Version" />
        public IPVersion Version => network.ToNetworkPrefix().Version;

        /// <inheritdoc cref="NetworkPrefix.Addresses" />
        public IEnumerable<IPAddress> Addresses()
        {
            return network.ToNetworkPrefix().Addresses();
        }

        /// <summary>
        ///     Determines whether this network is strictly contained by another network: it is a smaller network inside the other.
        /// </summary>
        /// <param name="other">The network that might contain this one.</param>
        /// <returns><see langword="true" /> when <paramref name="other" /> contains this network and is not equal to it.</returns>
        public bool ContainedBy(IPNetwork other)
        {
            return network.ToNetworkPrefix().ContainedBy(other.ToNetworkPrefix());
        }

        /// <summary>
        ///     Determines whether this network is contained by another network or is equal to it.
        /// </summary>
        /// <param name="other">The network that might contain this one.</param>
        /// <returns><see langword="true" /> when every address of this network is in <paramref name="other" />.</returns>
        public bool ContainedByOrEqual(IPNetwork other)
        {
            return network.ToNetworkPrefix().ContainedByOrEqual(other.ToNetworkPrefix());
        }

        /// <summary>
        ///     Determines whether this network strictly contains another network: the other is a smaller network inside this one.
        /// </summary>
        /// <param name="other">The network that might be contained.</param>
        /// <returns><see langword="true" /> when this network contains <paramref name="other" /> and is not equal to it.</returns>
        public bool Contains(IPNetwork other)
        {
            return network.ToNetworkPrefix().Contains(other.ToNetworkPrefix());
        }

        /// <summary>
        ///     Determines whether this network contains another network or is equal to it.
        /// </summary>
        /// <param name="other">The network that might be contained.</param>
        /// <returns><see langword="true" /> when every address of <paramref name="other" /> is in this network.</returns>
        public bool ContainsOrEqual(IPNetwork other)
        {
            return network.ToNetworkPrefix().ContainsOrEqual(other.ToNetworkPrefix());
        }

        /// <summary>
        ///     Computes the set difference of this network and another: the smallest list of networks that covers every address of
        ///     this network that is not in <paramref name="networkToExclude" />. See <see cref="NetworkPrefix.Exclude" />.
        /// </summary>
        /// <param name="networkToExclude">The network to remove from this one.</param>
        /// <returns>The networks that remain.</returns>
        public IReadOnlyList<IPNetwork> Exclude(IPNetwork networkToExclude)
        {
            return network.ToNetworkPrefix()
                .Exclude(networkToExclude.ToNetworkPrefix())
                .Select(remainingNetwork => remainingNetwork.ToIPNetwork())
                .ToArray();
        }

        /// <inheritdoc cref="NetworkPrefix.GetSupernet()" />
        public IPNetwork GetSupernet()
        {
            return network.ToNetworkPrefix().GetSupernet();
        }

        /// <inheritdoc cref="NetworkPrefix.GetSupernet(int)" />
        public IPNetwork GetSupernet(int prefixLength)
        {
            return network.ToNetworkPrefix().GetSupernet(prefixLength);
        }

        /// <inheritdoc cref="NetworkPrefix.Hosts" />
        public IEnumerable<IPAddress> Hosts()
        {
            return network.ToNetworkPrefix().Hosts();
        }

        /// <summary>
        ///     Determines whether this network and another network share at least one address.
        /// </summary>
        /// <param name="other">The other network.</param>
        /// <returns><see langword="true" /> when the two networks overlap.</returns>
        public bool Overlaps(IPNetwork other)
        {
            return network.ToNetworkPrefix().Overlaps(other.ToNetworkPrefix());
        }

        /// <inheritdoc cref="NetworkPrefix.Split()" />
        public IEnumerable<IPNetwork> Split()
        {
            return network.ToNetworkPrefix().Split().Select(subnet => subnet.ToIPNetwork());
        }

        /// <inheritdoc cref="NetworkPrefix.Split(int)" />
        public IEnumerable<IPNetwork> Split(int prefixLength)
        {
            return network.ToNetworkPrefix().Split(prefixLength).Select(subnet => subnet.ToIPNetwork());
        }

        /// <summary>
        ///     Converts this network to a <see cref="NetworkPrefix" />.
        /// </summary>
        /// <returns>A new prefix with the same network address and prefix length.</returns>
        public NetworkPrefix ToNetworkPrefix()
        {
            return new NetworkPrefix(network);
        }

        /// <inheritdoc cref="NetworkPrefix.ToString(NetworkFormat)" />
        public string ToString(NetworkFormat format)
        {
            return network.ToNetworkPrefix().ToString(format);
        }
    }

    extension(IPNetwork)
    {
        /// <summary>
        ///     A comparer that sorts networks in the same order as <see cref="NetworkPrefix.CompareTo(NetworkPrefix)" />: IPv4 before
        ///     IPv6, then by network address, then the bigger network first. <see cref="IPNetwork" /> does not implement
        ///     <see cref="IComparable{T}" />, so pass this to sorting methods, as in <c>networks.Order(IPNetwork.Comparer)</c>.
        /// </summary>
        public static IComparer<IPNetwork> Comparer => IPNetworkComparer.Instance;

        /// <summary>
        ///     Computes the smallest network that contains two networks. See
        ///     <see cref="NetworkPrefix.GetContainingNetwork(NetworkPrefix, NetworkPrefix)" />.
        /// </summary>
        /// <param name="first">The first network.</param>
        /// <param name="second">The second network.</param>
        /// <returns>The smallest network that contains both networks.</returns>
        /// <exception cref="ArgumentException">Thrown when the networks are different IP versions.</exception>
        public static IPNetwork GetContainingNetwork(IPNetwork first, IPNetwork second)
        {
            return NetworkPrefix.GetContainingNetwork(first.ToNetworkPrefix(), second.ToNetworkPrefix());
        }

        /// <summary>
        ///     Computes the smallest network that contains every one of a set of networks. See
        ///     <see cref="NetworkPrefix.GetContainingNetwork(IEnumerable{NetworkPrefix})" />.
        /// </summary>
        /// <param name="networks">The networks, which must all be the same IP version.</param>
        /// <returns>The smallest network that contains every network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="networks" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="networks" /> is empty or mixes IP versions.</exception>
        public static IPNetwork GetContainingNetwork(IEnumerable<IPNetwork> networks)
        {
            ArgumentNullException.ThrowIfNull(networks);

            return NetworkPrefix.GetContainingNetwork(networks.Select(network => network.ToNetworkPrefix()));
        }

        /// <summary>
        ///     Summarizes a set of networks into the fewest networks that cover exactly the same addresses. See
        ///     <see cref="NetworkPrefix.Summarize" />.
        /// </summary>
        /// <param name="networks">The networks to summarize.</param>
        /// <returns>The summarized networks, sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="networks" /> is <see langword="null" />.</exception>
        public static IReadOnlyList<IPNetwork> Summarize(IEnumerable<IPNetwork> networks)
        {
            ArgumentNullException.ThrowIfNull(networks);

            return NetworkPrefix
                .Summarize(networks.Select(network => network.ToNetworkPrefix()))
                .Select(summarizedNetwork => summarizedNetwork.ToIPNetwork())
                .ToArray();
        }
    }

    /// <summary>
    ///     Sorts <see cref="IPNetwork" /> values in the same order as <see cref="NetworkPrefix" />.
    /// </summary>
    private sealed class IPNetworkComparer : IComparer<IPNetwork>
    {
        /// <summary>
        ///     The single instance of the comparer, which has no state.
        /// </summary>
        internal static readonly IPNetworkComparer Instance = new();

        /// <inheritdoc />
        public int Compare(IPNetwork first, IPNetwork second)
        {
            // compare the parts directly rather than converting each network to a NetworkPrefix, since sorting compares often
            var firstVersion = AddressBits.GetVersion(first.BaseAddress.AddressFamily);
            var secondVersion = AddressBits.GetVersion(second.BaseAddress.AddressFamily);

            return NetworkPrefix.Compare(
                firstVersion, AddressBits.FromIPAddress(first.BaseAddress), first.PrefixLength,
                secondVersion, AddressBits.FromIPAddress(second.BaseAddress), second.PrefixLength);
        }
    }
}
