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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Bitvantage.InternetProtocol;

public sealed partial class NetworkPrefix : ISpanParsable<NetworkPrefix>
{
    /// <summary>
    ///     The characters that may separate an address from a prefix length or a network mask.
    /// </summary>
    private const string SuffixSeparators = "/ \t";

    /// <summary>
    ///     The characters of a prefix length.
    /// </summary>
    private const string Digits = "0123456789";

    /// <summary>
    ///     A description of the supported formats, for exception messages.
    /// </summary>
    private const string SupportedFormats = "The supported formats are an address and prefix length such as 10.0.0.0/24, an address and network mask such as 10.0.0.0 255.255.255.0 or 10.0.0.0/255.255.255.0, and a single address such as 10.0.0.1, which is a host prefix. An IPv4 address must be four decimal numbers separated by dots, without leading zeros.";

    /// <summary>
    ///     Parses the text of a prefix.
    /// </summary>
    /// <remarks>
    ///     <para>The following formats are accepted, with any IPv4 or IPv6 address:</para>
    ///     <list type="bullet">
    ///         <item><description>An address and a prefix length, such as <c>10.0.0.0/24</c> or <c>2001:db8::/32</c>.</description></item>
    ///         <item><description>An address and a network mask, such as <c>10.0.0.0 255.255.255.0</c> or <c>10.0.0.0/255.255.255.0</c>.</description></item>
    ///         <item><description>A single address, such as <c>10.0.0.1</c>, which becomes a host prefix.</description></item>
    ///     </list>
    ///     <para>
    ///         Leading and trailing whitespace is ignored, and any host bits that are set in the address are cleared. Host names
    ///         are not accepted, because looking them up is a network operation; use <see cref="Resolve" /> or
    ///         <see cref="ResolveAsync" /> for those.
    ///     </para>
    ///     <para>
    ///         An IPv4 address or mask must be in the standard form of four decimal numbers separated by dots, without leading
    ///         zeros. <see cref="IPAddress.Parse(string)" /> and <see cref="IPNetwork.Parse(string)" /> also accept older forms,
    ///         and read a number with a leading zero as octal, so that <c>010.0.0.0/8</c> is <c>8.0.0.0/8</c>. Those forms are
    ///         rejected here, because they are rarely intended and are a known source of security bugs.
    ///     </para>
    /// </remarks>
    /// <param name="value">The text to parse.</param>
    /// <returns>The parsed prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid prefix.</exception>
    public static NetworkPrefix Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Parse(value.AsSpan());
    }

    /// <summary>
    ///     Parses the text of a prefix. See <see cref="Parse(string)" /> for the accepted formats.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <returns>The parsed prefix.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid prefix.</exception>
    public static NetworkPrefix Parse(ReadOnlySpan<char> value)
    {
        if (TryParse(value, out var network))
            return network;

        throw new FormatException($"'{value}' is not a valid network prefix. {SupportedFormats}");
    }

    /// <summary>
    ///     Creates a prefix from text that may hold a host name in place of an address, such as <c>example.com/24</c>, looking
    ///     the host name up in DNS.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Text that <see cref="TryParse(string, out NetworkPrefix)" /> accepts is parsed without a lookup. Otherwise the text
    ///         before the prefix length or network mask is looked up as a host name, and the first address returned is used.
    ///     </para>
    ///     <para>
    ///         When <paramref name="addressFamily" /> is <see cref="AddressFamily.Unspecified" /> and the text has a network mask,
    ///         only addresses of the mask's IP version are looked up.
    ///     </para>
    /// </remarks>
    /// <param name="value">The text, such as <c>example.com</c>, <c>example.com/24</c> or <c>example.com 255.255.255.0</c>.</param>
    /// <param name="addressFamily">
    ///     The kind of address to look up: <see cref="AddressFamily.InterNetwork" /> for IPv4,
    ///     <see cref="AddressFamily.InterNetworkV6" /> for IPv6, or <see cref="AddressFamily.Unspecified" /> for either.
    /// </param>
    /// <returns>The prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="addressFamily" /> is not one of the values above.</exception>
    /// <exception cref="FormatException">Thrown when the text is not a valid prefix or host name.</exception>
    /// <exception cref="SocketException">Thrown when the host name can't be resolved.</exception>
    public static NetworkPrefix Resolve(string value, AddressFamily addressFamily = AddressFamily.Unspecified)
    {
        // text that is already a prefix needs no lookup
        if (TryParseBeforeLookup(value, addressFamily, out var network, out var hostName, out var lookupFamily))
            return network;

        var hostAddresses = Dns.GetHostAddresses(hostName, lookupFamily);
        return CreateFromHostAddresses(value, hostAddresses);
    }

    /// <summary>
    ///     Creates a prefix from text that may hold a host name in place of an address, such as <c>example.com/24</c>, looking
    ///     the host name up in DNS asynchronously. See <see cref="Resolve" />.
    /// </summary>
    /// <param name="value">The text, such as <c>example.com</c>, <c>example.com/24</c> or <c>example.com 255.255.255.0</c>.</param>
    /// <param name="addressFamily">
    ///     The kind of address to look up: <see cref="AddressFamily.InterNetwork" /> for IPv4,
    ///     <see cref="AddressFamily.InterNetworkV6" /> for IPv6, or <see cref="AddressFamily.Unspecified" /> for either.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the lookup.</param>
    /// <returns>The prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="addressFamily" /> is not one of the values above.</exception>
    /// <exception cref="FormatException">Thrown when the text is not a valid prefix or host name.</exception>
    /// <exception cref="SocketException">Thrown when the host name can't be resolved.</exception>
    public static async Task<NetworkPrefix> ResolveAsync(string value, AddressFamily addressFamily = AddressFamily.Unspecified, CancellationToken cancellationToken = default)
    {
        // text that is already a prefix needs no lookup
        if (TryParseBeforeLookup(value, addressFamily, out var network, out var hostName, out var lookupFamily))
            return network;

        var hostAddresses = await Dns.GetHostAddressesAsync(hostName, lookupFamily, cancellationToken).ConfigureAwait(false);
        return CreateFromHostAddresses(value, hostAddresses);
    }

    /// <summary>
    ///     Attempts to parse the text of a prefix. See <see cref="Parse(string)" /> for the accepted formats.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed prefix.</param>
    /// <returns><see langword="true" /> when the text is a valid prefix; otherwise <see langword="false" />. This method never throws.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, [NotNullWhen(true)] out NetworkPrefix? result)
    {
        if (value is null)
        {
            result = null;
            return false;
        }

        return TryParse(value.AsSpan(), out result);
    }

    /// <summary>
    ///     Attempts to parse the text of a prefix. See <see cref="Parse(string)" /> for the accepted formats.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed prefix.</param>
    /// <returns><see langword="true" /> when the text is a valid prefix; otherwise <see langword="false" />. This method never throws.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out NetworkPrefix? result)
    {
        result = null;

        if (!TrySplitAddressAndSuffix(value, out var addressText, out var suffixText))
            return false;

        if (!TryParseAddress(addressText, out var address, out var version))
            return false;

        if (!TryParseSuffix(suffixText, version, out var prefixLength))
            return false;

        result = new NetworkPrefix(AddressBits.FromIPAddress(address), prefixLength, version);
        return true;
    }

    /// <inheritdoc cref="Parse(string)" />
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; the format of a prefix does not depend on culture.</param>
    static NetworkPrefix IParsable<NetworkPrefix>.Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <inheritdoc cref="Parse(ReadOnlySpan{char})" />
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; the format of a prefix does not depend on culture.</param>
    static NetworkPrefix ISpanParsable<NetworkPrefix>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <inheritdoc cref="TryParse(string, out NetworkPrefix)" />
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; the format of a prefix does not depend on culture.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed prefix.</param>
    static bool IParsable<NetworkPrefix>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NetworkPrefix result)
    {
        return TryParse(s, out result);
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, out NetworkPrefix)" />
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; the format of a prefix does not depend on culture.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed prefix.</param>
    static bool ISpanParsable<NetworkPrefix>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out NetworkPrefix result)
    {
        return TryParse(s, out result);
    }

    /// <summary>
    ///     Creates a prefix from the addresses that DNS returned for the host name in some text.
    /// </summary>
    /// <param name="value">The text that was looked up, which still holds the prefix length or network mask.</param>
    /// <param name="hostAddresses">The addresses that DNS returned.</param>
    /// <returns>The prefix.</returns>
    /// <exception cref="SocketException">Thrown when DNS returned no addresses.</exception>
    /// <exception cref="FormatException">Thrown when the prefix length or network mask does not suit the address that was found.</exception>
    private static NetworkPrefix CreateFromHostAddresses(string value, IPAddress[] hostAddresses)
    {
        // use the first IPv4 or IPv6 address, in the order of preference that the resolver returned them in
        var hostAddress = hostAddresses.FirstOrDefault(address => AddressBits.TryGetVersion(address.AddressFamily, out _));
        if (hostAddress is null)
            throw new SocketException((int)SocketError.HostNotFound);

        TrySplitAddressAndSuffix(value, out _, out var suffixText);
        var version = AddressBits.GetVersion(hostAddress.AddressFamily);

        // a prefix length that suits one IP version, such as /64, may not suit the address that the lookup returned
        if (!TryParseSuffix(suffixText, version, out var prefixLength))
            throw new FormatException($"'{suffixText}' is not a valid prefix length or network mask for {hostAddress}, the {version} address of '{value}'.");

        return new NetworkPrefix(AddressBits.FromIPAddress(hostAddress), prefixLength, version);
    }

    /// <summary>
    ///     Determines whether IPv4 address text is in the standard dotted-decimal form: four decimal numbers separated by dots,
    ///     without leading zeros.
    /// </summary>
    /// <remarks>
    ///     <see cref="IPAddress" /> also accepts shorthand such as <c>10.1</c> for <c>10.0.0.1</c>, a whole number such as
    ///     <c>167772161</c>, hexadecimal such as <c>0x0a.0.0.1</c>, and reads a number with a leading zero as octal, so that
    ///     <c>010.0.0.1</c> is <c>8.0.0.1</c>. This check rejects all of those. <see cref="IPAddress" /> still checks that each
    ///     number is no more than 255.
    /// </remarks>
    /// <param name="addressText">The text of an address that <see cref="IPAddress" /> has parsed as IPv4.</param>
    /// <returns><see langword="true" /> when the text is in the standard form.</returns>
    private static bool IsDottedDecimal(ReadOnlySpan<char> addressText)
    {
        var numberCount = 0;
        foreach (var numberRange in addressText.Split('.'))
        {
            var numberText = addressText[numberRange];
            numberCount++;

            // each number is one to three digits, and only a lone zero may start with a zero
            if (numberText.IsEmpty || numberText.Length > 3 || numberText.ContainsAnyExcept(Digits))
                return false;

            if (numberText.Length > 1 && numberText[0] == '0')
                return false;
        }

        return numberCount == 4;
    }

    /// <summary>
    ///     Determines whether text is a prefix length, which is made only of digits.
    /// </summary>
    /// <param name="suffixText">The text after the separator.</param>
    /// <returns><see langword="true" /> when the text is made only of digits.</returns>
    private static bool IsPrefixLength(ReadOnlySpan<char> suffixText)
    {
        return !suffixText.IsEmpty && !suffixText.ContainsAnyExcept(Digits);
    }

    /// <summary>
    ///     Parses what follows the address: a prefix length, a network mask, or nothing.
    /// </summary>
    /// <param name="suffixText">The text after the separator, which is empty when there was no separator.</param>
    /// <param name="version">The IP version of the address.</param>
    /// <param name="prefixLength">When this method returns <see langword="true" />, the prefix length.</param>
    /// <returns><see langword="true" /> when the suffix is valid for the IP version.</returns>
    private static bool TryParseSuffix(ReadOnlySpan<char> suffixText, IPVersion version, out int prefixLength)
    {
        var addressBitLength = AddressBits.GetBitLength(version);

        // with no suffix the prefix holds a single host
        if (suffixText.IsEmpty)
        {
            prefixLength = addressBitLength;
            return true;
        }

        // text made only of digits is a prefix length. It is never treated as a mask, even though IPAddress would accept a
        // number such as 4294967295 as an IPv4 address. NumberStyles.None rejects signs, whitespace and group separators
        if (IsPrefixLength(suffixText))
            return int.TryParse(suffixText, NumberStyles.None, CultureInfo.InvariantCulture, out prefixLength) && prefixLength <= addressBitLength;

        // anything else must be a network mask of the same IP version as the address
        prefixLength = 0;
        return TryParseAddress(suffixText, out var networkMask, out _) && TryGetPrefixLength(networkMask, version, out prefixLength);
    }

    /// <summary>
    ///     Parses the text of an IPv4 or IPv6 address, accepting only the standard dotted-decimal form for IPv4.
    /// </summary>
    /// <param name="addressText">The text of the address.</param>
    /// <param name="address">When this method returns <see langword="true" />, the address.</param>
    /// <param name="version">When this method returns <see langword="true" />, the IP version of the address.</param>
    /// <returns><see langword="true" /> when the text is a valid address.</returns>
    private static bool TryParseAddress(ReadOnlySpan<char> addressText, [NotNullWhen(true)] out IPAddress? address, out IPVersion version)
    {
        if (!IPAddress.TryParse(addressText, out address) || !AddressBits.TryGetVersion(address.AddressFamily, out version))
        {
            address = null;
            version = IPVersion.Undefined;
            return false;
        }

        // an IPv4 address embedded in IPv6 text, such as ::ffff:10.0.0.1, is parsed by the stricter IPv6 rules and is not checked here
        if (version == IPVersion.IPv4 && !IsDottedDecimal(addressText))
        {
            address = null;
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Validates the arguments of <see cref="Resolve" /> and <see cref="ResolveAsync" />, parses text that needs no lookup, and
    ///     otherwise finds the host name and address family to look up.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="addressFamily">The address family that the caller asked for.</param>
    /// <param name="network">When this method returns <see langword="true" />, the parsed prefix.</param>
    /// <param name="hostName">When this method returns <see langword="false" />, the host name to look up.</param>
    /// <param name="lookupFamily">When this method returns <see langword="false" />, the address family to look up.</param>
    /// <returns><see langword="true" /> when the text is a prefix and needs no lookup.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="addressFamily" /> is not IPv4, IPv6 or unspecified.</exception>
    /// <exception cref="FormatException">Thrown when the text is neither a prefix nor a host name with a valid suffix.</exception>
    private static bool TryParseBeforeLookup(string value, AddressFamily addressFamily, [NotNullWhen(true)] out NetworkPrefix? network, out string hostName, out AddressFamily lookupFamily)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (addressFamily is not (AddressFamily.Unspecified or AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            throw new ArgumentOutOfRangeException(nameof(addressFamily), addressFamily, "Only IPv4, IPv6 or unspecified are supported.");

        hostName = string.Empty;
        lookupFamily = addressFamily;

        if (TryParse(value, out network))
            return true;

        if (!TrySplitAddressAndSuffix(value, out var hostText, out var suffixText) || hostText.IsEmpty)
            throw new FormatException($"'{value}' is not a valid network prefix or host name.");

        // text that IPAddress reads as an address, in any of its forms, is a malformed prefix rather than a host name to look
        // up. That covers an invalid suffix, as in 10.0.0.0/33, and an address in a rejected form, as in 010.0.0.0/8, which a
        // resolver would otherwise read as an address in its own way
        if (IPAddress.TryParse(hostText, out _))
            throw new FormatException($"'{value}' is not a valid network prefix. {SupportedFormats}");

        // a network mask decides which IP version to look up, unless the caller already chose one
        if (lookupFamily == AddressFamily.Unspecified && !suffixText.IsEmpty && !IsPrefixLength(suffixText) && IPAddress.TryParse(suffixText, out var networkMask))
            lookupFamily = networkMask.AddressFamily;

        hostName = hostText.ToString();
        return false;
    }

    /// <summary>
    ///     Splits the text of a prefix into the address and whatever follows it.
    /// </summary>
    /// <param name="value">The text, such as <c>10.0.0.0/24</c>, <c>10.0.0.0 255.255.255.0</c> or <c>10.0.0.1</c>.</param>
    /// <param name="addressText">When this method returns <see langword="true" />, the text before the separator.</param>
    /// <param name="suffixText">
    ///     When this method returns <see langword="true" />, the text after the separator, or empty when there is no separator.
    /// </param>
    /// <returns><see langword="false" /> when a separator is not followed by anything.</returns>
    private static bool TrySplitAddressAndSuffix(ReadOnlySpan<char> value, out ReadOnlySpan<char> addressText, out ReadOnlySpan<char> suffixText)
    {
        var trimmedValue = value.Trim();

        // the address ends at the first slash or whitespace character
        var separatorIndex = trimmedValue.IndexOfAny(SuffixSeparators);
        if (separatorIndex < 0)
        {
            addressText = trimmedValue;
            suffixText = ReadOnlySpan<char>.Empty;
            return true;
        }

        addressText = trimmedValue[..separatorIndex];

        // more than one space may separate an address from its mask, so whitespace after the separator is skipped
        suffixText = trimmedValue[(separatorIndex + 1)..].TrimStart();
        return !suffixText.IsEmpty;
    }
}
