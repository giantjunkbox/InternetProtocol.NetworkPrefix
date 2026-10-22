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
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bitvantage.InternetProtocol;

public static partial class IPNetworkExtensions
{
    /// <summary>
    ///     The characters that may separate an address from a prefix length or a mask.
    /// </summary>
    private const string SuffixSeparators = "/ \t";

    /// <summary>
    ///     A description of the supported formats, for exception messages.
    /// </summary>
    private const string SupportedFormats = "The supported formats are an address and prefix length such as 10.0.0.0/24, an address and network mask such as 10.0.0.0 255.255.255.0 or 10.0.0.0/255.255.255.0, an address and wildcard mask such as 10.0.0.0 0.0.0.255, and a single address such as 10.0.0.1, which is a host network. An IPv4 address must be four decimal numbers separated by dots, without leading zeros.";

    /// <summary>
    ///     The longest UTF-8 text that is converted to characters on the stack rather than in a rented array. Every valid network
    ///     is far shorter, but text may be padded with whitespace, so longer text is still parsed.
    /// </summary>
    private const int Utf8StackLimit = 256;

    extension(IPNetwork)
    {
        /// <summary>
        ///     Creates the host network, a /32 for IPv4 or a /128 for IPv6, that holds a single address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>The host network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address.</exception>
        public static IPNetwork FromAddress(IPAddress address)
        {
            var version = GetVersionOf(address, nameof(address));

            return new NetworkBits(version, AddressMath.FromIPAddress(address), AddressMath.GetBitLength(version)).ToIPNetwork();
        }

        /// <summary>
        ///     Creates a network from an address and a host mask, also called a wildcard mask, such as <c>10.0.0.0</c> and
        ///     <c>0.0.0.255</c>. This is the form that Cisco access control lists use.
        /// </summary>
        /// <param name="address">An address within the network. Any host bits that are set are cleared.</param>
        /// <param name="hostMask">The host mask, which must be the same IP version as <paramref name="address" />.</param>
        /// <returns>The network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address, or when <paramref name="hostMask" />
        ///     is not a valid host mask of the same IP version.
        /// </exception>
        public static IPNetwork FromHostMask(IPAddress address, IPAddress hostMask)
        {
            var version = GetVersionOf(address, nameof(address));
            ArgumentNullException.ThrowIfNull(hostMask);

            if (!TryGetPrefixLengthFromHostMask(hostMask, version, out var prefixLength))
                throw new ArgumentException($"'{hostMask}' is not a valid {version} host mask. A host mask is a run of zero bits followed by a run of one bits, such as 0.0.0.255.", nameof(hostMask));

            return new NetworkBits(version, AddressMath.FromIPAddress(address), prefixLength).ToIPNetwork();
        }

        /// <summary>
        ///     Creates a network from an address and a network mask, such as <c>10.0.0.0</c> and <c>255.255.255.0</c>.
        /// </summary>
        /// <param name="address">An address within the network. Any host bits that are set are cleared.</param>
        /// <param name="networkMask">The network mask, which must be the same IP version as <paramref name="address" />.</param>
        /// <returns>The network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address, or when <paramref name="networkMask" />
        ///     is not a valid network mask of the same IP version.
        /// </exception>
        public static IPNetwork FromMask(IPAddress address, IPAddress networkMask)
        {
            var version = GetVersionOf(address, nameof(address));
            ArgumentNullException.ThrowIfNull(networkMask);

            if (!TryGetPrefixLength(networkMask, version, out var prefixLength))
                throw new ArgumentException($"'{networkMask}' is not a valid {version} network mask. A network mask is a run of one bits followed by a run of zero bits, such as 255.255.255.0.", nameof(networkMask));

            return new NetworkBits(version, AddressMath.FromIPAddress(address), prefixLength).ToIPNetwork();
        }

