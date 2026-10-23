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

public static partial class IPNetworkExtensions
{
    /// <summary>
    ///     A description of the range format, for exception messages.
    /// </summary>
    private const string SupportedRangeFormat = "A range is two complete addresses of the same IP version separated by a dash, the first no higher than the second, such as 10.0.0.1-10.0.0.9.";

    extension(IPNetwork)
    {
        /// <summary>
        ///     Computes the smallest network that contains two addresses. The result may also contain addresses outside the range
        ///     between them. For example, the containing network of <c>10.0.0.1</c> and <c>10.0.0.9</c> is <c>10.0.0.0/28</c>.
        /// </summary>
        /// <param name="firstAddress">The first address.</param>
        /// <param name="secondAddress">The second address.</param>
        /// <returns>The smallest network that contains both addresses.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when the addresses are different IP versions, or neither IPv4 nor IPv6.</exception>
        public static IPNetwork GetContainingNetwork(IPAddress firstAddress, IPAddress secondAddress)
        {
            var firstNetwork = ToHostNetworkBits(firstAddress, nameof(firstAddress));
            var secondNetwork = ToHostNetworkBits(secondAddress, nameof(secondAddress));

            return GetContainingNetwork(firstNetwork, secondNetwork, nameof(secondAddress)).ToIPNetwork();
        }

        /// <summary>
        ///     Parses the text of an address range, such as <c>10.0.0.1-10.0.0.9</c>, into the fewest networks that cover exactly
        ///     that range. See <see cref="Summarize(IPAddress, IPAddress)" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The range is written as the first and last addresses, separated by a dash with optional whitespace around it, as
        ///         in <c>10.0.0.1 - 10.0.0.9</c> or <c>2001:db8::1-2001:db8::ff</c>. Text that <c>ParseStrict</c> accepts, such
        ///         as <c>10.0.0.0/24</c>, is also accepted, and produces that single network.
        ///     </para>
        ///     <para>
        ///         Both addresses must be complete. Shorthand such as <c>10.0.0.1-9</c> is rejected, for the same reason that
        ///         <c>ParseStrict</c> rejects shorthand IPv4 addresses: it is ambiguous, and easy to misread.
        ///     </para>
        /// </remarks>
        /// <param name="value">The text to parse.</param>
        /// <returns>The networks that cover the range, in ascending order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid range or network.</exception>
        public static IReadOnlyList<IPNetwork> ParseRange(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return IPNetwork.ParseRange(value.AsSpan());
        }

        /// <summary>
        ///     Parses the text of an address range into the fewest networks that cover exactly that range. See
        ///     <see cref="ParseRange(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <returns>The networks that cover the range, in ascending order.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid range or network.</exception>
        public static IReadOnlyList<IPNetwork> ParseRange(ReadOnlySpan<char> value)
        {
            if (TryParseRangeCore(value, out var networks))
                return ToIPNetworks(networks);

            throw new FormatException($"'{value}' is not a valid address range or network. {SupportedRangeFormat}");
        }

        /// <summary>
        ///     Parses UTF-8 text of an address range into the fewest networks that cover exactly that range. See
        ///     <see cref="ParseRange(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text to parse.</param>
        /// <returns>The networks that cover the range, in ascending order.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="utf8Text" /> is not a valid range or network.</exception>
        public static IReadOnlyList<IPNetwork> ParseRange(ReadOnlySpan<byte> utf8Text)
        {
            if (Utf8Text.TryParse<List<NetworkBits>?>(utf8Text, TryParseRangeCore, out var networks) && networks is not null)
                return ToIPNetworks(networks);

            throw new FormatException($"'{Encoding.UTF8.GetString(utf8Text)}' is not a valid address range or network. {SupportedRangeFormat}");
        }

