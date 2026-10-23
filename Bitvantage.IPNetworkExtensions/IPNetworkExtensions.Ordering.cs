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
using System.Net.Sockets;

namespace Bitvantage.InternetProtocol;

public static partial class IPNetworkExtensions
{
    /// <param name="network">The network.</param>
    extension(IPNetwork network)
    {
        /// <summary>
        ///     Compares this network with another to determine their sort order.
        /// </summary>
        /// <remarks>
        ///     IPv4 networks sort before IPv6 networks. Within a version, networks sort by base address, and a bigger network
        ///     sorts before a smaller network with the same base address, so a network always sorts before the networks it
        ///     contains. IPv6 networks that differ only in the scope ID of their base address sort by scope ID, so that two
        ///     networks sort the same only when they are equal.
        /// </remarks>
        /// <param name="other">The network to compare with.</param>
        /// <returns>A negative number, zero, or a positive number when this network sorts before, the same as, or after <paramref name="other" />.</returns>
        public int CompareTo(IPNetwork other)
        {
            var comparison = NetworkBits.FromIPNetwork(network).CompareTo(NetworkBits.FromIPNetwork(other));
            if (comparison != 0 || network.BaseAddress.AddressFamily != AddressFamily.InterNetworkV6)
                return comparison;

            // IPNetwork equality includes the scope ID, so networks that differ only in it must not sort the same, or a sorted
            // set would keep just one of them. Only an IPv6 address has a scope ID; reading it from an IPv4 address throws
            return network.BaseAddress.ScopeId.CompareTo(other.BaseAddress.ScopeId);
        }
    }

    extension(IPNetwork)
    {
        /// <summary>
        ///     A comparer that sorts networks in the order of <c>CompareTo</c>.
        /// </summary>
        /// <remarks>
        ///     <see cref="IPNetwork" /> does not implement <see cref="IComparable{T}" />, so sorting methods and sorted collections
        ///     throw unless they are given a comparer, as in <c>networks.Order(IPNetwork.Comparer)</c> or
        ///     <c>new SortedSet&lt;IPNetwork&gt;(IPNetwork.Comparer)</c>.
        /// </remarks>
        public static IComparer<IPNetwork> Comparer => IPNetworkComparer.Instance;

        /// <summary>
        ///     Moves a network forward by a number of networks of the same size. For example, <c>10.0.0.0/24 + 2</c> is <c>10.0.2.0/24</c>.
        /// </summary>
        /// <param name="network">The network to move.</param>
        /// <param name="networkCount">The number of networks to move forward by.</param>
        /// <returns>The network <paramref name="networkCount" /> networks after <paramref name="network" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be past the end of the address space.</exception>
        public static IPNetwork operator +(IPNetwork network, UInt128 networkCount)
        {
            return MoveBy(network, networkCount, true);
        }

        /// <summary>
        ///     Moves a network back by a number of networks of the same size. For example, <c>10.0.2.0/24 - 2</c> is <c>10.0.0.0/24</c>.
        /// </summary>
        /// <param name="network">The network to move.</param>
        /// <param name="networkCount">The number of networks to move back by.</param>
        /// <returns>The network <paramref name="networkCount" /> networks before <paramref name="network" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be before the start of the address space.</exception>
        public static IPNetwork operator -(IPNetwork network, UInt128 networkCount)
        {
            return MoveBy(network, networkCount, false);
        }

        /// <summary>
        ///     Moves a network forward by a number of networks of the same size, or back when the number is negative. For example,
        ///     <c>10.0.0.0/24 + 2</c> is <c>10.0.2.0/24</c>.
        /// </summary>
        /// <remarks>
        ///     This overload accepts an <see cref="int" /> variable, as in <c>network + index</c>. Cast other integer types to
        ///     <see cref="int" /> or <see cref="UInt128" />.
        /// </remarks>
        /// <param name="network">The network to move.</param>
        /// <param name="networkCount">The number of networks to move forward by, or back by when negative.</param>
        /// <returns>The network <paramref name="networkCount" /> networks after <paramref name="network" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be outside the address space.</exception>
        public static IPNetwork operator +(IPNetwork network, int networkCount)
        {
            return MoveBy(network, networkCount, true);
        }

