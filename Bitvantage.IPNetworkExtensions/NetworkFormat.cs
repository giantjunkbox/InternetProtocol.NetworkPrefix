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

namespace Bitvantage.InternetProtocol;

/// <summary>
///     The ways that the <c>ToString(NetworkFormat)</c> extension method on <see cref="System.Net.IPNetwork" /> can write a network.
/// </summary>
public enum NetworkFormat
{
    /// <summary>
    ///     The network address and the prefix length in CIDR notation, such as <c>10.0.0.0/24</c>. This is the default format.
    /// </summary>
    AddressAndPrefix,

    /// <summary>
    ///     A multi-line table of the network address, masks, broadcast address for IPv4, host range and host count, with each
    ///     address also written in binary.
    /// </summary>
    Detail,

    /// <summary>
    ///     The network address and the network mask separated by a space, such as <c>10.0.0.0 255.255.255.0</c>.
    /// </summary>
    AddressAndMask,

    /// <summary>
    ///     The network address alone, such as <c>10.0.0.0</c>.
    /// </summary>
    Address,

    /// <summary>
    ///     The network mask alone, such as <c>255.255.255.0</c>.
    /// </summary>
    Mask,

    /// <summary>
    ///     The network address and the host mask separated by a space, such as <c>10.0.0.0 0.0.0.255</c>. This is the form
    ///     that Cisco access control lists use, where the host mask is called a wildcard mask.
    /// </summary>
    AddressAndHostMask
}
