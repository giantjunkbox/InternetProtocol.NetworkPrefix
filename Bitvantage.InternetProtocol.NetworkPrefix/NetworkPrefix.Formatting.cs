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

using System.Globalization;
using System.Text;

namespace Bitvantage.InternetProtocol;

public sealed partial class NetworkPrefix : IFormattable
{
    /// <summary>
    ///     Writes the prefix in CIDR notation, such as <c>10.0.0.0/24</c>.
    /// </summary>
    /// <returns>The text of the prefix.</returns>
    public override string ToString()
    {
        return ToString(NetworkFormat.AddressAndPrefix);
    }

    /// <summary>
    ///     Writes the prefix in a chosen format.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The text of the prefix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format" /> is not a defined value.</exception>
    public string ToString(NetworkFormat format)
    {
        return format switch
        {
            NetworkFormat.AddressAndPrefix => string.Create(CultureInfo.InvariantCulture, $"{NetworkAddress}/{PrefixLength}"),
            NetworkFormat.AddressAndMask => $"{NetworkAddress} {NetworkMask}",
            NetworkFormat.AddressAndHostMask => $"{NetworkAddress} {HostMask}",
            NetworkFormat.Address => NetworkAddress.ToString(),
            NetworkFormat.Mask => NetworkMask.ToString(),
            NetworkFormat.Detail => FormatDetail(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    /// <summary>
    ///     Writes the prefix in the format named by a format string, so that a prefix can be formatted in an interpolated string,
    ///     as in <c>$"{prefix:AddressAndMask}"</c>.
    /// </summary>
    /// <param name="format">
    ///     <see langword="null" />, an empty string, or <c>G</c> for CIDR notation; otherwise the name of a <see cref="NetworkFormat" />
    ///     value, ignoring case.
    /// </param>
    /// <param name="formatProvider">Ignored; the format of a prefix does not depend on culture.</param>
    /// <returns>The text of the prefix.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        // the general format, which is what an interpolated string asks for when no format is given, is CIDR notation
        if (string.IsNullOrEmpty(format) || format is "G" or "g")
            return ToString();

        if (Enum.TryParse<NetworkFormat>(format, true, out var networkFormat) && Enum.IsDefined(networkFormat))
            return ToString(networkFormat);

        throw new FormatException($"'{format}' is not a supported format. Use 'G', or the name of a {nameof(NetworkFormat)} value such as '{nameof(NetworkFormat.AddressAndMask)}'.");
    }

    /// <summary>
    ///     Writes an address of this prefix's IP version in binary, grouped the same way as the address is written: IPv4 in four
    ///     groups of 8 bits separated by dots, and IPv6 in eight groups of 16 bits separated by colons.
    /// </summary>
    /// <param name="addressBits">The address in integer form.</param>
    /// <returns>The address in binary.</returns>
    private string FormatBinary(UInt128 addressBits)
    {
        var (groupBitLength, groupSeparator) = Version == IPVersion.IPv4 ? (8, '.') : (16, ':');

        var binaryBuilder = new StringBuilder(AddressBitLength + AddressBitLength / groupBitLength);
        for (var bitPosition = AddressBitLength - 1; bitPosition >= 0; bitPosition--)
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
    /// <returns>One line for each property, with the label, value and binary value in aligned columns.</returns>
    private string FormatDetail()
    {
        var rows = new List<(string Label, string Value, string Binary)>
        {
            ("Address:", NetworkAddress.ToString(), FormatBinary(NetworkAddressBits)),
            ("Mask:", NetworkMask.ToString(), FormatBinary(NetworkMaskBits)),
            ("Wildcard:", HostMask.ToString(), FormatBinary(HostMaskBits)),
            ("Broadcast:", BroadcastAddress.ToString(), FormatBinary(LastAddressBits)),
            ("First Host:", FirstHost.ToString(), FormatBinary(FirstHostBits)),
            ("Last Host:", LastHost.ToString(), FormatBinary(LastHostBits)),
            ("Hosts/Net:", TotalHosts.ToString("N0", CultureInfo.InvariantCulture), string.Empty)
        };

        // pad the label and value columns to their widest entries so that the columns line up
        var labelWidth = rows.Max(row => row.Label.Length);
        var valueWidth = rows.Max(row => row.Value.Length);

        var detailBuilder = new StringBuilder();
        foreach (var (label, value, binary) in rows)
            detailBuilder.AppendLine($"{label.PadRight(labelWidth)} {value.PadRight(valueWidth)}  {binary}".TrimEnd());

        return detailBuilder.ToString();
    }
}
