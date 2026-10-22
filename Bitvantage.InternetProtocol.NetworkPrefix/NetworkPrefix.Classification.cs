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

namespace Bitvantage.InternetProtocol;

public sealed partial class NetworkPrefix
{
    /// <summary>
    ///     The purpose of the most specific registered block of address space that contains all of this prefix, such as
    ///     <see cref="AddressAllocation.Private" /> for <c>10.1.0.0/16</c>.
    /// </summary>
    /// <remarks>
    ///     A prefix that spans blocks with different purposes, such as <c>0.0.0.0/1</c>, is <see cref="AddressAllocation.Undefined" />.
    /// </remarks>
    public AddressAllocation Allocation => FindMostSpecificBlock(ClassificationTables.Allocations, AddressAllocation.Undefined);

    /// <summary>
    ///     The historical class of an IPv4 prefix, such as <see cref="NetworkClass.A" /> for <c>10.0.0.0/8</c>.
    /// </summary>
    /// <remarks>
    ///     An IPv6 prefix, or an IPv4 prefix that spans more than one class, such as <c>0.0.0.0/0</c>, is
    ///     <see cref="NetworkClass.Undefined" />.
    /// </remarks>
    public NetworkClass Class => FindMostSpecificBlock(ClassificationTables.Classes, NetworkClass.Undefined);

    /// <summary>
    ///     Finds the value of the most specific block in a table that contains all of this prefix.
    /// </summary>
    /// <typeparam name="TValue">The type of value that the table holds.</typeparam>
    /// <param name="table">The table, ordered from the most specific block to the least specific.</param>
    /// <param name="defaultValue">The value to return when no block contains this prefix.</param>
    /// <returns>The value of the most specific block that contains this prefix.</returns>
    private TValue FindMostSpecificBlock<TValue>((NetworkPrefix Block, TValue Value)[] table, TValue defaultValue)
    {
        // the table is ordered from the longest prefix to the shortest, so the first block found is the most specific one
        foreach (var (block, value) in table)
            if (block.ContainsOrEqual(this))
                return value;

        return defaultValue;
    }

    /// <summary>
    ///     The lookup tables behind <see cref="Allocation" /> and <see cref="Class" />.
    /// </summary>
    /// <remarks>
    ///     The tables are in a nested class so that they are built the first time they are used, after everything that building a
    ///     <see cref="NetworkPrefix" /> depends on.
    /// </remarks>
    private static class ClassificationTables
    {
        /// <summary>
        ///     The purpose of each registered block of address space, from the IANA IPv4 and IPv6 special-purpose address registries.
        /// </summary>
        internal static readonly (NetworkPrefix Block, AddressAllocation Value)[] Allocations = CreateTable(
        [
            // the IPv4 globally routable unicast space is 0.0.0.0/0 less the multicast, reserved and private blocks. It is listed
            // as the blocks that remain, so that a prefix that spans a private block is not reported as global
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
            ("2001:2::/48", AddressAllocation.Benchmarking)
        ]);

        /// <summary>
        ///     The block of the IPv4 address space that belongs to each historical class.
        /// </summary>
        internal static readonly (NetworkPrefix Block, NetworkClass Value)[] Classes = CreateTable(
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
        private static (NetworkPrefix Block, TValue Value)[] CreateTable<TValue>((string Block, TValue Value)[] entries)
        {
            return entries
                .Select(entry => (Block: Parse(entry.Block), entry.Value))
                .OrderByDescending(entry => entry.Block.PrefixLength)
                .ToArray();
        }
    }
}
