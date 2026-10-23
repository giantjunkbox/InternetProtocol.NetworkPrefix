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

using System.Globalization;
using System.Net;
using System.Text;

namespace Bitvantage.InternetProtocol;

public static partial class IPNetworkExtensions
{
    /// <param name="network">The network.</param>
    extension(IPNetwork network)
    {
        /// <summary>
        ///     Returns the name of the reverse DNS zone that holds the pointer records for the network, such as
        ///     <c>0.0.10.in-addr.arpa</c> for <c>10.0.0.0/24</c>, or <c>8.b.d.0.1.0.0.2.ip6.arpa</c> for <c>2001:db8::/32</c>.
        /// </summary>
        /// <remarks>
        ///     Reverse DNS names have one label for each byte of an IPv4 address and for each 4 bit digit of an IPv6 address, so a
        ///     network has a zone of its own only when its prefix length is a multiple of 8 for IPv4, or of 4 for IPv6. For the
        ///     pointer name of a single address, use the <c>ReversePointer</c> property of the address.
        /// </remarks>
        /// <returns>The zone name, without a trailing dot.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the prefix length does not end on a label boundary.</exception>
        public string GetReverseZone()
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            var labelBitLength = GetReverseLabelBitLength(networkBits.Version);

            if (networkBits.PrefixLength % labelBitLength != 0)
                throw new InvalidOperationException($"{network} has no reverse DNS zone of its own, because a reverse zone needs a prefix length that is a multiple of {labelBitLength} for {networkBits.Version}. Split it into networks of the next multiple and use theirs.");

            return FormatReverseName(networkBits);
        }

        /// <summary>
        ///     Returns the names of the reverse DNS zones that together hold the pointer records for the network. For example,
        ///     <c>10.0.0.0/22</c> has the four zones <c>0.0.10.in-addr.arpa</c> to <c>3.0.10.in-addr.arpa</c>.
        /// </summary>
        /// <remarks>
        ///     A network whose prefix length is a multiple of 8 for IPv4, or of 4 for IPv6, has a single zone, the one that
        ///     <c>GetReverseZone</c> returns. Any other network is split into the networks of the next longer multiple, which is
        ///     at most 128 zones for IPv4 and 8 for IPv6.
        /// </remarks>
        /// <returns>The zone names, without trailing dots, in ascending order of address.</returns>
        public IReadOnlyList<string> GetReverseZones()
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            var labelBitLength = GetReverseLabelBitLength(networkBits.Version);

            // round the prefix length up to the next label boundary
            var zonePrefixLength = (networkBits.PrefixLength + labelBitLength - 1) / labelBitLength * labelBitLength;

