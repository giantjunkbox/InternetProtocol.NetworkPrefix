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
using System.Text;

namespace Bitvantage.InternetProtocol;

public static partial class IPAddressExtensions
{
    extension(IPAddress)
    {
        /// <summary>
        ///     Parses the text of an IPv4 or IPv6 address, accepting only the standard dotted-decimal form for IPv4.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <see cref="IPAddress.Parse(string)" /> also accepts shorthand such as <c>10.1</c> for <c>10.0.0.1</c>, whole
        ///         numbers such as <c>167772161</c> and hexadecimal such as <c>0x0a.0.0.1</c>, and reads a number with a leading
        ///         zero as octal, so that <c>010.0.0.1</c> is <c>8.0.0.1</c>. This method rejects those forms, because they are
        ///         rarely intended and are a known source of security bugs. Prefer it for text from users, files or other systems.
        ///     </para>
        ///     <para>
        ///         Leading and trailing whitespace is ignored, and an IPv6 scope ID, as in <c>fe80::1%3</c>, is kept.
        ///     </para>
        /// </remarks>
        /// <param name="value">The text to parse.</param>
        /// <returns>The parsed address.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid address.</exception>
        public static IPAddress ParseStrict(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return IPAddress.ParseStrict(value.AsSpan());
        }

        /// <summary>
        ///     Parses the text of an IPv4 or IPv6 address. See <see cref="ParseStrict(string)" /> for the accepted forms.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <returns>The parsed address.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid address.</exception>
        public static IPAddress ParseStrict(ReadOnlySpan<char> value)
        {
            if (TryParseTrimmed(value, out var address))
                return address;

            throw new FormatException($"'{value}' is not a valid IP address. An IPv4 address must be four decimal numbers separated by dots, without leading zeros, such as 10.0.0.1.");
        }

        /// <summary>
        ///     Parses UTF-8 text of an IPv4 or IPv6 address. See <see cref="ParseStrict(string)" /> for the accepted forms.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text to parse.</param>
        /// <returns>The parsed address.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="utf8Text" /> is not a valid address.</exception>
        public static IPAddress ParseStrict(ReadOnlySpan<byte> utf8Text)
        {
            if (IPAddress.TryParseStrict(utf8Text, out var address))
                return address;

            throw new FormatException($"'{Encoding.UTF8.GetString(utf8Text)}' is not a valid IP address. An IPv4 address must be four decimal numbers separated by dots, without leading zeros, such as 10.0.0.1.");
        }

        /// <summary>
        ///     Attempts to parse the text of an IPv4 or IPv6 address. See <see cref="ParseStrict(string)" /> for the accepted forms.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the parsed address.</param>
        /// <returns><see langword="true" /> when the text is a valid address; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseStrict([NotNullWhen(true)] string? value, [NotNullWhen(true)] out IPAddress? result)
        {
            if (value is null)
            {
                result = null;
                return false;
            }

            return IPAddress.TryParseStrict(value.AsSpan(), out result);
        }

        /// <summary>
        ///     Attempts to parse the text of an IPv4 or IPv6 address. See <see cref="ParseStrict(string)" /> for the accepted forms.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the parsed address.</param>
        /// <returns><see langword="true" /> when the text is a valid address; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseStrict(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPAddress? result)
        {
            return TryParseTrimmed(value, out result);
        }

        /// <summary>
        ///     Attempts to parse UTF-8 text of an IPv4 or IPv6 address. See <see cref="ParseStrict(string)" /> for the accepted forms.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the parsed address.</param>
        /// <returns><see langword="true" /> when the text is a valid address; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseStrict(ReadOnlySpan<byte> utf8Text, [NotNullWhen(true)] out IPAddress? result)
        {
            return Utf8Text.TryParse(utf8Text, TryParseTrimmed, out result);
        }
    }

    /// <summary>
    ///     Parses the text of an address, ignoring leading and trailing whitespace.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="address">When this method returns <see langword="true" />, the parsed address.</param>
    /// <returns><see langword="true" /> when the text is a valid address.</returns>
    private static bool TryParseTrimmed(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPAddress? address)
    {
        return StrictAddressParser.TryParse(value.Trim(), out address, out _);
    }
}