        /// <summary>
        ///     Gets the host mask, also called the wildcard mask, for a prefix length, such as <c>0.0.0.255</c> for an IPv4 /24.
        /// </summary>
        /// <param name="version">The IP version.</param>
        /// <param name="prefixLength">The prefix length, from 0 to 32 for IPv4 or 0 to 128 for IPv6.</param>
        /// <returns>The host mask.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either argument is out of range.</exception>
        public static IPAddress GetHostMask(IPVersion version, int prefixLength)
        {
            ValidateVersionAndPrefixLength(version, prefixLength);

            return AddressMath.ToIPAddress(AddressMath.GetHostMaskBits(version, prefixLength), version);
        }

        /// <summary>
        ///     Gets the network mask for a prefix length, such as <c>255.255.255.0</c> for an IPv4 /24.
        /// </summary>
        /// <param name="version">The IP version.</param>
        /// <param name="prefixLength">The prefix length, from 0 to 32 for IPv4 or 0 to 128 for IPv6.</param>
        /// <returns>The network mask.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either argument is out of range.</exception>
        public static IPAddress GetNetworkMask(IPVersion version, int prefixLength)
        {
            ValidateVersionAndPrefixLength(version, prefixLength);

            return AddressMath.ToIPAddress(AddressMath.GetNetworkMaskBits(version, prefixLength), version);
        }

        /// <summary>
        ///     Parses the text of a network, accepting more formats than <see cref="IPNetwork.Parse(string)" /> but only the
        ///     standard form of an IPv4 address.
        /// </summary>
        /// <remarks>
        ///     <para>The following formats are accepted, with any IPv4 or IPv6 address:</para>
        ///     <list type="bullet">
        ///         <item><description>An address and a prefix length, such as <c>10.0.0.0/24</c> or <c>2001:db8::/32</c>.</description></item>
        ///         <item><description>An address and a network mask, such as <c>10.0.0.0 255.255.255.0</c> or <c>10.0.0.0/255.255.255.0</c>.</description></item>
        ///         <item><description>An address and a host mask, also called a wildcard mask, such as <c>10.0.0.0 0.0.0.255</c>.</description></item>
        ///         <item><description>A single address, such as <c>10.0.0.1</c>, which becomes a host network.</description></item>
        ///     </list>
        ///     <para>
        ///         A mask is read as a network mask when it is one, and otherwise as a host mask. The all-zero and all-one masks are
        ///         valid network masks, so <c>10.0.0.0 0.0.0.0</c> is a /0 and <c>10.0.0.0 255.255.255.255</c> is a /32; Python's
        ///         <c>ipaddress</c> module follows the same rule. A /0 or /32 written with
        ///         <see cref="NetworkFormat.AddressAndHostMask" /> therefore does not parse back to the same network.
        ///     </para>
        ///     <para>
        ///         Leading and trailing whitespace is ignored, any host bits that are set in the address are cleared, and an IPv6
        ///         scope ID is dropped. Host names are not accepted, because looking them up is a network operation; use
        ///         <see cref="Resolve" /> or <see cref="ResolveAsync" /> for those.
        ///     </para>
        ///     <para>
        ///         An IPv4 address or mask must be four decimal numbers separated by dots, without leading zeros.
        ///         <see cref="IPNetwork.Parse(string)" /> and <see cref="IPAddress.Parse(string)" /> also accept older forms, and
        ///         read a number with a leading zero as octal, so that <c>010.0.0.0/8</c> is <c>8.0.0.0/8</c>. Those forms are
        ///         rejected here, because they are rarely intended and are a known source of security bugs. Prefer this method for
        ///         text from users, files or other systems.
        ///     </para>
        /// </remarks>
        /// <param name="value">The text to parse.</param>
        /// <returns>The parsed network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid network.</exception>
        public static IPNetwork ParseStrict(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return IPNetwork.ParseStrict(value.AsSpan());
        }

        /// <summary>
        ///     Parses the text of a network. See <see cref="ParseStrict(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <returns>The parsed network.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid network.</exception>
        public static IPNetwork ParseStrict(ReadOnlySpan<char> value)
        {
            if (TryParseCore(value, out var networkBits))
                return networkBits.ToIPNetwork();

            throw new FormatException($"'{value}' is not a valid network. {SupportedFormats}");
        }

