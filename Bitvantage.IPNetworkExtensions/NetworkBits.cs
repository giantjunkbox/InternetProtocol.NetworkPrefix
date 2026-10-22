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
///     A network in integer form: its IP version, its base address as an unsigned integer, and its prefix length.
/// </summary>
/// <remarks>
///     The extension members on <see cref="IPNetwork" /> convert a network to this form, do their arithmetic on the integers,
///     and convert the result back. The host bits of the base address are always clear, so the base address is always the
///     network address.
/// </remarks>
internal readonly record struct NetworkBits : IComparable<NetworkBits>
{
    /// <summary>
    ///     Initializes a network from its parts.
    /// </summary>
    /// <param name="version">The IP version.</param>
    /// <param name="addressBits">An address within the network, in integer form. Any host bits that are set are cleared.</param>
    /// <param name="prefixLength">A prefix length that is valid for <paramref name="version" />.</param>
    public NetworkBits(IPVersion version, UInt128 addressBits, int prefixLength)
    {
        Version = version;
        PrefixLength = prefixLength;

        // clear the host bits so that the value always holds the network address; for example 10.1.2.3/8 becomes 10.0.0.0/8
        BaseAddressBits = addressBits & AddressBits.GetNetworkMaskBits(version, prefixLength);
    }

    /// <summary>
    ///     The number of bits in an address of this network's IP version: 32 for IPv4, or 128 for IPv6.
    /// </summary>
    public int AddressBitLength => AddressBits.GetBitLength(Version);

    /// <summary>
    ///     The base address, which is the network address, in integer form.
    /// </summary>
    public UInt128 BaseAddressBits { get; }

    /// <summary>
    ///     The other half of this network's supernet: the network of the same length whose base address differs only in its last
    ///     network bit. The caller must check that the prefix length is not 0.
    /// </summary>
    public NetworkBits ComplementaryNetwork
    {
        get
        {
            // flip the last network bit, which is the bit immediately above the host bits
            var lastNetworkBit = UInt128.One << HostBitLength;
            return new NetworkBits(Version, BaseAddressBits ^ lastNetworkBit, PrefixLength);
        }
    }

    /// <summary>
    ///     The first address that can be assigned to a host, in integer form.
    /// </summary>
    /// <remarks>
    ///     The network address is not a host address: for IPv4 it identifies the network, and for IPv6 it is the subnet-router
    ///     anycast address (RFC 4291). A /31 or /127 point-to-point network (RFC 3021 and RFC 6164) and a host network have no
    ///     address to leave out.
    /// </remarks>
    public UInt128 FirstHostBits => HostBitLength <= 1 ? BaseAddressBits : BaseAddressBits + UInt128.One;

    /// <summary>
    ///     The number of bits of an address that identify a host within the network.
    /// </summary>
    public int HostBitLength => AddressBitLength - PrefixLength;

    /// <summary>
    ///     The host mask in integer form, which has a one in every bit that identifies a host.
    /// </summary>
    public UInt128 HostMaskBits => AddressBits.GetHostMaskBits(Version, PrefixLength);

    /// <summary>
    ///     Gets whether this is a host network, a /32 or a /128, which holds a single address.
    /// </summary>
    public bool IsHostNetwork => HostBitLength == 0;

    /// <summary>
    ///     The last address in the network, in which every host bit is set, in integer form.
    /// </summary>
    public UInt128 LastAddressBits => BaseAddressBits | HostMaskBits;

    /// <summary>
    ///     The last address that can be assigned to a host, in integer form.
    /// </summary>
    /// <remarks>
    ///     Only an IPv4 network of more than two addresses has a broadcast address to leave out; IPv6 has no broadcast address.
    /// </remarks>
    public UInt128 LastHostBits => HostBitLength <= 1 || Version == IPVersion.IPv6 ? LastAddressBits : LastAddressBits - UInt128.One;

    /// <summary>
    ///     The network mask in integer form, which has a one in every bit that identifies the network.
    /// </summary>
    public UInt128 NetworkMaskBits => AddressBits.GetNetworkMaskBits(Version, PrefixLength);

    /// <summary>
    ///     The number of leading bits of an address that identify the network.
    /// </summary>
    public int PrefixLength { get; }

    /// <summary>
    ///     The number of addresses that can be assigned to a host, from <see cref="FirstHostBits" /> to <see cref="LastHostBits" />.
    /// </summary>
    public UInt128 TotalHosts
    {
        get
        {
            return HostBitLength switch
            {
                // a host network holds one address, and a point-to-point network holds two, all of which are usable
                0 => UInt128.One,
                1 => 2,

                // every address, which is the host mask plus one, less the subnet-router anycast address
                _ when Version == IPVersion.IPv6 => HostMaskBits,

                // every address less the network and broadcast addresses
                _ => HostMaskBits - UInt128.One
            };
        }
    }

    /// <summary>
    ///     The IP version.
    /// </summary>
    public IPVersion Version { get; }

    /// <summary>
    ///     Compares this network with another to determine their sort order: IPv4 before IPv6, then by base address, then the
    ///     bigger network, with the shorter prefix, first. A network therefore always sorts before the networks it contains.
    /// </summary>
    /// <param name="other">The network to compare with.</param>
    /// <returns>A negative number, zero, or a positive number when this network sorts before, the same as, or after <paramref name="other" />.</returns>
    public int CompareTo(NetworkBits other)
    {
        // IPv4 sorts before IPv6
        var versionComparison = ((int)Version).CompareTo((int)other.Version);
        if (versionComparison != 0)
            return versionComparison;

        // then the lower base address sorts first
        var addressComparison = BaseAddressBits.CompareTo(other.BaseAddressBits);
        if (addressComparison != 0)
            return addressComparison;

        // and when the base addresses are the same, the bigger network, with the shorter prefix, sorts first
        return PrefixLength.CompareTo(other.PrefixLength);
    }

    /// <summary>
    ///     Determines whether this network is a supernet of another network: every address of the other is in this network. A
    ///     network is a supernet of itself, and a network of the other IP version is never a subnet.
    /// </summary>
    /// <param name="other">The network that might be contained.</param>
    /// <returns><see langword="true" /> when every address of <paramref name="other" /> is in this network.</returns>
    public bool IsSupernetOf(NetworkBits other)
    {
        if (Version != other.Version)
            return false;

        // a network can't contain a network that is bigger than it is
        if (PrefixLength > other.PrefixLength)
            return false;

        // the other network is inside this one when its address, cut down to this network's prefix length, is this network's address
        return (other.BaseAddressBits & NetworkMaskBits) == BaseAddressBits;
    }

    /// <summary>
    ///     Converts an <see cref="IPNetwork" /> to integer form.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <returns>The network in integer form.</returns>
    public static NetworkBits FromIPNetwork(IPNetwork network)
    {
        var baseAddress = network.BaseAddress;
        var version = AddressBits.GetVersion(baseAddress.AddressFamily);

        return new NetworkBits(version, AddressBits.FromIPAddress(baseAddress), network.PrefixLength);
    }

    /// <summary>
    ///     Converts an address of this network's IP version from integer form to an <see cref="IPAddress" />.
    /// </summary>
    /// <param name="addressBits">The address in integer form.</param>
    /// <returns>A new <see cref="IPAddress" />.</returns>
    public IPAddress ToIPAddress(UInt128 addressBits)
    {
        return AddressBits.ToIPAddress(addressBits, Version);
    }

    /// <summary>
    ///     Converts this network to an <see cref="IPNetwork" />.
    /// </summary>
    /// <returns>A new <see cref="IPNetwork" />.</returns>
    public IPNetwork ToIPNetwork()
    {
        return new IPNetwork(ToIPAddress(BaseAddressBits), PrefixLength);
    }

    /// <summary>
    ///     Returns the network with the same base address and a different prefix length.
    /// </summary>
    /// <param name="prefixLength">A prefix length that is valid for this network's IP version.</param>
    /// <returns>The network, with any bits beyond the new prefix length cleared.</returns>
    public NetworkBits WithPrefixLength(int prefixLength)
    {
        return new NetworkBits(Version, BaseAddressBits, prefixLength);
    }
}
