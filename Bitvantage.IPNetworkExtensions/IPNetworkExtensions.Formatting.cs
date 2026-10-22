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
                NetworkFormat.Detail => FormatDetail(networkBits),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
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
