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
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bitvantage.InternetProtocol;

/// <summary>
///     Extension members that add network arithmetic to <see cref="IPNetwork" />: masks, host ranges, subnet and
///     supernet tests, splitting, supernets, exclusion, summarization, address ranges, address planning, sort order,
///     classification, strict parsing, formatting and reverse DNS names.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IPNetwork" /> holds a base address and a prefix length, and can parse a network and test whether an
///         address is in it. These members add the rest. They are C# 14 extension members, so the properties, operators and
///         static members appear on <see cref="IPNetwork" /> itself once the <c>Bitvantage.InternetProtocol</c> namespace is
///         imported.
///     </para>
///     <code>
///     using Bitvantage.InternetProtocol;
///
///     var network = IPNetwork.ParseStrict("10.0.0.0/24");
///     var mask = network.NetworkMask;                   // 255.255.255.0
///     var halves = network.Split();                     // 10.0.0.0/25, 10.0.0.128/25
///     var sorted = networks.Order(IPNetwork.Comparer);
///     </code>
///     <para>
///         The networks that these members return never have an IPv6 scope ID, because a scope ID identifies a network
///         interface rather than part of an address.
///     </para>
///     <para>
///         The subnet, supernet and overlap tests treat IPv4 and IPv6 as separate address spaces, as Go's <c>net/netip</c> does,
///         so an IPv4-mapped IPv6 address such as <c>::ffff:10.1.2.3</c> is not inside <c>10.0.0.0/8</c>. The built-in
///         <see cref="IPNetwork.Contains(IPAddress)" /> does map such an address to IPv4. Use the <c>Unmap</c> extension method of
///         <see cref="IPAddress" /> to compare a mapped address as IPv4.
///     </para>
///     <para>
///         Each member converts its networks to integers on every call. The conversion takes a few nanoseconds and allocates
///         nothing, so the subnet, supernet and overlap tests cost about as much as <see cref="IPNetwork.Contains(IPAddress)" />.
///         Members that return an <see cref="IPAddress" />, such as <c>NetworkMask</c>, create a new address on every call,
///         because an <see cref="IPAddress" /> can be changed and so can't be shared. In a hot loop, use the integer members
///         <c>BaseAddressBits</c>, <c>NetworkMaskBits</c> and <c>HostMaskBits</c> instead.
///     </para>
/// </remarks>
public static partial class IPNetworkExtensions
{
    /// <param name="network">The network.</param>
    extension(IPNetwork network)
    {
        /// <summary>
        ///     The number of bits in an address of the network's IP version: 32 for IPv4, or 128 for IPv6.
        /// </summary>
        public int AddressBitLength => NetworkBits.FromIPNetwork(network).AddressBitLength;

        /// <summary>
        ///     The base address, which is the network address, as an unsigned integer whose most significant bit is the first
        ///     bit of the address. IPv4 addresses occupy the low 32 bits.
        /// </summary>
        public UInt128 BaseAddressBits => NetworkBits.FromIPNetwork(network).BaseAddressBits;

        /// <summary>
        ///     The last address in the network, in which every host bit is set. For example, <c>10.0.0.255</c> for <c>10.0.0.0/24</c>.
        /// </summary>
        /// <remarks>
        ///     IPv6 has no broadcast address. For an IPv6 network this is simply the last address, the same as
        ///     <c>LastAddress</c>, and it can be assigned to a host.
        /// </remarks>
        public IPAddress BroadcastAddress => network.LastAddress;

        /// <summary>
        ///     The first address in the network that can be assigned to a host.
        /// </summary>
        /// <remarks>
        ///     This is the address after the network address. For IPv4 the network address identifies the network, and for IPv6
        ///     it is the subnet-router anycast address (RFC 4291). The exceptions are a /31 or /127 point-to-point network
        ///     (RFC 3021 and RFC 6164), where both addresses are usable, and a host network, which holds just one address.
        /// </remarks>
        public IPAddress FirstHost
        {
            get
            {
                var networkBits = NetworkBits.FromIPNetwork(network);
                return networkBits.ToIPAddress(networkBits.FirstHostBits);
            }
        }

        /// <summary>
        ///     The number of bits of an address that identify a host within the network: <c>AddressBitLength</c>
        ///     minus <see cref="IPNetwork.PrefixLength" />.
        /// </summary>
        public int HostBitLength => NetworkBits.FromIPNetwork(network).HostBitLength;

        /// <summary>
        ///     The host mask, which has a one in every bit that identifies a host. For example, <c>0.0.0.255</c> for a /24.
        /// </summary>
        /// <remarks>
        ///     The host mask is the inverse of the <c>NetworkMask</c>, and Cisco access control lists call it a
        ///     wildcard mask.
        /// </remarks>
        public IPAddress HostMask
        {
            get
            {
                var networkBits = NetworkBits.FromIPNetwork(network);
                return networkBits.ToIPAddress(networkBits.HostMaskBits);
            }
        }

