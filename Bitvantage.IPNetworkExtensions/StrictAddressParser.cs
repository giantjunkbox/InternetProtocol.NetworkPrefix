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
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Bitvantage.InternetProtocol;

/// <summary>
///     Parses the text of an IPv4 or IPv6 address, accepting only the standard dotted-decimal form for IPv4.
/// </summary>
/// <remarks>
///     <see cref="IPAddress" /> also accepts shorthand such as <c>10.1</c> for <c>10.0.0.1</c>, a whole number such as
///     <c>167772161</c>, hexadecimal such as <c>0x0a.0.0.1</c>, and reads a number with a leading zero as octal, so that
///     <c>010.0.0.1</c> is <c>8.0.0.1</c>. Those forms are rarely intended and are a known source of security bugs, so this
///     parser rejects them. Both the network and the address extension members parse through it.
/// </remarks>
internal static class StrictAddressParser
{
    /// <summary>
    ///     The characters of a decimal number.
    /// </summary>
    internal const string Digits = "0123456789";

    /// <summary>
    ///     Determines whether IPv4 address text is in the standard dotted-decimal form: four decimal numbers separated by dots,
    ///     without leading zeros.
    /// </summary>
    /// <param name="addressText">The text of an address that <see cref="IPAddress" /> has parsed as IPv4.</param>
    /// <returns><see langword="true" /> when the text is in the standard form.</returns>
    internal static bool IsDottedDecimal(ReadOnlySpan<char> addressText)
    {
        var numberCount = 0;
        foreach (var numberRange in addressText.Split('.'))
        {
            var numberText = addressText[numberRange];
            numberCount++;

            // each number is one to three digits, and only a lone zero may start with a zero. IPAddress has already checked
            // that each number is no more than 255
            if (numberText.IsEmpty || numberText.Length > 3 || numberText.ContainsAnyExcept(Digits))
                return false;

            if (numberText.Length > 1 && numberText[0] == '0')
                return false;
        }

        return numberCount == 4;
    }

    /// <summary>
    ///     Parses the text of an IPv4 or IPv6 address, accepting only the standard dotted-decimal form for IPv4.
    /// </summary>
    /// <remarks>
    ///     Whitespace is not trimmed. An IPv6 scope ID, as in <c>fe80::1%3</c>, is kept on the address.
    /// </remarks>
    /// <param name="addressText">The text of the address.</param>
    /// <param name="address">When this method returns <see langword="true" />, the address.</param>
    /// <param name="version">When this method returns <see langword="true" />, the IP version of the address.</param>
    /// <returns><see langword="true" /> when the text is a valid address.</returns>
    internal static bool TryParse(ReadOnlySpan<char> addressText, [NotNullWhen(true)] out IPAddress? address, out IPVersion version)
    {
        if (!IPAddress.TryParse(addressText, out address) || !AddressMath.TryGetVersion(address.AddressFamily, out version))
        {
            address = null;
            version = IPVersion.Undefined;
            return false;
        }

        // an IPv4 address embedded in IPv6 text, such as ::ffff:10.0.0.1, is parsed by the stricter IPv6 rules and is not checked here
        if (version == IPVersion.IPv4 && !IsDottedDecimal(addressText))
        {
            address = null;
            version = IPVersion.Undefined;
            return false;
        }

        return true;
    }
}
