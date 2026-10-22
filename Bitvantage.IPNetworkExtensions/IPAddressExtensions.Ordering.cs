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

public static partial class IPAddressExtensions
{
    /// <param name="address">The address.</param>
    extension(IPAddress address)
    {
        /// <summary>
        ///     Compares this address with another to determine their sort order.
        /// </summary>
        /// <remarks>
        ///     IPv4 addresses sort before IPv6 addresses, and within a version addresses sort by their numeric value. IPv6
        ///     addresses that differ only in their scope ID sort by scope ID, so that only equal addresses compare as the same.
        ///     Every address sorts after <see langword="null" />.
        /// </remarks>
        /// <param name="other">The address to compare with.</param>
        /// <returns>A negative number, zero, or a positive number when this address sorts before, the same as, or after <paramref name="other" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown when this address is <see langword="null" />.</exception>
        public int CompareTo(IPAddress? other)
        {
            ArgumentNullException.ThrowIfNull(address);

            return CompareAddresses(address, other);
        }
    }

    extension(IPAddress)
    {
        /// <summary>
        ///     A comparer that sorts addresses in the order of <c>CompareTo</c>, with <see langword="null" /> first.
        /// </summary>
        /// <remarks>
        ///     <see cref="IPAddress" /> does not implement <see cref="IComparable{T}" />, so sorting methods and sorted collections
        ///     throw unless they are given a comparer, as in <c>addresses.Order(IPAddress.Comparer)</c>.
        /// </remarks>
        public static IComparer<IPAddress?> Comparer => IPAddressComparer.Instance;

        /// <summary>
        ///     Moves an address forward. For example, <c>10.0.0.255 + 1</c> is <c>10.0.1.0</c>. An IPv6 scope ID is kept.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="count">The number of addresses to move forward by.</param>
        /// <returns>The address <paramref name="count" /> addresses after <paramref name="address" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be past the last address.</exception>
        public static IPAddress operator +(IPAddress address, UInt128 count)
        {
            return MoveBy(address, count, true);
        }

        /// <summary>
        ///     Moves an address back. For example, <c>10.0.1.0 - 1</c> is <c>10.0.0.255</c>. An IPv6 scope ID is kept.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="count">The number of addresses to move back by.</param>
        /// <returns>The address <paramref name="count" /> addresses before <paramref name="address" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be before the first address.</exception>
        public static IPAddress operator -(IPAddress address, UInt128 count)
        {
            return MoveBy(address, count, false);
        }

        /// <summary>
        ///     Determines whether one address sorts after another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first address.</param>
        /// <param name="right">The second address.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts after <paramref name="right" />.</returns>
        public static bool operator >(IPAddress? left, IPAddress? right)
        {
            return CompareAddresses(left, right) > 0;
        }

        /// <summary>
        ///     Determines whether one address sorts after or the same as another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first address.</param>
        /// <param name="right">The second address.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts after or the same as <paramref name="right" />.</returns>
        public static bool operator >=(IPAddress? left, IPAddress? right)
        {
            return CompareAddresses(left, right) >= 0;
        }

        /// <summary>
        ///     Determines whether one address sorts before another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first address.</param>
        /// <param name="right">The second address.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts before <paramref name="right" />.</returns>
        public static bool operator <(IPAddress? left, IPAddress? right)
        {
            return CompareAddresses(left, right) < 0;
        }

        /// <summary>
        ///     Determines whether one address sorts before or the same as another. See <c>CompareTo</c>.
        /// </summary>
        /// <param name="left">The first address.</param>
        /// <param name="right">The second address.</param>
        /// <returns><see langword="true" /> when <paramref name="left" /> sorts before or the same as <paramref name="right" />.</returns>
        public static bool operator <=(IPAddress? left, IPAddress? right)
        {
            return CompareAddresses(left, right) <= 0;
        }
    }

    /// <summary>
    ///     Compares two addresses, either of which may be <see langword="null" />. See <c>CompareTo</c>.
    /// </summary>
    /// <param name="first">The first address.</param>
    /// <param name="second">The second address.</param>
    /// <returns>A negative number, zero, or a positive number when the first address sorts before, the same as, or after the second.</returns>
    private static int CompareAddresses(IPAddress? first, IPAddress? second)
    {
        if (ReferenceEquals(first, second))
            return 0;

        // null sorts before every address
        if (first is null)
            return -1;

        if (second is null)
            return 1;

        // IPv4 sorts before IPv6
        var firstVersion = IPNetworkExtensions.GetVersionOf(first, nameof(first));
        var secondVersion = IPNetworkExtensions.GetVersionOf(second, nameof(second));
        var versionComparison = ((int)firstVersion).CompareTo((int)secondVersion);
        if (versionComparison != 0)
            return versionComparison;

        // then by numeric value
        var valueComparison = AddressMath.FromIPAddress(first).CompareTo(AddressMath.FromIPAddress(second));
        if (valueComparison != 0 || firstVersion == IPVersion.IPv4)
            return valueComparison;

        // IPv6 addresses that differ only in scope ID are not equal, so they do not compare as the same either. An IPv4 address
        // has no scope ID, and reading one throws, so this is only reached for IPv6
        return first.ScopeId.CompareTo(second.ScopeId);
    }

    /// <summary>
    ///     Moves an address forward or back by a number of addresses.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="count">The number of addresses to move by.</param>
    /// <param name="isForward"><see langword="true" /> to move towards higher addresses; <see langword="false" /> to move towards lower addresses.</param>
    /// <returns>The address that is <paramref name="count" /> addresses away.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result would be outside the address space.</exception>
    private static IPAddress MoveBy(IPAddress address, UInt128 count, bool isForward)
    {
        var version = IPNetworkExtensions.GetVersionOf(address, nameof(address));
        var addressBits = AddressMath.FromIPAddress(address);

        UInt128 movedAddressBits;
        try
        {
            // checked arithmetic turns an overflow past the last IPv6 address, or an underflow below address zero, into an
            // OverflowException
            movedAddressBits = checked(isForward ? addressBits + count : addressBits - count);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, $"Moving {address} {(isForward ? "forward" : "back")} by {count} goes past the end of the {version} address space.");
        }

        // an IPv4 address that runs past 255.255.255.255 still fits in the integer, so it is checked separately
        if (movedAddressBits > AddressMath.GetAllBits(version))
            throw new ArgumentOutOfRangeException(nameof(count), count, $"Moving {address} forward by {count} goes past the end of the {version} address space.");

        var movedAddress = AddressMath.ToIPAddress(movedAddressBits, version);

        // keep the scope ID, so that a link-local address stays on the same interface
        if (version == IPVersion.IPv6 && address.ScopeId != 0)
            movedAddress.ScopeId = address.ScopeId;

        return movedAddress;
    }

    /// <summary>
    ///     Sorts <see cref="IPAddress" /> values in the order of <c>CompareTo</c>, with <see langword="null" /> first.
    /// </summary>
    private sealed class IPAddressComparer : IComparer<IPAddress?>
    {
        /// <summary>
        ///     The single instance of the comparer, which has no state.
        /// </summary>
        internal static readonly IPAddressComparer Instance = new();

        /// <inheritdoc />
        public int Compare(IPAddress? first, IPAddress? second)
        {
            return CompareAddresses(first, second);
        }
    }
}
