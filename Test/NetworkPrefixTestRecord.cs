/*
   Bitvantage.InternetProtocol.Network
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

namespace Test;

/// <summary>
///     The expected properties of a prefix built from an address and a prefix length.
/// </summary>
public record NetworkPrefixTestRecord
{
    /// <summary>
    ///     Creates a test case from the text of each address.
    /// </summary>
    public NetworkPrefixTestRecord(string address, int prefixLength, string networkAddress, string networkMask, string broadcastAddress, string hostMask, string firstHost, string lastHost)
    {
        Address = IPAddress.Parse(address);
        PrefixLength = prefixLength;
        NetworkAddress = IPAddress.Parse(networkAddress);
        NetworkMask = IPAddress.Parse(networkMask);
        BroadcastAddress = IPAddress.Parse(broadcastAddress);
        HostMask = IPAddress.Parse(hostMask);
        FirstHost = IPAddress.Parse(firstHost);
        LastHost = IPAddress.Parse(lastHost);
    }

    /// <summary>
    ///     The address that the prefix is built from, which may have host bits set.
    /// </summary>
    public IPAddress Address { get; init; }

    /// <summary>
    ///     The expected broadcast address.
    /// </summary>
    public IPAddress BroadcastAddress { get; init; }

    /// <summary>
    ///     The expected first host address.
    /// </summary>
    public IPAddress FirstHost { get; init; }

    /// <summary>
    ///     The expected host mask.
    /// </summary>
    public IPAddress HostMask { get; init; }

    /// <summary>
    ///     The expected last host address.
    /// </summary>
    public IPAddress LastHost { get; init; }

    /// <summary>
    ///     The expected network address, which is <see cref="Address" /> with the host bits cleared.
    /// </summary>
    public IPAddress NetworkAddress { get; init; }

    /// <summary>
    ///     The expected network mask.
    /// </summary>
    public IPAddress NetworkMask { get; init; }

    /// <summary>
    ///     The prefix length that the prefix is built with.
    /// </summary>
    public int PrefixLength { get; init; }
}