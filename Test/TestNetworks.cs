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
using Bitvantage.InternetProtocol;

namespace Test;

/// <summary>
///     Shorthand for creating the networks and addresses that the tests use.
/// </summary>
internal static class TestNetworks
{
    /// <summary>
    ///     Parses an address.
    /// </summary>
    public static IPAddress Address(string addressText)
    {
        return IPAddress.Parse(addressText);
    }

    /// <summary>
    ///     Parses a network with the strict parser, which accepts every format the tests use.
    /// </summary>
    public static IPNetwork Network(string networkText)
    {
        return IPNetwork.ParseStrict(networkText);
    }

    /// <summary>
    ///     Parses several networks with the strict parser.
    /// </summary>
    public static IPNetwork[] Networks(params string[] networkTexts)
    {
        return networkTexts.Select(networkText => IPNetwork.ParseStrict(networkText)).ToArray();
    }
}