        /// <summary>
        ///     Parses UTF-8 text of a network. See <see cref="ParseStrict(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text to parse.</param>
        /// <returns>The parsed network.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="utf8Text" /> is not a valid network.</exception>
        public static IPNetwork ParseStrict(ReadOnlySpan<byte> utf8Text)
        {
            if (TryParseUtf8(utf8Text, out var networkBits))
                return networkBits.ToIPNetwork();

            throw new FormatException($"'{Encoding.UTF8.GetString(utf8Text)}' is not a valid network. {SupportedFormats}");
        }

        /// <summary>
        ///     Creates a network from text that may hold a host name in place of an address, such as <c>example.com/24</c>, looking
        ///     the host name up in DNS.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Text that <see cref="ParseStrict(string)" /> accepts is parsed without a lookup. Otherwise the text before the
        ///         prefix length or mask is looked up as a host name, and the first address returned is used.
        ///     </para>
        ///     <para>
        ///         When <paramref name="addressFamily" /> is <see cref="AddressFamily.Unspecified" /> and the text has a mask, only
        ///         addresses of the mask's IP version are looked up.
        ///     </para>
        /// </remarks>
        /// <param name="value">The text, such as <c>example.com</c>, <c>example.com/24</c> or <c>example.com 255.255.255.0</c>.</param>
        /// <param name="addressFamily">
        ///     The kind of address to look up: <see cref="AddressFamily.InterNetwork" /> for IPv4,
        ///     <see cref="AddressFamily.InterNetworkV6" /> for IPv6, or <see cref="AddressFamily.Unspecified" /> for either.
        /// </param>
        /// <returns>The network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="addressFamily" /> is not one of the values above.</exception>
        /// <exception cref="FormatException">Thrown when the text is not a valid network or host name.</exception>
        /// <exception cref="SocketException">Thrown when the host name can't be resolved.</exception>
        public static IPNetwork Resolve(string value, AddressFamily addressFamily = AddressFamily.Unspecified)
        {
            // text that is already a network needs no lookup
            if (TryParseBeforeLookup(value, addressFamily, out var network, out var hostName, out var lookupFamily))
                return network;

            var hostAddresses = Dns.GetHostAddresses(hostName, lookupFamily);
            return CreateFromHostAddresses(value, hostAddresses);
        }

        /// <summary>
        ///     Creates a network from text that may hold a host name in place of an address, such as <c>example.com/24</c>, looking
        ///     the host name up in DNS asynchronously. See <see cref="Resolve" />.
        /// </summary>
        /// <param name="value">The text, such as <c>example.com</c>, <c>example.com/24</c> or <c>example.com 255.255.255.0</c>.</param>
        /// <param name="addressFamily">
        ///     The kind of address to look up: <see cref="AddressFamily.InterNetwork" /> for IPv4,
        ///     <see cref="AddressFamily.InterNetworkV6" /> for IPv6, or <see cref="AddressFamily.Unspecified" /> for either.
        /// </param>
        /// <param name="cancellationToken">A token that cancels the lookup.</param>
        /// <returns>The network.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="addressFamily" /> is not one of the values above.</exception>
        /// <exception cref="FormatException">Thrown when the text is not a valid network or host name.</exception>
        /// <exception cref="SocketException">Thrown when the host name can't be resolved.</exception>
        public static async Task<IPNetwork> ResolveAsync(string value, AddressFamily addressFamily = AddressFamily.Unspecified, CancellationToken cancellationToken = default)
        {
            // text that is already a network needs no lookup
            if (TryParseBeforeLookup(value, addressFamily, out var network, out var hostName, out var lookupFamily))
                return network;

            var hostAddresses = await Dns.GetHostAddressesAsync(hostName, lookupFamily, cancellationToken).ConfigureAwait(false);
            return CreateFromHostAddresses(value, hostAddresses);
        }

