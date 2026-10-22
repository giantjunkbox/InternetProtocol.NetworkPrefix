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

using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Bitvantage.InternetProtocol;

public sealed partial class NetworkPrefix : IXmlSerializable
{
    /// <summary>
    ///     Returns <see langword="null" />, as <see cref="IXmlSerializable" /> requires.
    /// </summary>
    /// <returns><see langword="null" />.</returns>
    XmlSchema? IXmlSerializable.GetSchema()
    {
        return null;
    }

    /// <summary>
    ///     Reads a prefix from an element whose text is the prefix in CIDR notation, such as
    ///     <c>&lt;NetworkPrefix&gt;10.0.0.0/24&lt;/NetworkPrefix&gt;</c>.
    /// </summary>
    /// <remarks>
    ///     <see cref="XmlSerializer" /> calls this on a new, empty instance. It is the only way to change a prefix after it has
    ///     been created, and it should not be called on a prefix that is already in use.
    /// </remarks>
    /// <param name="reader">The reader, positioned on the element.</param>
    /// <exception cref="XmlException">Thrown when the element is empty.</exception>
    /// <exception cref="FormatException">Thrown when the element's text is not a valid prefix.</exception>
    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        reader.MoveToContent();

        if (reader.IsEmptyElement)
            throw new XmlException($"The {reader.Name} element is empty, but it must hold a network prefix such as 10.0.0.0/24.");

        reader.ReadStartElement();
        var prefixText = reader.ReadContentAsString();

        if (!TryParse(prefixText, out var network))
            throw new FormatException($"'{prefixText}' is not a valid network prefix. {SupportedFormats}");

        Initialize(network.NetworkAddressBits, network.PrefixLength, network.Version);

        reader.ReadEndElement();
    }

    /// <summary>
    ///     Writes the prefix in CIDR notation as the text of the current element.
    /// </summary>
    /// <param name="writer">The writer.</param>
    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteString(ToString());
    }
}