        /// <summary>
        ///     Moves a network back by a number of networks of the same size, or forward when the number is negative. For example,
        ///     <c>10.0.2.0/24 - 2</c> is <c>10.0.0.0/24</c>.
        /// </summary>
        /// <remarks>
        ///     This overload accepts an <see cref="int" /> variable, as in <c>network - index</c>. Cast other integer types to
        ///     <see cref="int" /> or <see cref="UInt128" />.
        /// </remarks>
        /// <param name="network">The network to move.</param>
        /// <param name="networkCount">The number of networks to move back by, or forward by when negative.</param>
        /// <returns>The network <paramref name="networkCount" /> networks before <paramref name="network" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be outside the address space.</exception>
        public static IPNetwork operator -(IPNetwork network, int networkCount)
        {
            return MoveBy(network, networkCount, false);
        }

        /// <summary>
        ///     Determines whether one network sorts after another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first network.</param>
        /// <param name="right">The second network.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts after <paramref name="right" />.</returns>
        public static bool operator >(IPNetwork left, IPNetwork right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        ///     Determines whether one network sorts after or the same as another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first network.</param>
        /// <param name="right">The second network.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts after or the same as <paramref name="right" />.</returns>
        public static bool operator >=(IPNetwork left, IPNetwork right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        ///     Determines whether one network sorts before another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first network.</param>
        /// <param name="right">The second network.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts before <paramref name="right" />.</returns>
        public static bool operator <(IPNetwork left, IPNetwork right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        ///     Determines whether one network sorts before or the same as another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first network.</param>
        /// <param name="right">The second network.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts before or the same as <paramref name="right" />.</returns>
        public static bool operator <=(IPNetwork left, IPNetwork right)
        {
            return left.CompareTo(right) <= 0;
        }
    }

    /// <summary>
    ///     Moves a network by a signed number of networks of the same size.
    /// </summary>
    /// <param name="network">The network to move.</param>
    /// <param name="networkCount">The number of networks to move by, in the other direction when negative.</param>
    /// <param name="isForward"><see langword="true" /> when a positive count moves towards higher addresses.</param>
    /// <returns>The network that is <paramref name="networkCount" /> networks away.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be outside the address space.</exception>
    private static IPNetwork MoveBy(IPNetwork network, int networkCount, bool isForward)
    {
        // a negative count moves the other way. It is widened before it is negated, because -int.MinValue does not fit in an int
        return networkCount >= 0 ? MoveBy(network, (UInt128)networkCount, isForward) : MoveBy(network, (UInt128)(-(long)networkCount), !isForward);
    }

    /// <summary>
    ///     Moves a network forward or back by a number of networks of the same size.
    /// </summary>
    /// <param name="network">The network to move.</param>
    /// <param name="networkCount">The number of networks to move by.</param>
    /// <param name="isForward"><see langword="true" /> to move towards higher addresses; <see langword="false" /> to move towards lower addresses.</param>
    /// <returns>The network that is <paramref name="networkCount" /> networks away.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be outside the address space.</exception>
    private static IPNetwork MoveBy(IPNetwork network, UInt128 networkCount, bool isForward)
    {
        var networkBits = NetworkBits.FromIPNetwork(network);

        if (networkCount == UInt128.Zero)
            return networkBits.ToIPNetwork();

        // ::/0 spans all 2^128 IPv6 addresses, which is one more than a UInt128 can hold, and there is no network beside it to move to
        if (networkBits.HostMaskBits == UInt128.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(networkCount), networkCount, $"{network} spans every IPv6 address and can't be moved.");

        try
        {
            // checked arithmetic turns an overflow, or an underflow below address zero, into an OverflowException
            checked
            {
                // each network of this length spans the host mask plus one addresses
                var networkSpan = networkBits.HostMaskBits + UInt128.One;
                var distance = networkSpan * networkCount;
                var movedAddressBits = isForward ? networkBits.BaseAddressBits + distance : networkBits.BaseAddressBits - distance;

                // the last network of this length has every network bit set, so its address equals the network mask, and an
                // IPv4 result beyond it has run past the end of the 32 bit address space
                if (movedAddressBits > networkBits.NetworkMaskBits)
                    throw new OverflowException();

                return new NetworkBits(networkBits.Version, movedAddressBits, networkBits.PrefixLength).ToIPNetwork();
            }
        }
        catch (OverflowException)
        {
            var direction = isForward ? "after" : "before";
            throw new ArgumentOutOfRangeException(nameof(networkCount), networkCount, $"There are fewer than {networkCount} /{networkBits.PrefixLength} networks {direction} {network}.");
        }
    }

    /// <summary>
    ///     Sorts <see cref="IPNetwork" /> values in the order of <c>CompareTo</c>.
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
            return first.CompareTo(second);
        }
    }
}
