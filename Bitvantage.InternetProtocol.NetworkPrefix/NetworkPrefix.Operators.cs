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

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Bitvantage.InternetProtocol;

public sealed partial class NetworkPrefix
{
    /// <summary>
    ///     Moves a prefix forward by a number of networks of the same size. For example, <c>10.0.0.0/24 + 2</c> is <c>10.0.2.0/24</c>.
    /// </summary>
    /// <param name="network">The prefix to move.</param>
    /// <param name="networkCount">The number of networks to move forward by.</param>
    /// <returns>The prefix <paramref name="networkCount" /> networks after <paramref name="network" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="network" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be past the end of the address space.</exception>
    public static NetworkPrefix operator +(NetworkPrefix network, UInt128 networkCount)
    {
        ArgumentNullException.ThrowIfNull(network);

        return network.MoveBy(networkCount, true);
    }

    /// <summary>
    ///     Determines whether two prefixes are equal. See <see cref="Equals(NetworkPrefix)" />.
    /// </summary>
    /// <param name="left">The first prefix.</param>
    /// <param name="right">The second prefix.</param>
    /// <returns><see langword="true" /> when the prefixes are equal, or both are <see langword="null" />.</returns>
    public static bool operator ==(NetworkPrefix? left, NetworkPrefix? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>
    ///     Determines whether a prefix and an <see cref="IPNetwork" /> describe the same network.
    /// </summary>
    /// <remarks>
    ///     This overload exists because the implicit conversions in both directions would otherwise make the comparison ambiguous.
    /// </remarks>
    /// <param name="left">The prefix.</param>
    /// <param name="right">The network.</param>
    /// <returns><see langword="true" /> when both have the same network address and prefix length.</returns>
    public static bool operator ==(NetworkPrefix? left, IPNetwork right)
    {
        return left is not null && left.Equals(new NetworkPrefix(right));
    }

    /// <summary>
    ///     Determines whether an <see cref="IPNetwork" /> and a prefix describe the same network.
    /// </summary>
    /// <remarks>
    ///     This overload exists because the implicit conversions in both directions would otherwise make the comparison ambiguous.
    /// </remarks>
    /// <param name="left">The network.</param>
    /// <param name="right">The prefix.</param>
    /// <returns><see langword="true" /> when both have the same network address and prefix length.</returns>
    public static bool operator ==(IPNetwork left, NetworkPrefix? right)
    {
        return right == left;
    }

    /// <summary>
    ///     Converts a string to a prefix with <see cref="Parse(string)" />.
    /// </summary>
    /// <param name="value">The text of a prefix, such as <c>10.0.0.0/24</c>.</param>
    /// <returns>The parsed prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid prefix.</exception>
    public static implicit operator NetworkPrefix(string value)
    {
        return Parse(value);
    }

    /// <summary>
    ///     Converts a prefix to its CIDR notation, such as <c>10.0.0.0/24</c>.
    /// </summary>
    /// <param name="value">The prefix.</param>
    /// <returns>The text of the prefix, or <see langword="null" /> when <paramref name="value" /> is <see langword="null" />.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static implicit operator string?(NetworkPrefix? value)
    {
        return value?.ToString();
    }

    /// <summary>
    ///     Converts an <see cref="IPNetwork" /> to a prefix. See <see cref="FromIPNetwork" />.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <returns>A new prefix.</returns>
    public static implicit operator NetworkPrefix(IPNetwork network)
    {
        return new NetworkPrefix(network);
    }

    /// <summary>
    ///     Converts a prefix to an <see cref="IPNetwork" />. See <see cref="ToIPNetwork" />.
    /// </summary>
    /// <param name="network">The prefix.</param>
    /// <returns>A new <see cref="IPNetwork" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="network" /> is <see langword="null" />.</exception>
    public static implicit operator IPNetwork(NetworkPrefix network)
    {
        ArgumentNullException.ThrowIfNull(network);

        return network.ToIPNetwork();
    }

    /// <summary>
    ///     Determines whether one prefix sorts after another. See <see cref="CompareTo(NetworkPrefix)" />.
    /// </summary>
    /// <param name="left">The first prefix.</param>
    /// <param name="right">The second prefix.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> sorts after <paramref name="right" />.</returns>
    public static bool operator >(NetworkPrefix? left, NetworkPrefix? right)
    {
        return Comparer<NetworkPrefix>.Default.Compare(left, right) > 0;
    }

    /// <summary>
    ///     Determines whether one prefix sorts after or the same as another. See <see cref="CompareTo(NetworkPrefix)" />.
    /// </summary>
    /// <param name="left">The first prefix.</param>
    /// <param name="right">The second prefix.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> sorts after or the same as <paramref name="right" />.</returns>
    public static bool operator >=(NetworkPrefix? left, NetworkPrefix? right)
    {
        return Comparer<NetworkPrefix>.Default.Compare(left, right) >= 0;
    }

    /// <summary>
    ///     Determines whether two prefixes are not equal. See <see cref="Equals(NetworkPrefix)" />.
    /// </summary>
    /// <param name="left">The first prefix.</param>
    /// <param name="right">The second prefix.</param>
    /// <returns><see langword="true" /> when the prefixes are not equal.</returns>
    public static bool operator !=(NetworkPrefix? left, NetworkPrefix? right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Determines whether a prefix and an <see cref="IPNetwork" /> describe different networks.
    /// </summary>
    /// <param name="left">The prefix.</param>
    /// <param name="right">The network.</param>
    /// <returns><see langword="true" /> when the network address or the prefix length differ.</returns>
    public static bool operator !=(NetworkPrefix? left, IPNetwork right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Determines whether an <see cref="IPNetwork" /> and a prefix describe different networks.
    /// </summary>
    /// <param name="left">The network.</param>
    /// <param name="right">The prefix.</param>
    /// <returns><see langword="true" /> when the network address or the prefix length differ.</returns>
    public static bool operator !=(IPNetwork left, NetworkPrefix? right)
    {
        return !(right == left);
    }

    /// <summary>
    ///     Determines whether one prefix sorts before another. See <see cref="CompareTo(NetworkPrefix)" />.
    /// </summary>
    /// <param name="left">The first prefix.</param>
    /// <param name="right">The second prefix.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> sorts before <paramref name="right" />.</returns>
    public static bool operator <(NetworkPrefix? left, NetworkPrefix? right)
    {
        return Comparer<NetworkPrefix>.Default.Compare(left, right) < 0;
    }

    /// <summary>
    ///     Determines whether one prefix sorts before or the same as another. See <see cref="CompareTo(NetworkPrefix)" />.
    /// </summary>
    /// <param name="left">The first prefix.</param>
    /// <param name="right">The second prefix.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> sorts before or the same as <paramref name="right" />.</returns>
    public static bool operator <=(NetworkPrefix? left, NetworkPrefix? right)
    {
        return Comparer<NetworkPrefix>.Default.Compare(left, right) <= 0;
    }

    /// <summary>
    ///     Moves a prefix back by a number of networks of the same size. For example, <c>10.0.2.0/24 - 2</c> is <c>10.0.0.0/24</c>.
    /// </summary>
    /// <param name="network">The prefix to move.</param>
    /// <param name="networkCount">The number of networks to move back by.</param>
    /// <returns>The prefix <paramref name="networkCount" /> networks before <paramref name="network" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="network" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be before the start of the address space.</exception>
    public static NetworkPrefix operator -(NetworkPrefix network, UInt128 networkCount)
    {
        ArgumentNullException.ThrowIfNull(network);

        return network.MoveBy(networkCount, false);
    }

    /// <summary>
    ///     Moves this prefix forward or back by a number of networks of the same size.
    /// </summary>
    /// <param name="networkCount">The number of networks to move by.</param>
    /// <param name="isForward"><see langword="true" /> to move towards higher addresses; <see langword="false" /> to move towards lower addresses.</param>
    /// <returns>The prefix that is <paramref name="networkCount" /> networks away.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be outside the address space.</exception>
    private NetworkPrefix MoveBy(UInt128 networkCount, bool isForward)
    {
        if (networkCount == UInt128.Zero)
            return this;

        // ::/0 spans all 2^128 IPv6 addresses, which is one more than a UInt128 can hold, and there is no network beside it to move to
        if (HostMaskBits == UInt128.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(networkCount), networkCount, $"{this} spans every IPv6 address and can't be moved.");

        try
        {
            // checked arithmetic turns an overflow, or an underflow below address zero, into an OverflowException
            checked
            {
                // each network of this length spans the host mask plus one addresses
                var networkSpan = HostMaskBits + UInt128.One;
                var distance = networkSpan * networkCount;
                var movedAddressBits = isForward ? NetworkAddressBits + distance : NetworkAddressBits - distance;

                // the last network of this length has every network bit set, so its address equals the network mask, and an
                // IPv4 result beyond it has run past the end of the 32 bit address space
                if (movedAddressBits > NetworkMaskBits)
                    throw new OverflowException();

                return new NetworkPrefix(movedAddressBits, PrefixLength, Version);
            }
        }
        catch (OverflowException)
        {
            var direction = isForward ? "after" : "before";
            throw new ArgumentOutOfRangeException(nameof(networkCount), networkCount, $"There are fewer than {networkCount} /{PrefixLength} networks {direction} {this}.");
        }
    }
}
