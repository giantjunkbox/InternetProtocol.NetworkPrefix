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

namespace Bitvantage.InternetProtocol;

/// <summary>
///     The purpose that a block of the IPv4 or IPv6 address space is set aside for.
/// </summary>
/// <remarks>
///     <para>
///         The blocks come from the IANA IPv4 and IPv6 special-purpose address registries, and the address space they leave
///         uncovered is <see cref="Global" />: the rest of IPv4, and the rest of the IPv6 global unicast block <c>2000::/3</c>.
///         A network takes the allocation of the most specific block that contains all of it, so a globally reachable block
///         inside a reserved one, such as <c>192.0.0.9/32</c> inside <c>192.0.0.0/24</c>, is <see cref="Global" />.
///     </para>
///     <para>
///         A network that is itself a registered block keeps that block's allocation, so <c>192.0.0.0/24</c> is
///         <see cref="ProtocolAssignments" />. Any other network that spans blocks with different allocations is
///         <see cref="Undefined" />, such as <c>0.0.0.0/1</c>, or <c>126.0.0.0/7</c>, which includes the loopback block
///         <c>127.0.0.0/8</c>. A network is therefore only <see cref="Global" /> when all of it is.
///     </para>
///     <para>
///         An IPv4-mapped IPv6 address or network, inside <c>::ffff:0:0/96</c>, takes the allocation of the IPv4 address or network
///         that it maps, so <c>::ffff:10.0.0.1</c> is <see cref="Private" />. A dual-stack server reports IPv4 clients this way.
///     </para>
/// </remarks>
public enum AddressAllocation
{
    /// <summary>
    ///     No single allocation covers the whole network: it spans blocks with different allocations, or it is IPv6 address
    ///     space outside every registered block and outside <c>2000::/3</c>, which is not yet allocated.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///     Private address space that is not routed on the internet: <c>10.0.0.0/8</c>, <c>172.16.0.0/12</c> and
    ///     <c>192.168.0.0/16</c> (RFC 1918), and the IPv6 unique local addresses <c>fc00::/7</c> (RFC 4193).
    /// </summary>
    Private = 1,

    /// <summary>
    ///     Globally routable unicast address space: IPv4 outside the special-purpose blocks, and <c>2000::/3</c> outside them for
    ///     IPv6, plus the globally reachable blocks that the registries list inside special-purpose blocks.
    /// </summary>
    Global = 2,

    /// <summary>
    ///     Link-local addresses, which are only valid on a single network segment: <c>169.254.0.0/16</c> (RFC 3927), also known
    ///     as automatic private IP addressing, and <c>fe80::/10</c> (RFC 4291).
    /// </summary>
    LinkLocal = 3,

    /// <summary>
    ///     Loopback addresses: <c>127.0.0.0/8</c> and <c>::1/128</c>.
    /// </summary>
    Loopback = 4,

    /// <summary>
    ///     "This network", <c>0.0.0.0/8</c> (RFC 1122).
    /// </summary>
    CurrentNetwork = 5,

    /// <summary>
    ///     Multicast addresses: <c>224.0.0.0/4</c> and <c>ff00::/8</c>.
    /// </summary>
    Multicast = 6,

    /// <summary>
    ///     The reserved, formerly experimental, block <c>240.0.0.0/4</c> (RFC 1112).
    /// </summary>
    Experimental = 7,

    /// <summary>
    ///     The limited broadcast address, <c>255.255.255.255/32</c>.
    /// </summary>
    Broadcast = 8,

    /// <summary>
    ///     Shared address space for carrier-grade NAT, <c>100.64.0.0/10</c> (RFC 6598).
    /// </summary>
    SharedAddressSpace = 9,

    /// <summary>
    ///     Address space reserved for use in documentation and examples: <c>192.0.2.0/24</c>, <c>198.51.100.0/24</c> and
    ///     <c>203.0.113.0/24</c> (RFC 5737), and <c>2001:db8::/32</c> (RFC 3849) and <c>3fff::/20</c> (RFC 9637).
    /// </summary>
    Documentation = 10,

    /// <summary>
    ///     Address space reserved for benchmarking network devices: <c>198.18.0.0/15</c> (RFC 2544) and <c>2001:2::/48</c>
    ///     (RFC 5180).
    /// </summary>
    Benchmarking = 11,

    /// <summary>
    ///     The unspecified address, <c>0.0.0.0/32</c> or <c>::/128</c>, which stands for the absence of an address.
    /// </summary>
    Unspecified = 12,

    /// <summary>
    ///     Address space for IETF protocol assignments: <c>192.0.0.0/24</c> (RFC 6890) and <c>2001::/23</c> (RFC 2928). The
    ///     globally reachable assignments inside them, such as the Port Control Protocol anycast addresses, are <see cref="Global" />.
    /// </summary>
    ProtocolAssignments = 13,

    /// <summary>
    ///     Prefixes for translating between IPv4 and IPv6, used by NAT64: <c>64:ff9b::/96</c> (RFC 6052) and <c>64:ff9b:1::/48</c>
    ///     (RFC 8215).
    /// </summary>
    IPv4Translation = 14,

    /// <summary>
    ///     The discard-only prefix, <c>100::/64</c> (RFC 6666), for traffic that is meant to be dropped.
    /// </summary>
    Discard = 15,

    /// <summary>
    ///     Dummy addresses for hosts that have no other address: <c>192.0.0.8/32</c> (RFC 7600) and <c>100:0:0:1::/64</c> (RFC 9780).
    /// </summary>
    Dummy = 16,

    /// <summary>
    ///     Teredo tunneling of IPv6 over IPv4 UDP, <c>2001::/32</c> (RFC 4380).
    /// </summary>
    Teredo = 17,

    /// <summary>
    ///     6to4 tunneling of IPv6 over IPv4: <c>2002::/16</c> (RFC 3056), and the deprecated relay anycast block
    ///     <c>192.88.99.0/24</c> (RFC 7526).
    /// </summary>
    SixToFour = 18,

    /// <summary>
    ///     The deprecated IPv6 site-local prefix, <c>fec0::/10</c> (RFC 3879), which unique local addresses replaced.
    /// </summary>
    SiteLocal = 19,

    /// <summary>
    ///     Segment Routing over IPv6 segment identifiers, <c>5f00::/16</c> (RFC 9602).
    /// </summary>
    SegmentRouting = 20
}
