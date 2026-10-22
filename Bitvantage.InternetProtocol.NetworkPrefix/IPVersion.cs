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
///     A version of the Internet Protocol.
/// </summary>
public enum IPVersion
{
    /// <summary>
    ///     No version. A <see cref="NetworkPrefix" /> never has this version.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///     Internet Protocol version 4, with 32 bit addresses.
    /// </summary>
    IPv4 = 1,

    /// <summary>
    ///     Internet Protocol version 6, with 128 bit addresses.
    /// </summary>
    IPv6 = 2
}
