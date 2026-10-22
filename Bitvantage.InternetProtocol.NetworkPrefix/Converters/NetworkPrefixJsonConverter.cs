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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bitvantage.InternetProtocol.Converters;

/// <summary>
///     Converts a <see cref="NetworkPrefix" /> to and from a JSON string in CIDR notation, such as <c>"10.0.0.0/24"</c>.
/// </summary>
/// <remarks>
///     <see cref="NetworkPrefix" /> uses this converter automatically. It also supports prefixes as the keys of a dictionary.
/// </remarks>
public sealed class NetworkPrefixJsonConverter : JsonConverter<NetworkPrefix>
{
    /// <summary>
    ///     Reads a prefix from a JSON string.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value.</param>
    /// <param name="typeToConvert">The type to convert, which is <see cref="NetworkPrefix" />.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The prefix.</returns>
    /// <exception cref="JsonException">Thrown when the value is not a string, or is not a valid prefix.</exception>
    public override NetworkPrefix Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"A network prefix must be a JSON string, such as \"10.0.0.0/24\", but the value is a {reader.TokenType}.");

        return ParsePrefix(reader.GetString());
    }

    /// <summary>
    ///     Reads a prefix from the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="reader">The reader, positioned on the property name.</param>
    /// <param name="typeToConvert">The type to convert, which is <see cref="NetworkPrefix" />.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The prefix.</returns>
    /// <exception cref="JsonException">Thrown when the property name is not a valid prefix.</exception>
    public override NetworkPrefix ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ParsePrefix(reader.GetString());
    }

    /// <summary>
    ///     Writes a prefix as a JSON string in CIDR notation.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The prefix.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, NetworkPrefix value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    /// <summary>
    ///     Writes a prefix as the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The prefix.</param>
    /// <param name="options">The serializer options.</param>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, NetworkPrefix value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }

    /// <summary>
    ///     Parses the text of a JSON string or property name as a prefix.
    /// </summary>
    /// <param name="prefixText">The text.</param>
    /// <returns>The prefix.</returns>
    /// <exception cref="JsonException">Thrown when the text is not a valid prefix.</exception>
    internal static NetworkPrefix ParsePrefix(string? prefixText)
    {
        // a JsonException, rather than a FormatException, lets the serializer add the path of the bad value to the message
        if (!NetworkPrefix.TryParse(prefixText, out var network))
            throw new JsonException($"'{prefixText}' is not a valid network prefix.");

        return network;
    }
}
