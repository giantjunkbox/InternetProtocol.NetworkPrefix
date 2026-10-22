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

public static partial class IPNetworkExtensions
{
    /// <param name="network">The network.</param>
    extension(IPNetwork network)
    {
        /// <summary>
        ///     The purpose of the most specific registered block of address space that contains all of this network, such as
        ///     <see cref="AddressAllocation.Private" /> for <c>10.1.0.0/16</c>.
        /// </summary>
        /// <remarks>
        ///     See <see cref="AddressAllocation" /> for how blocks are chosen, and how IPv4-mapped IPv6 networks are classified.
        /// </remarks>
        public AddressAllocation Allocation => GetAllocation(NetworkBits.FromIPNetwork(network));

        /// <summary>
        ///     The historical class of an IPv4 network, such as <see cref="NetworkClass.A" /> for <c>10.0.0.0/8</c>.
        /// </summary>
        /// <remarks>
        ///     An IPv6 network, or an IPv4 network that spans more than one class, such as <c>0.0.0.0/0</c>, is
        ///     <see cref="NetworkClass.Undefined" />. An IPv4-mapped IPv6 network, inside <c>::ffff:0:0/96</c>, takes the class of the
        ///     IPv4 network that it maps.
        /// </remarks>
        public NetworkClass Class => GetClass(NetworkBits.FromIPNetwork(network));
    }

    /// <summary>
    ///     Classifies a network by the purpose of its address space. This is shared by the network and address members.
    /// </summary>
    /// <param name="network">The network, in integer form.</param>
    /// <returns>The allocation of the most specific registered block that contains the network.</returns>
    internal static AddressAllocation GetAllocation(NetworkBits network)
    {
        return FindMostSpecificBlock(ClassificationTables.Allocations, UnmapIPv4(network), AddressAllocation.Undefined);
    }

    /// <summary>
    ///     Classifies a network by its historical IPv4 class. This is shared by the network and address members.
    /// </summary>
    /// <param name="network">The network, in integer form.</param>
    /// <returns>The class of the network.</returns>
    internal static NetworkClass GetClass(NetworkBits network)
    {
        return FindMostSpecificBlock(ClassificationTables.Classes, UnmapIPv4(network), NetworkClass.Undefined);
    }

    /// <summary>
    ///     Finds the value of the most specific block in a table that contains all of a network.
    /// </summary>
    /// <typeparam name="TValue">The type of value that the table holds.</typeparam>
    /// <param name="table">The table, ordered from the most specific block to the least specific.</param>
    /// <param name="network">The network to classify.</param>
    /// <param name="defaultValue">The value to return when no block contains the network.</param>
    /// <returns>The value of the most specific block that contains the network.</returns>
    private static TValue FindMostSpecificBlock<TValue>((NetworkBits Block, TValue Value)[] table, NetworkBits network, TValue defaultValue)
    {
        // the table is ordered from the longest prefix to the shortest, so the first block found is the most specific one
        foreach (var (block, value) in table)
            if (block.IsSupernetOf(network))
                return value;

        return defaultValue;
    }

    /// <summary>
    ///     Converts an IPv4-mapped IPv6 network, inside <c>::ffff:0:0/96</c>, to the IPv4 network that it maps, so that it is
    ///     classified as that IPv4 network. Any other network is returned unchanged.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <returns>The IPv4 network, or <paramref name="network" />.</returns>
    private static NetworkBits UnmapIPv4(NetworkBits network)
    {
        // a network shorter than /96 covers more than the mapped block, so it is not a mapped IPv4 network
        if (network.Version != IPVersion.IPv6 || network.PrefixLength < ClassificationTables.IPv4MappedBlock.PrefixLength || !ClassificationTables.IPv4MappedBlock.IsSupernetOf(network))
            return network;

        // the IPv4 address is the low 32 bits, and the 96 bits of the mapped prefix come off the prefix length
        return new NetworkBits(IPVersion.IPv4, network.BaseAddressBits & uint.MaxValue, network.PrefixLength - ClassificationTables.IPv4MappedBlock.PrefixLength);
    }