        /// <summary>
        ///     Attempts to parse the text of a network. See <see cref="ParseStrict(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the parsed network.</param>
        /// <returns><see langword="true" /> when the text is a valid network; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseStrict([NotNullWhen(true)] string? value, out IPNetwork result)
        {
            if (value is null)
            {
                result = default;
                return false;
            }

            return IPNetwork.TryParseStrict(value.AsSpan(), out result);
        }

        /// <summary>
        ///     Attempts to parse the text of a network. See <see cref="ParseStrict(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the parsed network.</param>
        /// <returns><see langword="true" /> when the text is a valid network; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseStrict(ReadOnlySpan<char> value, out IPNetwork result)
        {
            if (TryParseCore(value, out var networkBits))
            {
                result = networkBits.ToIPNetwork();
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        ///     Attempts to parse UTF-8 text of a network. See <see cref="ParseStrict(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the parsed network.</param>
        /// <returns><see langword="true" /> when the text is a valid network; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseStrict(ReadOnlySpan<byte> utf8Text, out IPNetwork result)
        {
            if (TryParseUtf8(utf8Text, out var networkBits))
            {
                result = networkBits.ToIPNetwork();
                return true;
            }

            result = default;
            return false;
        }
    }

    /// <summary>
    ///     Parses the text of a network into integer form. This is the parser behind <c>ParseStrict</c> and <c>TryParseStrict</c>.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="network">When this method returns <see langword="true" />, the parsed network.</param>
    /// <returns><see langword="true" /> when the text is a valid network.</returns>
    internal static bool TryParseCore(ReadOnlySpan<char> value, out NetworkBits network)
    {
        network = default;

        if (!TrySplitAddressAndSuffix(value, out var addressText, out var suffixText))
            return false;

        if (!StrictAddressParser.TryParse(addressText, out var address, out var version))
            return false;

        if (!TryParseSuffix(suffixText, version, out var prefixLength))
            return false;

        network = new NetworkBits(version, AddressMath.FromIPAddress(address), prefixLength);
        return true;
    }

    /// <summary>
    ///     Parses UTF-8 text of a network into integer form, by converting it to characters and parsing those.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to parse.</param>
    /// <param name="network">When this method returns <see langword="true" />, the parsed network.</param>
    /// <returns><see langword="true" /> when the text is a valid network.</returns>
    internal static bool TryParseUtf8(ReadOnlySpan<byte> utf8Text, out NetworkBits network)
    {
        // UTF-8 never needs more characters than it has bytes, so a buffer as long as the text always fits it. Short text,
        // which is all valid text, converts on the stack
        char[]? rentedCharacters = null;
        var characters = utf8Text.Length <= Utf8StackLimit
            ? stackalloc char[Utf8StackLimit]
            : rentedCharacters = ArrayPool<char>.Shared.Rent(utf8Text.Length);

        try
        {
            // invalid UTF-8 becomes replacement characters, which then fail to parse
            var characterCount = Encoding.UTF8.GetChars(utf8Text, characters);
            return TryParseCore(characters[..characterCount], out network);
        }
        finally
        {
            if (rentedCharacters is not null)
                ArrayPool<char>.Shared.Return(rentedCharacters);
        }
    }

    /// <summary>
    ///     Creates a network from the addresses that DNS returned for the host name in some text.
    /// </summary>
    /// <param name="value">The text that was looked up, which still holds the prefix length or mask.</param>
    /// <param name="hostAddresses">The addresses that DNS returned.</param>
    /// <returns>The network.</returns>
    /// <exception cref="SocketException">Thrown when DNS returned no addresses.</exception>
    /// <exception cref="FormatException">Thrown when the prefix length or mask does not suit the address that was found.</exception>
    private static IPNetwork CreateFromHostAddresses(string value, IPAddress[] hostAddresses)
    {
        // use the first IPv4 or IPv6 address, in the order of preference that the resolver returned them in
        var hostAddress = hostAddresses.FirstOrDefault(address => AddressMath.TryGetVersion(address.AddressFamily, out _));
        if (hostAddress is null)
            throw new SocketException((int)SocketError.HostNotFound);

        TrySplitAddressAndSuffix(value, out _, out var suffixText);
        var version = AddressMath.GetVersion(hostAddress.AddressFamily);

        // a prefix length that suits one IP version, such as /64, may not suit the address that the lookup returned
        if (!TryParseSuffix(suffixText, version, out var prefixLength))
            throw new FormatException($"'{suffixText}' is not a valid prefix length or mask for {hostAddress}, the {version} address of '{value}'.");

        return new NetworkBits(version, AddressMath.FromIPAddress(hostAddress), prefixLength).ToIPNetwork();
    }

    /// <summary>
    ///     Gets the IP version of an address that a network is created from, and validates the address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="parameterName">The name of the parameter that holds the address, for the exception message.</param>
    /// <returns>The IP version of the address.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address.</exception>
    internal static IPVersion GetVersionOf([NotNull] IPAddress? address, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(address, parameterName);

        if (!AddressMath.TryGetVersion(address.AddressFamily, out var version))
            throw new ArgumentException($"Only IPv4 and IPv6 addresses are supported. The address family {address.AddressFamily} is not.", parameterName);

        return version;
    }

    /// <summary>
    ///     Determines whether text is a prefix length, which is made only of digits.
    /// </summary>
    /// <param name="suffixText">The text after the separator.</param>
    /// <returns><see langword="true" /> when the text is made only of digits.</returns>
    private static bool IsPrefixLength(ReadOnlySpan<char> suffixText)
    {
        return !suffixText.IsEmpty && !suffixText.ContainsAnyExcept(StrictAddressParser.Digits);
    }

    /// <summary>
    ///     Validates the IP version and prefix length that a mask is requested for.
    /// </summary>
    /// <param name="version">The IP version.</param>
    /// <param name="prefixLength">The prefix length.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either argument is out of range.</exception>
    private static void ValidateVersionAndPrefixLength(IPVersion version, int prefixLength)
    {
        if (version is not (IPVersion.IPv4 or IPVersion.IPv6))
            throw new ArgumentOutOfRangeException(nameof(version), version, "Only IPv4 and IPv6 are supported.");

        ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixLength, AddressMath.GetBitLength(version));
    }