        /// <summary>
        ///     The host mask in integer form. See <c>HostMask</c>.
        /// </summary>
        public UInt128 HostMaskBits => NetworkBits.FromIPNetwork(network).HostMaskBits;

        /// <summary>
        ///     Gets whether this is a host network, a /32 or a /128, which holds a single address.
        /// </summary>
        public bool IsHostNetwork => NetworkBits.FromIPNetwork(network).IsHostNetwork;

        /// <summary>
        ///     The last address in the network, in which every host bit is set. For example, <c>10.0.0.255</c> for <c>10.0.0.0/24</c>.
        /// </summary>
        public IPAddress LastAddress
        {
            get
            {
                var networkBits = NetworkBits.FromIPNetwork(network);
                return networkBits.ToIPAddress(networkBits.LastAddressBits);
            }
        }

        /// <summary>
        ///     The last address in the network that can be assigned to a host.
        /// </summary>
        /// <remarks>
        ///     For IPv4 this is the address before the broadcast address, except for a /31 point-to-point network (RFC 3021), where
        ///     both addresses are usable, and a /32 host network. IPv6 has no broadcast address, so for IPv6 this is the last
        ///     address.
        /// </remarks>
        public IPAddress LastHost
        {
            get
            {
                var networkBits = NetworkBits.FromIPNetwork(network);
                return networkBits.ToIPAddress(networkBits.LastHostBits);
            }
        }

        /// <summary>
        ///     The network mask, also called the subnet mask, which has a one in every bit that identifies the network. For
        ///     example, <c>255.255.255.0</c> for a /24.
        /// </summary>
        public IPAddress NetworkMask
        {
            get
            {
                var networkBits = NetworkBits.FromIPNetwork(network);
                return networkBits.ToIPAddress(networkBits.NetworkMaskBits);
            }
        }

        /// <summary>
        ///     The network mask in integer form. See <c>NetworkMask</c>.
        /// </summary>
        public UInt128 NetworkMaskBits => NetworkBits.FromIPNetwork(network).NetworkMaskBits;

        /// <summary>
        ///     The total number of addresses in the network, including the network and broadcast addresses.
        /// </summary>
        /// <remarks>
        ///     The count is a <see cref="BigInteger" /> because the 2^128 addresses of <c>::/0</c> do not fit in a <see cref="UInt128" />.
        /// </remarks>
        public BigInteger TotalAddresses => (BigInteger)NetworkBits.FromIPNetwork(network).HostMaskBits + BigInteger.One;

        /// <summary>
        ///     The number of addresses in the network that can be assigned to a host, from <c>FirstHost</c> to
        ///     <c>LastHost</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For IPv4 this excludes the network and broadcast addresses. For IPv6 it excludes only the subnet-router anycast
        ///         address, since IPv6 has no broadcast address. A /31 or /127 point-to-point network and a host network have no
        ///         excluded addresses.
        ///     </para>
        ///     <para>
        ///         Every host count fits in a <see cref="UInt128" />, which needs no allocation. A <see cref="UInt128" /> converts
        ///         to <see cref="BigInteger" /> implicitly, so this combines with <c>TotalAddresses</c> without a cast.
        ///     </para>
        /// </remarks>
        public UInt128 TotalHosts => NetworkBits.FromIPNetwork(network).TotalHosts;

        /// <summary>
        ///     The version of the Internet Protocol of the network.
        /// </summary>
        public IPVersion Version => NetworkBits.FromIPNetwork(network).Version;

        /// <summary>
        ///     Determines whether this network is a proper subnet of another network: a smaller network inside the other, and not
        ///     equal to it.
        /// </summary>
        /// <remarks>
        ///     The names follow <see cref="ISet{T}.IsProperSubsetOf" />. A network of the other IP version is never a subnet.
        /// </remarks>
        /// <param name="other">The network that might contain this one.</param>
        /// <returns><see langword="true" /> when <paramref name="other" /> contains this network and is not equal to it.</returns>
        public bool IsProperSubnetOf(IPNetwork other)
        {
            // a proper subnet has a longer prefix, which rules out the equal case
            return network.PrefixLength > other.PrefixLength && IsWithin(network, other);
        }

        /// <summary>
        ///     Determines whether this network is a proper supernet of another network: the other is a smaller network inside this
        ///     one, and not equal to it.
        /// </summary>
        /// <remarks>
        ///     The names follow <see cref="ISet{T}.IsProperSupersetOf" />. A network of the other IP version, including an
        ///     IPv4-mapped IPv6 network, is never a subnet; see <c>IsSupernetOf</c>.
        /// </remarks>
        /// <param name="other">The network that might be inside this one.</param>
        /// <returns><see langword="true" /> when this network contains <paramref name="other" /> and is not equal to it.</returns>
        public bool IsProperSupernetOf(IPNetwork other)
        {
            // a proper subnet has a longer prefix, which rules out the equal case
            return other.PrefixLength > network.PrefixLength && IsWithin(other, network);
        }

