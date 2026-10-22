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
///     A network takes the allocation of the most specific registered block that contains all of it. A network that spans blocks
///     with different allocations, such as <c>0.0.0.0/1</c>, is <see cref="Undefined" />.
/// </remarks>
public enum AddressAllocation
{
    /// <summary>
    ///     No single allocation covers the whole prefix.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///     Private address space that is not routed on the internet: <c>10.0.0.0/8</c>, <c>172.16.0.0/12</c> and
    ///     <c>192.168.0.0/16</c> (RFC 1918), and the IPv6 unique local addresses <c>fc00::/7</c> (RFC 4193).
    /// </summary>
    Private = 1,

    /// <summary>
    ///     Globally routable unicast address space.
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
    ///     The reserved, formerly experimental, block <c>240.0.0.0/4</c>.
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
    Unspecified = 12
}