    /// <summary>
    ///     The lookup tables behind <c>Allocation</c> and <c>Class</c>.
    /// </summary>
    /// <remarks>
    ///     The tables are in a nested class so that they are built the first time they are used.
    /// </remarks>
    private static class ClassificationTables
    {
        /// <summary>
        ///     The IPv4-mapped IPv6 block, <c>::ffff:0:0/96</c> (RFC 4291).
        /// </summary>
        internal static readonly NetworkBits IPv4MappedBlock = ParseBlock("::ffff:0:0/96");

        /// <summary>
        ///     The purpose of each registered block of address space, from the IANA IPv4 and IPv6 special-purpose address
        ///     registries, as published in 2026.
        /// </summary>
        internal static readonly (NetworkBits Block, AddressAllocation Value)[] Allocations = CreateTable(
        [
            // the IPv4 globally routable unicast space is 0.0.0.0/0 less the multicast, reserved and private blocks. It is listed
            // as the blocks that remain, so that a network that spans a private block is not reported as global. The special
            // purpose blocks below are more specific, so they take precedence over these
            ("0.0.0.0/5", AddressAllocation.Global),
            ("8.0.0.0/7", AddressAllocation.Global),
            ("11.0.0.0/8", AddressAllocation.Global),
            ("12.0.0.0/6", AddressAllocation.Global),
            ("16.0.0.0/4", AddressAllocation.Global),
            ("32.0.0.0/3", AddressAllocation.Global),
            ("64.0.0.0/2", AddressAllocation.Global),
            ("128.0.0.0/3", AddressAllocation.Global),
            ("160.0.0.0/5", AddressAllocation.Global),
            ("168.0.0.0/6", AddressAllocation.Global),
            ("172.0.0.0/12", AddressAllocation.Global),
            ("172.32.0.0/11", AddressAllocation.Global),
            ("172.64.0.0/10", AddressAllocation.Global),
            ("172.128.0.0/9", AddressAllocation.Global),
            ("173.0.0.0/8", AddressAllocation.Global),
            ("174.0.0.0/7", AddressAllocation.Global),
            ("176.0.0.0/4", AddressAllocation.Global),
            ("192.0.0.0/9", AddressAllocation.Global),
            ("192.128.0.0/11", AddressAllocation.Global),
            ("192.160.0.0/13", AddressAllocation.Global),
            ("192.169.0.0/16", AddressAllocation.Global),
            ("192.170.0.0/15", AddressAllocation.Global),
            ("192.172.0.0/14", AddressAllocation.Global),
            ("192.176.0.0/12", AddressAllocation.Global),
            ("192.192.0.0/10", AddressAllocation.Global),
            ("193.0.0.0/8", AddressAllocation.Global),
            ("194.0.0.0/7", AddressAllocation.Global),
            ("196.0.0.0/6", AddressAllocation.Global),
            ("200.0.0.0/5", AddressAllocation.Global),
            ("208.0.0.0/4", AddressAllocation.Global),
            ("2000::/3", AddressAllocation.Global),

            // globally reachable assignments inside blocks that are otherwise not
            ("192.0.0.9/32", AddressAllocation.Global),
            ("192.0.0.10/32", AddressAllocation.Global),
            ("2001:1::1/128", AddressAllocation.Global),
            ("2001:1::2/128", AddressAllocation.Global),
            ("2001:1::3/128", AddressAllocation.Global),
            ("2001:3::/32", AddressAllocation.Global),
            ("2001:4:112::/48", AddressAllocation.Global),
            ("2001:20::/28", AddressAllocation.Global),
            ("2001:30::/28", AddressAllocation.Global),

            ("10.0.0.0/8", AddressAllocation.Private),
            ("172.16.0.0/12", AddressAllocation.Private),
            ("192.168.0.0/16", AddressAllocation.Private),
            ("fc00::/7", AddressAllocation.Private),

            ("169.254.0.0/16", AddressAllocation.LinkLocal),
            ("fe80::/10", AddressAllocation.LinkLocal),

            ("127.0.0.0/8", AddressAllocation.Loopback),
            ("::1/128", AddressAllocation.Loopback),

            ("0.0.0.0/8", AddressAllocation.CurrentNetwork),

            ("0.0.0.0/32", AddressAllocation.Unspecified),
            ("::/128", AddressAllocation.Unspecified),

            ("224.0.0.0/4", AddressAllocation.Multicast),
            ("ff00::/8", AddressAllocation.Multicast),

            ("240.0.0.0/4", AddressAllocation.Experimental),

            ("255.255.255.255/32", AddressAllocation.Broadcast),

            ("100.64.0.0/10", AddressAllocation.SharedAddressSpace),

            ("192.0.2.0/24", AddressAllocation.Documentation),
            ("198.51.100.0/24", AddressAllocation.Documentation),
            ("203.0.113.0/24", AddressAllocation.Documentation),
            ("2001:db8::/32", AddressAllocation.Documentation),
            ("3fff::/20", AddressAllocation.Documentation),

            ("198.18.0.0/15", AddressAllocation.Benchmarking),
            ("2001:2::/48", AddressAllocation.Benchmarking),

            ("192.0.0.0/24", AddressAllocation.ProtocolAssignments),
            ("2001::/23", AddressAllocation.ProtocolAssignments),

            ("64:ff9b::/96", AddressAllocation.IPv4Translation),
            ("64:ff9b:1::/48", AddressAllocation.IPv4Translation),

            ("100::/64", AddressAllocation.Discard),

            ("192.0.0.8/32", AddressAllocation.Dummy),
            ("100:0:0:1::/64", AddressAllocation.Dummy),

            ("2001::/32", AddressAllocation.Teredo),

            ("2002::/16", AddressAllocation.SixToFour),
            ("192.88.99.0/24", AddressAllocation.SixToFour),

            ("fec0::/10", AddressAllocation.SiteLocal),

            ("5f00::/16", AddressAllocation.SegmentRouting)
        ]);

