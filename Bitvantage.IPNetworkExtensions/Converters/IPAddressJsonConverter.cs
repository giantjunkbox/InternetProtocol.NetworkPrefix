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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bitvantage.InternetProtocol.Converters;

/// <summary>
///     Converts an <see cref="IPAddress" /> to and from a JSON string, such as <c>"10.0.0.1"</c> or <c>"2001:db8::1"</c>.
/// </summary>
/// <remarks>
///     <para>
///         System.Text.Json has no built-in support for <see cref="IPAddress" />: serializing one throws a
///         <see cref="System.Net.Sockets.SocketException" />, and deserializing one throws a <see cref="JsonException" />.
///         Register this converter for every address in a serialization, or apply it to a single property with
///         <see cref="JsonConverterAttribute" />:
///     </para>
///     <code>
///     var options = new JsonSerializerOptions();
///     options.Converters.Add(new IPAddressJsonConverter());
///
///     [JsonConverter(typeof(IPAddressJsonConverter))]
///     public IPAddress Address { get; set; }
///     </code>
///     <para>
///         It reads every format that <c>IPAddress.ParseStrict</c> accepts, rejects the nonstandard IPv4 forms that it rejects,
///         keeps an IPv6 scope ID, and supports addresses as the keys of a dictionary. A JSON <c>null</c> reads as a
///         <see langword="null" /> address, and a <see langword="null" /> address writes as <c>null</c>.
///     </para>
/// </remarks>
public sealed class IPAddressJsonConverter : JsonConverter<IPAddress>
{
    /// <summary>
    ///     The most UTF-8 bytes the text of an address can take. The longest IPv6 address is 45 characters, and a scope ID adds
    ///     up to 11 more, for a percent sign and a 32-bit number.
    /// </summary>
    private const int MaxAddressTextLength = 64;

    /// <summary>
    ///     Reads an address from a JSON string.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value.</param>
    /// <param name="typeToConvert">The type to convert, which is <see cref="IPAddress" />.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The address.</returns>
    /// <exception cref="JsonException">Thrown when the value is not a string, or is not a valid address.</exception>
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"An IP address must be a JSON string, such as \"10.0.0.1\", but the value is a {reader.TokenType}.");

        return ReadAddress(ref reader);
    }

    /// <summary>
    ///     Reads an address from the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="reader">The reader, positioned on the property name.</param>
    /// <param name="typeToConvert">The type to convert, which is <see cref="IPAddress" />.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The address.</returns>
    /// <exception cref="JsonException">Thrown when the property name is not a valid address.</exception>
    public override IPAddress ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadAddress(ref reader);
    }

    /// <summary>
    ///     Writes an address as a JSON string.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The address.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        // the address is formatted straight to UTF-8, so writing one creates no string
        Span<byte> addressText = stackalloc byte[MaxAddressTextLength];
        if (value.TryFormat(addressText, out var addressTextLength))
            writer.WriteStringValue(addressText[..addressTextLength]);
        else
            writer.WriteStringValue(value.ToString());
    }

    /// <summary>
    ///     Writes an address as the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The address.</param>
    /// <param name="options">The serializer options.</param>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        Span<byte> addressText = stackalloc byte[MaxAddressTextLength];
        if (value.TryFormat(addressText, out var addressTextLength))
            writer.WritePropertyName(addressText[..addressTextLength]);
        else
            writer.WritePropertyName(value.ToString());
    }

    /// <summary>
    ///     Parses the current JSON string or property name as an address.
    /// </summary>
    /// <param name="reader">The reader, positioned on the string or property name.</param>
    /// <returns>The address.</returns>
    /// <exception cref="JsonException">Thrown when the text is not a valid address.</exception>
    private static IPAddress ReadAddress(ref Utf8JsonReader reader)
    {
        // when the value is in one piece and has no escape sequences, its raw UTF-8 bytes are the text, and are parsed without
        // creating a string
        if (!reader.HasValueSequence && !reader.ValueIsEscaped && IPAddress.TryParseStrict(reader.ValueSpan, out var address))
            return address;

        var addressText = reader.GetString();
        if (IPAddress.TryParseStrict(addressText, out address))
            return address;

        // a JsonException, rather than a FormatException, lets the serializer add the path of the bad value to the message
        throw new JsonException($"'{addressText}' is not a valid IP address.");
    }
}