            return EnumerateSubnets(networkBits, zonePrefixLength).Select(FormatReverseName).ToArray();
        }

        /// <summary>
        ///     Writes the network in a chosen format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>The text of the network.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format" /> is not a defined value.</exception>
        public string ToString(NetworkFormat format)
        {
            var networkBits = NetworkBits.FromIPNetwork(network);
            var baseAddress = networkBits.ToIPAddress(networkBits.BaseAddressBits);

            return format switch
            {
                NetworkFormat.AddressAndPrefix => string.Create(CultureInfo.InvariantCulture, $"{baseAddress}/{networkBits.PrefixLength}"),
                NetworkFormat.AddressAndMask => $"{baseAddress} {networkBits.ToIPAddress(networkBits.NetworkMaskBits)}",
                NetworkFormat.AddressAndHostMask => $"{baseAddress} {networkBits.ToIPAddress(networkBits.HostMaskBits)}",
                NetworkFormat.Address => baseAddress.ToString(),
                NetworkFormat.Mask => networkBits.ToIPAddress(networkBits.NetworkMaskBits).ToString(),
                NetworkFormat.HostMask => networkBits.ToIPAddress(networkBits.HostMaskBits).ToString(),
                NetworkFormat.Detail => FormatDetail(networkBits),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
    }

    /// <summary>
    ///     Writes the reverse DNS name of a network whose prefix length ends on a label boundary. For a host network this is the
    ///     pointer name of its address.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <returns>The reverse DNS name, without a trailing dot.</returns>
    internal static string FormatReverseName(NetworkBits network)
    {
        // a reverse name lists the labels of the network part from the last to the first: each byte in decimal under
        // in-addr.arpa for IPv4, and each 4 bit digit in hexadecimal under ip6.arpa for IPv6
        var labelBitLength = GetReverseLabelBitLength(network.Version);
        var labelMask = (UInt128.One << labelBitLength) - UInt128.One;
        var (labelFormat, zoneSuffix) = network.Version == IPVersion.IPv4 ? ("D", "in-addr.arpa") : ("x", "ip6.arpa");

        var nameBuilder = new StringBuilder();
        for (var labelIndex = network.PrefixLength / labelBitLength - 1; labelIndex >= 0; labelIndex--)
        {
            // label 0 is the most significant, so it sits furthest to the left in the address
            var shiftToLabel = network.AddressBitLength - (labelIndex + 1) * labelBitLength;
            var labelValue = (int)((network.BaseAddressBits >> shiftToLabel) & labelMask);

            nameBuilder.Append(labelValue.ToString(labelFormat, CultureInfo.InvariantCulture)).Append('.');
        }

        return nameBuilder.Append(zoneSuffix).ToString();
    }

    /// <summary>
    ///     Gets the number of address bits in each label of a reverse DNS name.
    /// </summary>
    /// <param name="version">The IP version.</param>
    /// <returns>8 for IPv4, which has a label for each byte, or 4 for IPv6, which has a label for each hexadecimal digit.</returns>
    private static int GetReverseLabelBitLength(IPVersion version)
    {
        return version == IPVersion.IPv4 ? 8 : 4;
    }

    /// <summary>
    ///     Writes an address in binary, grouped the same way as the address is written: IPv4 in four groups of 8 bits separated
    ///     by dots, and IPv6 in eight groups of 16 bits separated by colons.
    /// </summary>
    /// <param name="network">The network that the address belongs to, which decides the IP version.</param>
    /// <param name="addressBits">The address in integer form.</param>
    /// <returns>The address in binary.</returns>
    private static string FormatBinary(NetworkBits network, UInt128 addressBits)
    {
        var addressBitLength = network.AddressBitLength;
        var (groupBitLength, groupSeparator) = network.Version == IPVersion.IPv4 ? (8, '.') : (16, ':');

        var binaryBuilder = new StringBuilder(addressBitLength + addressBitLength / groupBitLength);
        for (var bitPosition = addressBitLength - 1; bitPosition >= 0; bitPosition--)
        {
            var isBitSet = ((addressBits >> bitPosition) & UInt128.One) == UInt128.One;
            binaryBuilder.Append(isBitSet ? '1' : '0');

            // a separator follows the last bit of every group except the last
            if (bitPosition % groupBitLength == 0 && bitPosition > 0)
                binaryBuilder.Append(groupSeparator);
        }

        return binaryBuilder.ToString();
    }

    /// <summary>
    ///     Writes the <see cref="NetworkFormat.Detail" /> table.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <returns>One line for each property, with the label, value and binary value in aligned columns.</returns>
    private static string FormatDetail(NetworkBits network)
    {
        var rows = new List<(string Label, string Value, string Binary)>
        {
            ("Address:", network.ToIPAddress(network.BaseAddressBits).ToString(), FormatBinary(network, network.BaseAddressBits)),
            ("Mask:", network.ToIPAddress(network.NetworkMaskBits).ToString(), FormatBinary(network, network.NetworkMaskBits)),
            ("Wildcard:", network.ToIPAddress(network.HostMaskBits).ToString(), FormatBinary(network, network.HostMaskBits))
        };

        // IPv6 has no broadcast address; its last address is a host address, and appears as the last host
        if (network.Version == IPVersion.IPv4)
            rows.Add(("Broadcast:", network.ToIPAddress(network.LastAddressBits).ToString(), FormatBinary(network, network.LastAddressBits)));

        rows.Add(("First Host:", network.ToIPAddress(network.FirstHostBits).ToString(), FormatBinary(network, network.FirstHostBits)));
        rows.Add(("Last Host:", network.ToIPAddress(network.LastHostBits).ToString(), FormatBinary(network, network.LastHostBits)));
        rows.Add(("Hosts/Net:", network.TotalHosts.ToString("N0", CultureInfo.InvariantCulture), string.Empty));

        // pad the label and value columns to their widest entries so that the columns line up
        var labelWidth = rows.Max(row => row.Label.Length);
        var valueWidth = rows.Max(row => row.Value.Length);

        var detailBuilder = new StringBuilder();
        foreach (var (label, value, binary) in rows)
            detailBuilder.AppendLine($"{label.PadRight(labelWidth)} {value.PadRight(valueWidth)}  {binary}".TrimEnd());

        return detailBuilder.ToString();
    }
}
