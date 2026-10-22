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

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bitvantage.InternetProtocol.Converters;

/// <summary>
///     Converts an <see cref="IPNetwork" /> to and from a JSON string in CIDR notation, such as <c>"10.0.0.0/24"</c>.
/// </summary>
/// <remarks>
///     <para>
///         System.Text.Json has no built-in support for <see cref="IPNetwork" />, and serializing one without a converter fails.
///         Register this converter to fix that:
///     </para>
///     <code>
///     var options = new JsonSerializerOptions();
///     options.Converters.Add(new IPNetworkJsonConverter());
///     </code>
///     <para>
///         It reads every format that <see cref="NetworkPrefix.Parse(string)" /> accepts, and supports networks as the keys of
///         a dictionary.
///     </para>
/// </remarks>
public sealed class IPNetworkJsonConverter : JsonConverter<IPNetwork>
{
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

        return NetworkPrefixJsonConverter.ParsePrefix(reader.GetString()).ToIPNetwork();
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
        return NetworkPrefixJsonConverter.ParsePrefix(reader.GetString()).ToIPNetwork();
    }

    /// <summary>
    ///     Writes a network as a JSON string in CIDR notation.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The network.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, IPNetwork value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    /// <summary>
    ///     Writes a network as the name of a JSON property, which is how a dictionary key is written.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The network.</param>
    /// <param name="options">The serializer options.</param>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, IPNetwork value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}