    /// <summary>
    ///     Validates the arguments of <c>Resolve</c> and <c>ResolveAsync</c>, parses text that needs no lookup, and otherwise finds
    ///     the host name and address family to look up.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="addressFamily">The address family that the caller asked for.</param>
    /// <param name="network">When this method returns <see langword="true" />, the parsed network.</param>
    /// <param name="hostName">When this method returns <see langword="false" />, the host name to look up.</param>
    /// <param name="lookupFamily">When this method returns <see langword="false" />, the address family to look up.</param>
    /// <returns><see langword="true" /> when the text is a network and needs no lookup.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="addressFamily" /> is not IPv4, IPv6 or unspecified.</exception>
    /// <exception cref="FormatException">Thrown when the text is neither a network nor a host name with a valid suffix.</exception>
    private static bool TryParseBeforeLookup(string value, AddressFamily addressFamily, out IPNetwork network, out string hostName, out AddressFamily lookupFamily)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (addressFamily is not (AddressFamily.Unspecified or AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            throw new ArgumentOutOfRangeException(nameof(addressFamily), addressFamily, "Only IPv4, IPv6 or unspecified are supported.");

        hostName = string.Empty;
        lookupFamily = addressFamily;

        if (IPNetwork.TryParseStrict(value, out network))
            return true;

        if (!TrySplitAddressAndSuffix(value, out var hostText, out var suffixText) || hostText.IsEmpty)
            throw new FormatException($"'{value}' is not a valid network or host name.");

        // text that IPAddress reads as an address, in any of its forms, is a malformed network rather than a host name to look
        // up. That covers an invalid suffix, as in 10.0.0.0/33, and an address in a rejected form, as in 010.0.0.0/8, which a
        // resolver would otherwise read as an address in its own way
        if (IPAddress.TryParse(hostText, out _))
            throw new FormatException($"'{value}' is not a valid network. {SupportedFormats}");

        // a mask decides which IP version to look up, unless the caller already chose one
        if (lookupFamily == AddressFamily.Unspecified && !suffixText.IsEmpty && !IsPrefixLength(suffixText) && IPAddress.TryParse(suffixText, out var mask))
            lookupFamily = mask.AddressFamily;

        hostName = hostText.ToString();
        return false;
    }

    /// <summary>
    ///     Gets the prefix length of a network mask.
    /// </summary>
    /// <param name="networkMask">The network mask.</param>
    /// <param name="version">The IP version that the mask must have.</param>
    /// <param name="prefixLength">When this method returns <see langword="true" />, the prefix length of the mask.</param>
    /// <returns><see langword="true" /> when the mask is a valid network mask of the required IP version.</returns>
    internal static bool TryGetPrefixLength(IPAddress networkMask, IPVersion version, out int prefixLength)
    {
        // a mask of the other IP version does not apply to the address
        if (!AddressMath.TryGetVersion(networkMask.AddressFamily, out var maskVersion) || maskVersion != version)
        {
            prefixLength = 0;
            return false;
        }

        return AddressMath.TryGetPrefixLength(AddressMath.FromIPAddress(networkMask), version, out prefixLength);
    }

    /// <summary>
    ///     Gets the prefix length of a host mask, which is the inverse of a network mask.
    /// </summary>
    /// <param name="hostMask">The host mask.</param>
    /// <param name="version">The IP version that the mask must have.</param>
    /// <param name="prefixLength">When this method returns <see langword="true" />, the prefix length that the mask stands for.</param>
    /// <returns><see langword="true" /> when the mask is a valid host mask of the required IP version.</returns>
    internal static bool TryGetPrefixLengthFromHostMask(IPAddress hostMask, IPVersion version, out int prefixLength)
    {
        if (!AddressMath.TryGetVersion(hostMask.AddressFamily, out var maskVersion) || maskVersion != version)
        {
            prefixLength = 0;
            return false;
        }

        // inverting a host mask gives the network mask it stands for
        var networkMaskBits = ~AddressMath.FromIPAddress(hostMask) & AddressMath.GetAllBits(version);
        return AddressMath.TryGetPrefixLength(networkMaskBits, version, out prefixLength);
    }

    /// <summary>
    ///     Parses what follows the address: a prefix length, a network or host mask, or nothing.
    /// </summary>
    /// <param name="suffixText">The text after the separator, which is empty when there was no separator.</param>
    /// <param name="version">The IP version of the address.</param>
    /// <param name="prefixLength">When this method returns <see langword="true" />, the prefix length.</param>
    /// <returns><see langword="true" /> when the suffix is valid for the IP version.</returns>
    private static bool TryParseSuffix(ReadOnlySpan<char> suffixText, IPVersion version, out int prefixLength)
    {
        var addressBitLength = AddressMath.GetBitLength(version);

        // with no suffix the network holds a single host
        if (suffixText.IsEmpty)
        {
            prefixLength = addressBitLength;
            return true;
        }

        // text made only of digits is a prefix length. It is never treated as a mask, even though IPAddress would accept a
        // number such as 4294967295 as an IPv4 address. NumberStyles.None rejects signs, whitespace and group separators
        if (IsPrefixLength(suffixText))
            return int.TryParse(suffixText, NumberStyles.None, CultureInfo.InvariantCulture, out prefixLength) && prefixLength <= addressBitLength;

        // anything else must be a mask of the same IP version as the address
        prefixLength = 0;
        if (!StrictAddressParser.TryParse(suffixText, out var mask, out _))
            return false;

        // a mask is a network mask when it is one, and otherwise a host mask. The all-zero and all-one masks are both valid
        // network masks, so they are never read as host masks, which leaves no text that could mean two different networks
        return TryGetPrefixLength(mask, version, out prefixLength) || TryGetPrefixLengthFromHostMask(mask, version, out prefixLength);
    }

    /// <summary>
    ///     Splits the text of a network into the address and whatever follows it.
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
