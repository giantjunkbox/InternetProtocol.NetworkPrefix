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

namespace Bitvantage.InternetProtocol;

/// <summary>
///     The historical classes of the IPv4 address space, from before classless inter-domain routing.
/// </summary>
public enum NetworkClass
{
    /// <summary>
    ///     The prefix is an IPv6 prefix, or spans more than one class.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///     Class A, <c>0.0.0.0/1</c>.
    /// </summary>
    A = 1,

    /// <summary>
    ///     Class B, <c>128.0.0.0/2</c>.
    /// </summary>
    B = 2,

    /// <summary>
    ///     Class C, <c>192.0.0.0/3</c>.
    /// </summary>
    C = 3,

    /// <summary>
    ///     Class D, <c>224.0.0.0/4</c>, which is used for multicast.
    /// </summary>
    D = 4,

    /// <summary>
    ///     Class E, <c>240.0.0.0/4</c>, which is reserved.
    /// </summary>
    E = 5
}
