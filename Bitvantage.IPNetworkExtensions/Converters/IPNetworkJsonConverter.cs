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

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bitvantage.InternetProtocol.Converters;

/// <summary>
///     Converts an <see cref="IPNetwork" /> to and from a JSON string in CIDR notation, such as <c>"10.0.0.0/24"</c>.
/// </summary>
/// <remarks>
///     <para>
///         System.Text.Json has no built-in support for <see cref="IPNetwork" />, and serializing one without a converter fails.
///         Register this converter for every network in a serialization, or apply it to a single property with
///         <see cref="JsonConverterAttribute" />:
///     </para>
///     <code>
///     var options = new JsonSerializerOptions();
///     options.Converters.Add(new IPNetworkJsonConverter());
///
///     [JsonConverter(typeof(IPNetworkJsonConverter))]
///     public IPNetwork Network { get; set; }
///     </code>
///     <para>
///         It reads every format that <c>IPNetwork.ParseStrict</c> accepts, rejects the nonstandard IPv4 forms that it rejects,
///         and supports networks as the keys of a dictionary.
///     </para>
/// </remarks>
public sealed class IPNetworkJsonConverter : JsonConverter<IPNetwork>
{
    /// <summary>
    ///     The most UTF-8 bytes the text of a network without a scope ID can take: an IPv6 address of up to 45 characters, a
    ///     slash, and a prefix length of up to three digits.
    /// </summary>
    private const int MaxNetworkTextLength = 64;

    /// <summary>
    ///     Reads a network from a JSON string.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value.</param>
    /// <param name="typeToConvert">The type to convert, which is <see cref="IPNetwork" />.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The network.</returns>
    /// <exception cref="JsonException">Thrown when the value is not a string, or is not a valid network.</exception>
    public override IPNetwork Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"A network must be a JSON string, such as \"10.0.0.0/24\", but the value is a {reader.TokenType}.");

        return ReadNetwork(ref reader);
    }

    /// <summary>
    ///     Reads a network from the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="reader">The reader, positioned on the property name.</param>
    /// <param name="typeToConvert">The type to convert, which is <see cref="IPNetwork" />.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The network.</returns>
    /// <exception cref="JsonException">Thrown when the property name is not a valid network.</exception>
    public override IPNetwork ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadNetwork(ref reader);
    }

    /// <summary>
    ///     Writes a network as a JSON string in CIDR notation.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The network.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, IPNetwork value, JsonSerializerOptions options)
    {
        Span<byte> networkText = stackalloc byte[MaxNetworkTextLength];
        if (TryFormatWithoutScope(value, networkText, out var networkTextLength))
            writer.WriteStringValue(networkText[..networkTextLength]);
        else
            writer.WriteStringValue(value.ToString(NetworkFormat.AddressAndPrefix));
    }

    /// <summary>
    ///     Writes a network as the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The network.</param>
    /// <param name="options">The serializer options.</param>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, IPNetwork value, JsonSerializerOptions options)
    {
        Span<byte> networkText = stackalloc byte[MaxNetworkTextLength];
        if (TryFormatWithoutScope(value, networkText, out var networkTextLength))
            writer.WritePropertyName(networkText[..networkTextLength]);
        else
            writer.WritePropertyName(value.ToString(NetworkFormat.AddressAndPrefix));
    }

    /// <summary>
    ///     Formats a network in CIDR notation straight to UTF-8, which creates no string, unless its base address has an IPv6
    ///     scope ID.
    /// </summary>
    /// <remarks>
    ///     <see cref="IPNetwork" /> writes the scope ID of its base address, as in <c>fe80::%3/64</c>, but
    ///     <c>ToString(NetworkFormat.AddressAndPrefix)</c> and <c>IPNetwork.ParseStrict</c> leave it out. A network with a scope
    ///     ID is left to <c>ToString</c>, so that every network is written the same way.
    /// </remarks>
    /// <param name="network">The network.</param>
    /// <param name="destination">The buffer to write the UTF-8 text to.</param>
    /// <param name="bytesWritten">When this method returns <see langword="true" />, the number of bytes written.</param>
    /// <returns><see langword="true" /> when the network was formatted.</returns>
    private static bool TryFormatWithoutScope(IPNetwork network, Span<byte> destination, out int bytesWritten)
    {
        // only an IPv6 address has a scope ID; reading it from an IPv4 address throws
        var baseAddress = network.BaseAddress;
        if (baseAddress.AddressFamily == AddressFamily.InterNetworkV6 && baseAddress.ScopeId != 0)
        {
            bytesWritten = 0;
            return false;
        }

        return network.TryFormat(destination, out bytesWritten);
    }

    /// <summary>
    ///     Parses the current JSON string or property name as a network.
    /// </summary>
    /// <param name="reader">The reader, positioned on the string or property name.</param>
    /// <returns>The network.</returns>
    /// <exception cref="JsonException">Thrown when the text is not a valid network.</exception>
    private static IPNetwork ReadNetwork(ref Utf8JsonReader reader)
    {
        // when the value is in one piece and has no escape sequences, its raw UTF-8 bytes are the text, and are parsed without
        // creating a string
        if (!reader.HasValueSequence && !reader.ValueIsEscaped && IPNetwork.TryParseStrict(reader.ValueSpan, out var network))
            return network;

        var networkText = reader.GetString();
        if (IPNetwork.TryParseStrict(networkText, out network))
            return network;

        // a JsonException, rather than a FormatException, lets the serializer add the path of the bad value to the message
        throw new JsonException($"'{networkText}' is not a valid network.");
    }
}
