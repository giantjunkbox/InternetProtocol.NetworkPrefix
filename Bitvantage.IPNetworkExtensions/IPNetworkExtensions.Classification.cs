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
        ///     The purpose of the address space of this network, such as <see cref="AddressAllocation.Private" /> for
        ///     <c>10.1.0.0/16</c>, or <see cref="AddressAllocation.Undefined" /> when it spans blocks with different purposes.
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
    /// <returns>The allocation of the network, as <see cref="AddressAllocation" /> describes.</returns>
    internal static AddressAllocation GetAllocation(NetworkBits network)
    {
        return Classify(ClassificationTables.Allocations, ClassificationTables.UnregisteredAllocations, UnmapIPv4(network), AddressAllocation.Undefined);
    }

    /// <summary>
    ///     Classifies a network by its historical IPv4 class. This is shared by the network and address members.
    /// </summary>
    /// <param name="network">The network, in integer form.</param>
    /// <returns>The class of the network.</returns>
    internal static NetworkClass GetClass(NetworkBits network)
    {
        return Classify(ClassificationTables.Classes, [], UnmapIPv4(network), NetworkClass.Undefined);
    }

    /// <summary>
    ///     Classifies a network with tables of blocks, some of which may lie inside others.
    /// </summary>
    /// <remarks>
    ///     A network that is itself a registered block takes that block's value, even when smaller blocks with other values lie
    ///     inside it. Any other network takes the value that all of its addresses share, or <paramref name="undefinedValue" />
    ///     when they don't share one. An address takes the value of the most specific registered block that contains it, or
    ///     else of the unregistered block that contains it, or else <paramref name="undefinedValue" />.
    /// </remarks>
    /// <typeparam name="TValue">The type of value that the tables hold.</typeparam>
    /// <param name="registeredBlocks">The registered blocks, ordered from the longest prefix to the shortest.</param>
    /// <param name="unregisteredBlocks">
    ///     The blocks that give a value to address space that no registered block covers, ordered from the longest prefix to the
    ///     shortest. A network that is exactly one of these is not a registered block.
    /// </param>
    /// <param name="network">The network to classify.</param>
    /// <param name="undefinedValue">The value of a network that no block covers, or whose addresses don't share a value.</param>
    /// <returns>The value of the network.</returns>
    private static TValue Classify<TValue>((NetworkBits Block, TValue Value)[] registeredBlocks, (NetworkBits Block, TValue Value)[] unregisteredBlocks, NetworkBits network, TValue undefinedValue)
        where TValue : struct, Enum
    {
        var containingValue = undefinedValue;
        var foundContainingBlock = false;
        var hasInnerBlock = false;

        // the tables are ordered from the longest prefix to the shortest, so the blocks that may lie inside the network come
        // before the blocks that may contain it, and the first block found that contains it is the most specific one
        foreach (var (block, value) in registeredBlocks)
        {
            if (block.PrefixLength > network.PrefixLength)
            {
                hasInnerBlock |= network.IsSupernetOf(block);
            }
            else if (block.IsSupernetOf(network))
            {
                // a registered block keeps its own value, whatever lies inside it
                if (block.PrefixLength == network.PrefixLength)
                    return value;

                containingValue = value;
                foundContainingBlock = true;
                break;
            }
        }

        foreach (var (block, value) in unregisteredBlocks)
        {
            if (block.PrefixLength > network.PrefixLength)
            {
                hasInnerBlock |= network.IsSupernetOf(block);
            }
            else if (!foundContainingBlock && block.IsSupernetOf(network))
            {
                containingValue = value;
                foundContainingBlock = true;
            }
        }

        // with no smaller block inside it, every address of the network has the value of the block that contains it
        if (!hasInnerBlock)
            return containingValue;

        // the value of an address can only change where a block inside the network starts, or just after one ends, so the
        // network has a single value when the addresses at those points all share the value of its first address
        var firstAddressValue = ClassifyAddress(network.BaseAddressBits);
        return InnerBlocksAgree(registeredBlocks) && InnerBlocksAgree(unregisteredBlocks) ? firstAddressValue : undefinedValue;

        bool InnerBlocksAgree((NetworkBits Block, TValue Value)[] blocks)
        {
            foreach (var (block, _) in blocks)
            {
                if (block.PrefixLength <= network.PrefixLength || !network.IsSupernetOf(block))
                    continue;

                if (!EqualityComparer<TValue>.Default.Equals(ClassifyAddress(block.BaseAddressBits), firstAddressValue))
                    return false;

                if (block.LastAddressBits < network.LastAddressBits && !EqualityComparer<TValue>.Default.Equals(ClassifyAddress(block.LastAddressBits + 1), firstAddressValue))
                    return false;
            }

            return true;
        }

        // a host network has no blocks inside it, so classifying one never comes back here
        TValue ClassifyAddress(UInt128 addressBits)
        {
            return Classify(registeredBlocks, unregisteredBlocks, new NetworkBits(network.Version, addressBits, network.AddressBitLength), undefinedValue);
        }
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
        ///     The allocation of address space that no registered block covers: all of IPv4 is global unicast space, and so is
        ///     the IPv6 global unicast block <c>2000::/3</c> (RFC 4291). The rest of IPv6 is not allocated.
        /// </summary>
        internal static readonly (NetworkBits Block, AddressAllocation Value)[] UnregisteredAllocations = CreateTable(
        [
            ("0.0.0.0/0", AddressAllocation.Global),
            ("2000::/3", AddressAllocation.Global)
        ]);

        /// <summary>
        ///     The purpose of each registered block of address space, from the IANA IPv4 and IPv6 special-purpose address
        ///     registries, as published in 2026. The address space that they leave uncovered is in
        ///     <see cref="UnregisteredAllocations" />.
        /// </summary>
        internal static readonly (NetworkBits Block, AddressAllocation Value)[] Allocations = CreateTable(
        [
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
