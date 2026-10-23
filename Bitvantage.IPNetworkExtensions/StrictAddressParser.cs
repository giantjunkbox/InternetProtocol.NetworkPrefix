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
    ///     The characters of the address part of IPv6 text: hexadecimal digits, colons, and the dots of an embedded IPv4 address.
    /// </summary>
    private const string IPv6AddressCharacters = "0123456789abcdefABCDEF:.";

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
    ///     Whitespace is not trimmed. An IPv6 scope ID, as in <c>fe80::1%3</c>, is kept on the address. IPv6 endpoint text with
    ///     brackets, such as <c>[::1]:80</c>, and a scope ID that can't be read, such as <c>fe80::1%</c>, are rejected.
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

        var isStandardForm = version == IPVersion.IPv4 ? IsDottedDecimal(addressText) : IsPlainIPv6(addressText, address);
        if (!isStandardForm)
        {
            address = null;
            version = IPVersion.Undefined;
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Determines whether IPv6 address text is a plain address, with no brackets or port, and with a scope ID that was read.
    /// </summary>
    /// <remarks>
    ///     <see cref="IPAddress" /> also accepts endpoint text such as <c>[::1]:80</c> and drops the port, and it ignores a scope
    ///     ID that it cannot read, such as an empty one, a second one, or a number too large for 32 bits, and returns the address
    ///     without it.
    /// </remarks>
    /// <param name="addressText">The text of an address that <see cref="IPAddress" /> has parsed as IPv6.</param>
    /// <param name="address">The address that <see cref="IPAddress" /> parsed from the text.</param>
    /// <returns><see langword="true" /> when the text is a plain address.</returns>
    private static bool IsPlainIPv6(ReadOnlySpan<char> addressText, IPAddress address)
    {
        var scopeSeparatorIndex = addressText.IndexOf('%');
        var unscopedText = scopeSeparatorIndex < 0 ? addressText : addressText[..scopeSeparatorIndex];
        if (unscopedText.ContainsAnyExcept(IPv6AddressCharacters))
            return false;

        // an IPv4 address embedded at the end, as in ::ffff:10.0.0.1, follows the same rules as one on its own, so that
        // ::ffff:10.0.0.01 is rejected just as 10.0.0.01 is
        if (unscopedText.Contains('.') && !IsDottedDecimal(unscopedText[(unscopedText.LastIndexOf(':') + 1)..]))
            return false;

        if (scopeSeparatorIndex < 0)
            return true;

        // a scope ID is a number, or the name of a network interface, which is looked up. A scope ID of zero is only genuine
        // when it was written as zero; otherwise IPAddress could not read it
        var scopeText = addressText[(scopeSeparatorIndex + 1)..];
        return address.ScopeId != 0 || (!scopeText.IsEmpty && !scopeText.ContainsAnyExcept('0'));
    }
}