        /// <summary>
        ///     Computes the fewest networks that cover exactly the addresses from one address to another, inclusive. For example,
        ///     <c>10.0.0.1</c> to <c>10.0.0.9</c> is <c>10.0.0.1/32</c>, <c>10.0.0.2/31</c>, <c>10.0.0.4/30</c> and <c>10.0.0.8/31</c>.
        /// </summary>
        /// <remarks>
        ///     Firewalls, access control lists and cloud security groups usually accept only networks, while people, DHCP scopes
        ///     and vendor allow-lists often give ranges. This converts the one to the other.
        /// </remarks>
        /// <param name="firstAddress">The first address of the range.</param>
        /// <param name="lastAddress">The last address of the range, which must not be lower than the first.</param>
        /// <returns>The networks that cover the range, in ascending order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either address is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the addresses are different IP versions, neither IPv4 nor IPv6, or the last address is lower than the first.
        /// </exception>
        public static IReadOnlyList<IPNetwork> Summarize(IPAddress firstAddress, IPAddress lastAddress)
        {
            var firstVersion = GetVersionOf(firstAddress, nameof(firstAddress));
            var lastVersion = GetVersionOf(lastAddress, nameof(lastAddress));

            if (firstVersion != lastVersion)
                throw new ArgumentException($"Both addresses must be the same IP version, but {firstAddress} is {firstVersion} and {lastAddress} is {lastVersion}.", nameof(lastAddress));

            var firstAddressBits = AddressMath.FromIPAddress(firstAddress);
            var lastAddressBits = AddressMath.FromIPAddress(lastAddress);

            if (lastAddressBits < firstAddressBits)
                throw new ArgumentException($"The last address, {lastAddress}, must not be lower than the first address, {firstAddress}.", nameof(lastAddress));

            return ToIPNetworks(SummarizeRange(firstVersion, firstAddressBits, lastAddressBits));
        }

        /// <summary>
        ///     Attempts to parse the text of an address range into the fewest networks that cover exactly that range. See
        ///     <see cref="ParseRange(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the networks that cover the range, in ascending order.</param>
        /// <returns><see langword="true" /> when the text is a valid range or network; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseRange([NotNullWhen(true)] string? value, [NotNullWhen(true)] out IReadOnlyList<IPNetwork>? result)
        {
            if (value is null)
            {
                result = null;
                return false;
            }

            return IPNetwork.TryParseRange(value.AsSpan(), out result);
        }

