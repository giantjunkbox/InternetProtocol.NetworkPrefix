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

/// <summary>
///     Extension members that round out <see cref="IPAddress" /> for network work: its integer form, sort order and arithmetic,
///     classification, strict parsing, mask tests, unmapping of IPv4-mapped addresses, and reverse DNS names.
/// </summary>
/// <remarks>
///     <para>
///         These are C# 14 extension members, so the properties, operators and static members appear on <see cref="IPAddress" />
///         itself once the <c>Bitvantage.InternetProtocol</c> namespace is imported. They pair with the members on
///         <see cref="IPNetwork" /> in <see cref="IPNetworkExtensions" />, and use the same integer form and sort order.
///     </para>
///     <code>
///     using Bitvantage.InternetProtocol;
///
///     var address = IPAddress.ParseStrict("10.0.0.7");
///     var allocation = address.Allocation;              // Private
///     var next = address + 1;                           // 10.0.0.8
///     var sorted = addresses.Order(IPAddress.Comparer);
///     </code>
/// </remarks>
public static partial class IPAddressExtensions
{
    /// <param name="address">The address.</param>
    extension(IPAddress address)
    {
        /// <summary>
        ///     The address as an unsigned integer whose most significant bit is the first bit of the address. IPv4 addresses occupy
        ///     the low 32 bits. This is the same form as the integer members of <see cref="IPNetwork" />, such as
        ///     <c>BaseAddressBits</c> and <c>NetworkMaskBits</c>.
        /// </summary>
        /// <remarks>
        ///     An IPv6 scope ID is not part of the integer. Use <c>IPAddress.FromAddressBits</c> to convert back.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public UInt128 AddressBits
        {
            get
            {
                IPNetworkExtensions.GetVersionOf(address, nameof(address));
                return AddressMath.FromIPAddress(address);
            }
        }

        /// <summary>
        ///     The purpose of the registered block of address space that holds the address, such as
        ///     <see cref="AddressAllocation.Private" /> for <c>10.1.2.3</c>.
        /// </summary>
        /// <remarks>
        ///     An IPv4-mapped IPv6 address, such as <c>::ffff:10.1.2.3</c>, which is how a dual-stack server reports an IPv4 client,
        ///     takes the allocation of the IPv4 address that it maps. See <see cref="AddressAllocation" />.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public AddressAllocation Allocation => IPNetworkExtensions.GetAllocation(IPNetworkExtensions.ToHostNetworkBits(address, nameof(address)));

        /// <summary>
        ///     The historical class of an IPv4 address, such as <see cref="NetworkClass.A" /> for <c>10.1.2.3</c>, or
        ///     <see cref="NetworkClass.Undefined" /> for an IPv6 address.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public NetworkClass Class => IPNetworkExtensions.GetClass(IPNetworkExtensions.ToHostNetworkBits(address, nameof(address)));

        /// <summary>
        ///     Gets whether the address is a valid host mask, also called a wildcard mask: a run of zero bits followed by a run of
        ///     one bits, such as <c>0.0.0.255</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public bool IsHostMask
        {
            get
            {
                var version = IPNetworkExtensions.GetVersionOf(address, nameof(address));
                return IPNetworkExtensions.TryGetPrefixLengthFromHostMask(address, version, out _);
            }
        }

        /// <summary>
        ///     Gets whether the address is a valid network mask: a run of one bits followed by a run of zero bits, such as
        ///     <c>255.255.255.0</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public bool IsNetworkMask => address.TryGetPrefixLength(out _);

        /// <summary>
        ///     The name of the reverse DNS pointer record for the address, such as <c>4.3.2.1.in-addr.arpa</c> for <c>1.2.3.4</c>.
        ///     IPv6 addresses use one label for each hexadecimal digit, under <c>ip6.arpa</c>.
        /// </summary>
        /// <remarks>
        ///     The name has no trailing dot, matching Python's <c>reverse_pointer</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public string ReversePointer => IPNetworkExtensions.FormatReverseName(IPNetworkExtensions.ToHostNetworkBits(address, nameof(address)));

        /// <summary>
        ///     The version of the Internet Protocol of the address.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public IPVersion Version => IPNetworkExtensions.GetVersionOf(address, nameof(address));

        /// <summary>
        ///     Gets the prefix length of the address, read as a network mask. For example, the prefix length of
        ///     <c>255.255.255.0</c> is 24.
        /// </summary>
        /// <param name="prefixLength">When this method returns <see langword="true" />, the number of leading one bits.</param>
        /// <returns><see langword="true" /> when the address is a valid network mask; otherwise <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the address is neither IPv4 nor IPv6.</exception>
        public bool TryGetPrefixLength(out int prefixLength)
        {
            var version = IPNetworkExtensions.GetVersionOf(address, nameof(address));
            return IPNetworkExtensions.TryGetPrefixLength(address, version, out prefixLength);
        }

        /// <summary>
        ///     Converts an IPv4-mapped IPv6 address, such as <c>::ffff:10.1.2.3</c>, to the IPv4 address that it maps, and returns
        ///     any other address unchanged.
        /// </summary>
        /// <remarks>
        ///     A dual-stack server reports IPv4 clients as IPv4-mapped IPv6 addresses, which IPv4 networks do not contain under
        ///     the subnet tests of this library. Unmapping first lets them match. Unlike <see cref="IPAddress.MapToIPv4" />, which
        ///     turns any IPv6 address into an IPv4 address, such as <c>2001:db8::1</c> into <c>0.0.0.1</c>, this leaves an address
        ///     that is not IPv4-mapped alone.
        /// </remarks>
        /// <returns>The IPv4 address, or this address.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the address is <see langword="null" />.</exception>
        public IPAddress Unmap()
        {
            ArgumentNullException.ThrowIfNull(address);

            return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        }
    }

    extension(IPAddress)
    {
        /// <summary>
        ///     Converts an address from integer form, as returned by <c>AddressBits</c>, to an <see cref="IPAddress" />.
        /// </summary>
        /// <param name="addressBits">The address as an unsigned integer. An IPv4 address must fit in the low 32 bits.</param>
        /// <param name="version">The IP version of the address.</param>
        /// <returns>A new address.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="version" /> is neither IPv4 nor IPv6, or an IPv4 address does not fit in 32 bits.
        /// </exception>
        public static IPAddress FromAddressBits(UInt128 addressBits, IPVersion version)
        {
            if (version is not (IPVersion.IPv4 or IPVersion.IPv6))
                throw new ArgumentOutOfRangeException(nameof(version), version, "Only IPv4 and IPv6 are supported.");

            // an IPv4 address that used the upper bits would be silently cut short, so it is rejected instead
            if (addressBits > AddressMath.GetAllBits(version))
                throw new ArgumentOutOfRangeException(nameof(addressBits), addressBits, $"An {version} address must be no more than {AddressMath.GetAllBits(version)}.");

            return AddressMath.ToIPAddress(addressBits, version);
        }
    }
}