        /// <summary>
        ///     Determines whether this network is a subnet of another network: every address of this network is in the other. A
        ///     network is a subnet of itself.
        /// </summary>
        /// <remarks>
        ///     The names follow <see cref="ISet{T}.IsSubsetOf" />. A network of the other IP version is never a subnet.
        /// </remarks>
        /// <param name="other">The network that might contain this one.</param>
        /// <returns><see langword="true" /> when every address of this network is in <paramref name="other" />.</returns>
        public bool IsSubnetOf(IPNetwork other)
        {
            return IsWithin(network, other);
        }

        /// <summary>
        ///     Determines whether this network is a supernet of another network: every address of the other is in this network. A
        ///     network is a supernet of itself.
        /// </summary>
        /// <remarks>
        ///     The names follow <see cref="ISet{T}.IsSupersetOf" />. A network of the other IP version is never a subnet, and that
        ///     includes an IPv4-mapped IPv6 network such as <c>::ffff:10.0.0.0/104</c>, which is not a subnet of <c>10.0.0.0/8</c>.
        ///     The built-in <see cref="IPNetwork.Contains(IPAddress)" /> differs here: it does treat <c>::ffff:10.1.2.3</c> as a
        ///     member of <c>10.0.0.0/8</c>. Convert an address with <c>Unmap</c> to compare it as IPv4.
        /// </remarks>
        /// <param name="other">The network that might be inside this one.</param>
        /// <returns><see langword="true" /> when every address of <paramref name="other" /> is in this network.</returns>
        public bool IsSupernetOf(IPNetwork other)
        {
            return IsWithin(other, network);
        }

        /// <summary>
        ///     Determines whether this network and another network share at least one address, which happens when one is a subnet
        ///     of the other.
        /// </summary>
        /// <remarks>
        ///     The name follows <see cref="ISet{T}.Overlaps" />.
        /// </remarks>
        /// <param name="other">The other network.</param>
        /// <returns><see langword="true" /> when the two networks overlap.</returns>
        public bool Overlaps(IPNetwork other)
        {
            // two networks can't partly overlap; either they are disjoint, or one of them lies entirely within the other. Only
            // the network with the longer prefix can lie within the other, so only one of these tests converts any addresses
            return IsWithin(network, other) || IsWithin(other, network);
        }
    }

    extension(IPNetwork)
    {
        /// <summary>
        ///     The network that holds every IPv4 address, <c>0.0.0.0/0</c>, which access control lists call "any". The name follows
        ///     <see cref="IPAddress.Any" />, which is also IPv4.
        /// </summary>
        public static IPNetwork Any => new(IPAddress.Any, 0);

        /// <summary>
        ///     The network that holds every IPv6 address, <c>::/0</c>. The name follows <see cref="IPAddress.IPv6Any" />.
        /// </summary>
        public static IPNetwork IPv6Any => new(IPAddress.IPv6Any, 0);
    }

    /// <summary>
    ///     Determines whether every address of one network is in another. This is the test behind the subnet, supernet and
    ///     overlap members, which are often called in loops.
    /// </summary>
    /// <param name="innerNetwork">The network that might be inside the other.</param>
    /// <param name="outerNetwork">The network that might contain the other.</param>
    /// <returns><see langword="true" /> when <paramref name="innerNetwork" /> is a subnet of <paramref name="outerNetwork" />, or equal to it.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWithin(IPNetwork innerNetwork, IPNetwork outerNetwork)
    {
        var innerAddress = innerNetwork.BaseAddress;
        var outerAddress = outerNetwork.BaseAddress;

        // networks of different IP versions share no addresses, and a network can't fit inside a smaller one. Both cases are
        // answered without converting either address
        if (innerAddress.AddressFamily != outerAddress.AddressFamily || innerNetwork.PrefixLength < outerNetwork.PrefixLength)
            return false;

        // the inner network is inside the outer one when its address, cut down to the outer network's prefix length, is the outer
        // network's address. IPNetwork always clears the host bits of its base address, so the outer address needs no masking
        var version = AddressMath.GetVersion(outerAddress.AddressFamily);
        var outerNetworkMaskBits = AddressMath.GetNetworkMaskBits(version, outerNetwork.PrefixLength);

        return (AddressMath.FromIPAddress(innerAddress) & outerNetworkMaskBits) == AddressMath.FromIPAddress(outerAddress);
    }
}