        /// <summary>
        ///     Attempts to parse the text of an address range into the fewest networks that cover exactly that range. See
        ///     <see cref="ParseRange(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the networks that cover the range, in ascending order.</param>
        /// <returns><see langword="true" /> when the text is a valid range or network; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseRange(ReadOnlySpan<char> value, [NotNullWhen(true)] out IReadOnlyList<IPNetwork>? result)
        {
            if (TryParseRangeCore(value, out var networks))
            {
                result = ToIPNetworks(networks);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        ///     Attempts to parse UTF-8 text of an address range into the fewest networks that cover exactly that range. See
        ///     <see cref="ParseRange(string)" /> for the accepted formats.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text to parse.</param>
        /// <param name="result">When this method returns <see langword="true" />, the networks that cover the range, in ascending order.</param>
        /// <returns><see langword="true" /> when the text is a valid range or network; otherwise <see langword="false" />. This method never throws.</returns>
        public static bool TryParseRange(ReadOnlySpan<byte> utf8Text, [NotNullWhen(true)] out IReadOnlyList<IPNetwork>? result)
        {
            if (Utf8Text.TryParse<List<NetworkBits>?>(utf8Text, TryParseRangeCore, out var networks) && networks is not null)
            {
                result = ToIPNetworks(networks);
                return true;
            }

            result = null;
            return false;
        }
    }

    /// <summary>
    ///     Computes the fewest networks that cover exactly a range of addresses.
    /// </summary>
    /// <param name="version">The IP version of the addresses.</param>
    /// <param name="firstAddressBits">The first address of the range, in integer form.</param>
    /// <param name="lastAddressBits">The last address of the range, in integer form, which is not lower than the first.</param>
    /// <returns>The networks, in ascending order.</returns>
    private static List<NetworkBits> SummarizeRange(IPVersion version, UInt128 firstAddressBits, UInt128 lastAddressBits)
    {
        var addressBitLength = AddressMath.GetBitLength(version);
        var networks = new List<NetworkBits>();

        // take the largest network that starts at the current address and stays inside the range, then carry on from the address
        // after it. Each step takes the largest network that fits, so the result is the fewest networks
        var currentAddressBits = firstAddressBits;
        while (true)
        {
            // a network starts on a boundary of its own size, so its host bits can be no more than the number of trailing zero bits
            // of its first address. Address zero is a boundary of every size
            var alignedHostBitLength = currentAddressBits == UInt128.Zero ? addressBitLength : Math.Min((int)UInt128.TrailingZeroCount(currentAddressBits), addressBitLength);

            // and it must not run past the last address. A network of n host bits holds 2^n addresses, so n can be no more than
            // the base two logarithm of the number of addresses left. When every IPv6 address is left, that number, 2^128, does
            // not fit in a UInt128; the range then starts at address zero, and the alignment already gives 128
            var remainingAddressesLessOne = lastAddressBits - currentAddressBits;
            var fittingHostBitLength = remainingAddressesLessOne == UInt128.MaxValue
                ? AddressMath.IPv6BitLength
                : AddressMath.IPv6BitLength - 1 - (int)UInt128.LeadingZeroCount(remainingAddressesLessOne + UInt128.One);

            var network = new NetworkBits(version, currentAddressBits, addressBitLength - Math.Min(alignedHostBitLength, fittingHostBitLength));
            networks.Add(network);

            // stop on the network that ends at the last address, rather than moving past it; the last IPv6 address is
            // UInt128.MaxValue, and moving past it would wrap around to zero
            if (network.LastAddressBits == lastAddressBits)
                return networks;

            currentAddressBits = network.LastAddressBits + UInt128.One;
        }
    }

    /// <summary>
    ///     Converts an address to the host network that holds it, in integer form, and validates the address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="parameterName">The name of the parameter that holds the address, for the exception message.</param>
    /// <returns>The host network.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address.</exception>
    internal static NetworkBits ToHostNetworkBits(IPAddress? address, string parameterName)
    {
        var version = GetVersionOf(address, parameterName);

        return new NetworkBits(version, AddressMath.FromIPAddress(address), AddressMath.GetBitLength(version));
    }

    /// <summary>
    ///     Parses the text of an address range, or of a single network, into the networks that cover it.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="networks">When this method returns <see langword="true" />, the networks, in ascending order.</param>
    /// <returns><see langword="true" /> when the text is a valid range or network.</returns>
    private static bool TryParseRangeCore(ReadOnlySpan<char> value, [NotNullWhen(true)] out List<NetworkBits>? networks)
    {
        networks = null;
        var trimmedValue = value.Trim();

        // a dash is never part of an address or a network, so text without one is a single network
        var dashIndex = trimmedValue.IndexOf('-');
        if (dashIndex < 0)
        {
            if (!TryParseCore(trimmedValue, out var network))
                return false;

            networks = [network];
            return true;
        }

        // whitespace may surround the dash
        var firstAddressText = trimmedValue[..dashIndex].TrimEnd();
        var lastAddressText = trimmedValue[(dashIndex + 1)..].TrimStart();

        if (!StrictAddressParser.TryParse(firstAddressText, out var firstAddress, out var firstVersion))
            return false;

        if (!StrictAddressParser.TryParse(lastAddressText, out var lastAddress, out var lastVersion) || lastVersion != firstVersion)
            return false;

        var firstAddressBits = AddressMath.FromIPAddress(firstAddress);
        var lastAddressBits = AddressMath.FromIPAddress(lastAddress);
        if (lastAddressBits < firstAddressBits)
            return false;

        networks = SummarizeRange(firstVersion, firstAddressBits, lastAddressBits);
        return true;
    }
}