        /// <summary>
        ///     The block of the IPv4 address space that belongs to each historical class.
        /// </summary>
        internal static readonly (NetworkBits Block, NetworkClass Value)[] Classes = CreateTable(
        [
            ("0.0.0.0/1", NetworkClass.A),
            ("128.0.0.0/2", NetworkClass.B),
            ("192.0.0.0/3", NetworkClass.C),
            ("224.0.0.0/4", NetworkClass.D),
            ("240.0.0.0/4", NetworkClass.E)
        ]);

        /// <summary>
        ///     Parses the blocks of a table and orders them from the most specific block to the least specific.
        /// </summary>
        /// <typeparam name="TValue">The type of value that the table holds.</typeparam>
        /// <param name="entries">The blocks, in CIDR notation, and their values.</param>
        /// <returns>The table, ordered by prefix length, longest first.</returns>
        private static (NetworkBits Block, TValue Value)[] CreateTable<TValue>((string Block, TValue Value)[] entries)
        {
            return entries
                .Select(entry => (Block: ParseBlock(entry.Block), entry.Value))
                .OrderByDescending(entry => entry.Block.PrefixLength)
                .ToArray();
        }

        /// <summary>
        ///     Parses a block of a table, which is written in CIDR notation.
        /// </summary>
        /// <param name="blockText">The block, such as <c>10.0.0.0/8</c>.</param>
        /// <returns>The block in integer form.</returns>
        private static NetworkBits ParseBlock(string blockText)
        {
            if (!TryParseCore(blockText, out var block))
                throw new InvalidOperationException($"The classification table entry '{blockText}' is not a valid network.");

            return block;
        }
    }
}
