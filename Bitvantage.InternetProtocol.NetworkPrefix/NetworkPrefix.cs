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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Numerics;
using System.Text.Json.Serialization;
using Bitvantage.InternetProtocol.Converters;

namespace Bitvantage.InternetProtocol;

/// <summary>
///     An immutable IPv4 or IPv6 network prefix, such as <c>10.0.0.0/8</c> or <c>2001:db8::/32</c>.
/// </summary>
/// <remarks>
///     <para>
///         A prefix is a network address and a prefix length. The prefix length is the number of leading bits of the address
///         that identify the network, and the remaining bits identify a host within that network. Any host bits that are set in
///         the address a prefix is created from are cleared, so <c>10.1.2.3/8</c> becomes <c>10.0.0.0/8</c>.
///     </para>
///     <para>
///         A prefix converts implicitly to and from <see cref="IPNetwork" />, and <see cref="IPNetworkExtensions" /> adds the
///         functionality of this class to <see cref="IPNetwork" /> itself.
///     </para>
/// </remarks>
[JsonConverter(typeof(NetworkPrefixJsonConverter))]
public sealed partial class NetworkPrefix : IEquatable<NetworkPrefix>, IComparable<NetworkPrefix>, IComparable
{
    /// <summary>
    ///     Initializes a new prefix from an address and a prefix length.
    /// </summary>
    /// <param name="address">An address within the network. Any host bits that are set are cleared.</param>
    /// <param name="prefixLength">The number of leading bits that identify the network, from 0 to 32 for IPv4 or 0 to 128 for IPv6.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="prefixLength" /> is out of range for the IP version.</exception>
    public NetworkPrefix(IPAddress address, int prefixLength)
    {
        var version = GetVersionOf(address, nameof(address));

        ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixLength, AddressBits.GetBitLength(version));

        Initialize(AddressBits.FromIPAddress(address), prefixLength, version);
    }

    /// <summary>
    ///     Initializes a new host prefix, a /32 for IPv4 or a /128 for IPv6, that holds a single address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address.</exception>
    public NetworkPrefix(IPAddress address)
    {
        var version = GetVersionOf(address, nameof(address));

        Initialize(AddressBits.FromIPAddress(address), AddressBits.GetBitLength(version), version);
    }

    /// <summary>
    ///     Initializes a new prefix from an address and a network mask, such as <c>10.0.0.0</c> and <c>255.255.255.0</c>.
    /// </summary>
    /// <param name="address">An address within the network. Any host bits that are set are cleared.</param>
    /// <param name="networkMask">The network mask, which must be the same IP version as <paramref name="address" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address, or when <paramref name="networkMask" />
    ///     is not a valid network mask of the same IP version.
    /// </exception>
    public NetworkPrefix(IPAddress address, IPAddress networkMask)
    {
        var version = GetVersionOf(address, nameof(address));
        ArgumentNullException.ThrowIfNull(networkMask);

        if (!TryGetPrefixLength(networkMask, version, out var prefixLength))
            throw new ArgumentException($"'{networkMask}' is not a valid {version} network mask. A network mask is a run of one bits followed by a run of zero bits, such as 255.255.255.0.", nameof(networkMask));

        Initialize(AddressBits.FromIPAddress(address), prefixLength, version);
    }

    /// <summary>
    ///     Initializes a new prefix with the same network address and prefix length as an <see cref="IPNetwork" />.
    /// </summary>
    /// <param name="network">The network to copy.</param>
    public NetworkPrefix(IPNetwork network) : this(network.BaseAddress, network.PrefixLength)
    {
    }

    /// <summary>
    ///     Initializes a new prefix from an address in integer form that the caller has already validated, which avoids the cost
    ///     of converting the address through an <see cref="IPAddress" />.
    /// </summary>
    /// <param name="addressBits">An address within the network, in integer form. Any host bits that are set are cleared.</param>
    /// <param name="prefixLength">A prefix length that is valid for <paramref name="version" />.</param>
    /// <param name="version">The IP version.</param>
    private NetworkPrefix(UInt128 addressBits, int prefixLength, IPVersion version)
    {
        Initialize(addressBits, prefixLength, version);
    }

    /// <summary>
    ///     Initializes an empty prefix for <see cref="System.Xml.Serialization.XmlSerializer" />, which creates the instance first
    ///     and then fills it in through <see cref="System.Xml.Serialization.IXmlSerializable.ReadXml" />.
    /// </summary>
    private NetworkPrefix()
    {
        Initialize(UInt128.Zero, 0, IPVersion.IPv4);
    }

    /// <summary>
    ///     The number of bits in an address of this prefix's IP version: 32 for IPv4, or 128 for IPv6.
    /// </summary>
    public int AddressBitLength { get; private set; }

    /// <summary>
    ///     The last address in the network, in which every host bit is set. For example, <c>10.0.0.255</c> for <c>10.0.0.0/24</c>.
    /// </summary>
    /// <remarks>
    ///     IPv6 has no broadcast address. For an IPv6 prefix this is simply the last address, the same as <see cref="LastAddress" />,
    ///     and it can be assigned to a host.
    /// </remarks>
    public IPAddress BroadcastAddress => LastAddress;

    /// <summary>
    ///     Returns the other half of this prefix's supernet: the prefix of the same length whose network address differs only in
    ///     its last network bit. For example, the complementary prefix of <c>10.1.2.0/25</c> is <c>10.1.2.128/25</c>, and the other
    ///     way round.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the prefix length is 0, since such a prefix spans every address.</exception>
    public NetworkPrefix ComplementaryPrefix
    {
        get
        {
            if (PrefixLength == 0)
                throw new InvalidOperationException($"{this} spans every address of its version and has no complementary prefix.");

            // flip the last network bit, which is the bit immediately above the host bits
            var lastNetworkBit = UInt128.One << HostBitLength;
            return new NetworkPrefix(NetworkAddressBits ^ lastNetworkBit, PrefixLength, Version);
        }
    }

    /// <summary>
    ///     The first address in the network that can be assigned to a host.
    /// </summary>
    /// <remarks>
    ///     This is the address after the network address. For IPv4 the network address identifies the network, and for IPv6 it
    ///     is the subnet-router anycast address (RFC 4291). The exceptions are a /31 or /127 point-to-point prefix (RFC 3021 and
    ///     RFC 6164), where both addresses are usable, and a host prefix, which holds just one address.
    /// </remarks>
    public IPAddress FirstHost => ToIPAddress(FirstHostBits);

    /// <summary>
    ///     The number of bits of an address that identify a host within the network: <see cref="AddressBitLength" /> minus
    ///     <see cref="PrefixLength" />.
    /// </summary>
    public int HostBitLength => AddressBitLength - PrefixLength;

    /// <summary>
    ///     The host mask, which has a one in every bit that identifies a host. For example, <c>0.0.0.255</c> for a /24.
    /// </summary>
    /// <remarks>
    ///     The host mask is the inverse of the <see cref="NetworkMask" />, and Cisco access control lists call it a wildcard mask.
    /// </remarks>
    public IPAddress HostMask => ToIPAddress(HostMaskBits);

    /// <summary>
    ///     The host mask in integer form. See <see cref="HostMask" />.
    /// </summary>
    public UInt128 HostMaskBits { get; private set; }

    /// <summary>
    ///     Gets whether this is a host prefix, a /32 or a /128, which holds a single address.
    /// </summary>
    public bool IsHostPrefix => HostBitLength == 0;

    /// <summary>
    ///     The last address in the network, in which every host bit is set. For example, <c>10.0.0.255</c> for <c>10.0.0.0/24</c>.
    /// </summary>
    public IPAddress LastAddress => ToIPAddress(LastAddressBits);

    /// <summary>
    ///     The last address in the network that can be assigned to a host.
    /// </summary>
    /// <remarks>
    ///     For IPv4 this is the address before the broadcast address, except for a /31 point-to-point prefix (RFC 3021), where
    ///     both addresses are usable, and a /32 host prefix. IPv6 has no broadcast address, so for IPv6 this is the last address.
    /// </remarks>
    public IPAddress LastHost => ToIPAddress(LastHostBits);

    /// <summary>
    ///     The network address, which is the first address in the network and has every host bit cleared. For example,
    ///     <c>10.0.0.0</c> for <c>10.0.0.0/24</c>.
    /// </summary>
    public IPAddress NetworkAddress { get; private set; }

    /// <summary>
    ///     The network address in integer form. See <see cref="NetworkAddress" />.
    /// </summary>
    public UInt128 NetworkAddressBits { get; private set; }

    /// <summary>
    ///     The network mask, also called the subnet mask, which has a one in every bit that identifies the network. For example,
    ///     <c>255.255.255.0</c> for a /24.
    /// </summary>
    public IPAddress NetworkMask => ToIPAddress(NetworkMaskBits);

    /// <summary>
    ///     The network mask in integer form. See <see cref="NetworkMask" />.
    /// </summary>
    public UInt128 NetworkMaskBits { get; private set; }

    /// <summary>
    ///     The number of leading bits of an address that identify the network, from 0 to 32 for IPv4 or 0 to 128 for IPv6.
    /// </summary>
    public int PrefixLength { get; private set; }

    /// <summary>
    ///     The total number of addresses in the network, including the network and broadcast addresses.
    /// </summary>
    /// <remarks>
    ///     The count is a <see cref="BigInteger" /> because the 2^128 addresses of <c>::/0</c> do not fit in a <see cref="UInt128" />.
    /// </remarks>
    public BigInteger TotalAddresses => (BigInteger)HostMaskBits + BigInteger.One;

    /// <summary>
    ///     The number of addresses in the network that can be assigned to a host, from <see cref="FirstHost" /> to
    ///     <see cref="LastHost" />.
    /// </summary>
    /// <remarks>
    ///     For IPv4 this excludes the network and broadcast addresses. For IPv6 it excludes only the subnet-router anycast
    ///     address, since IPv6 has no broadcast address. A /31 or /127 point-to-point prefix and a host prefix have no excluded
    ///     addresses.
    /// </remarks>
    public UInt128 TotalHosts
    {
        get
        {
            return HostBitLength switch
            {
                // a host prefix holds one address, and a point-to-point prefix holds two, all of which are usable
                0 => UInt128.One,
                1 => 2,

                // every address, which is the host mask plus one, less the subnet-router anycast address
                _ when Version == IPVersion.IPv6 => HostMaskBits,

                // every address less the network and broadcast addresses
                _ => HostMaskBits - UInt128.One
            };
        }
    }

    /// <summary>
    ///     The version of the Internet Protocol of this prefix.
    /// </summary>
    public IPVersion Version { get; private set; }

    /// <summary>
    ///     The first host address in integer form. See <see cref="FirstHost" />.
    /// </summary>
    private UInt128 FirstHostBits => HostBitLength <= 1 ? NetworkAddressBits : NetworkAddressBits + UInt128.One;

    /// <summary>
    ///     The last address in integer form. See <see cref="LastAddress" />.
    /// </summary>
    private UInt128 LastAddressBits => NetworkAddressBits | HostMaskBits;

    /// <summary>
    ///     The last host address in integer form. See <see cref="LastHost" />.
    /// </summary>
    /// <remarks>
    ///     Only an IPv4 network of more than two addresses has a broadcast address to leave out.
    /// </remarks>
    private UInt128 LastHostBits => HostBitLength <= 1 || Version == IPVersion.IPv6 ? LastAddressBits : LastAddressBits - UInt128.One;

    /// <summary>
    ///     Compares this prefix with another object. See <see cref="CompareTo(NetworkPrefix)" />.
    /// </summary>
    /// <param name="obj">The object to compare with, which must be a <see cref="NetworkPrefix" /> or <see langword="null" />.</param>
    /// <returns>A negative number, zero, or a positive number when this prefix sorts before, the same as, or after <paramref name="obj" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="obj" /> is not a <see cref="NetworkPrefix" />.</exception>
    int IComparable.CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            NetworkPrefix otherNetwork => CompareTo(otherNetwork),
            _ => throw new ArgumentException($"Object must be of type {nameof(NetworkPrefix)}.", nameof(obj))
        };
    }

    /// <summary>
    ///     Compares this prefix with another prefix to determine their sort order.
    /// </summary>
    /// <remarks>
    ///     IPv4 prefixes sort before IPv6 prefixes. Within a version, prefixes sort by network address, and a shorter prefix sorts
    ///     before a longer prefix with the same network address, so a network always sorts before the networks it contains.
    ///     Every prefix sorts after <see langword="null" />.
    /// </remarks>
    /// <param name="other">The prefix to compare with.</param>
    /// <returns>A negative number, zero, or a positive number when this prefix sorts before, the same as, or after <paramref name="other" />.</returns>
    public int CompareTo(NetworkPrefix? other)
    {
        if (other is null)
            return 1;

        return Compare(Version, NetworkAddressBits, PrefixLength, other.Version, other.NetworkAddressBits, other.PrefixLength);
    }

    /// <summary>
    ///     Determines whether this prefix is strictly contained by another prefix: it is a smaller network inside the other.
    /// </summary>
    /// <param name="other">The prefix that might contain this one.</param>
    /// <returns><see langword="true" /> when <paramref name="other" /> contains this prefix and is not equal to it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other" /> is <see langword="null" />.</exception>
    public bool ContainedBy(NetworkPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return other.Contains(this);
    }

    /// <summary>
    ///     Determines whether this prefix is contained by another prefix or is equal to it.
    /// </summary>
    /// <param name="other">The prefix that might contain this one.</param>
    /// <returns><see langword="true" /> when every address of this prefix is in <paramref name="other" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other" /> is <see langword="null" />.</exception>
    public bool ContainedByOrEqual(NetworkPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return other.ContainsOrEqual(this);
    }

    /// <summary>
    ///     Determines whether an address is a member of this network.
    /// </summary>
    /// <remarks>
    ///     A host prefix contains the single address that it represents. An address of the other IP version is never a member.
    /// </remarks>
    /// <param name="address">The address.</param>
    /// <returns><see langword="true" /> when the address is in this network.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    public bool Contains(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!AddressBits.TryGetVersion(address.AddressFamily, out var addressVersion) || addressVersion != Version)
            return false;

        // the address is a member when its network bits are the same as this network's address
        return (AddressBits.FromIPAddress(address) & NetworkMaskBits) == NetworkAddressBits;
    }

    /// <summary>
    ///     Determines whether this prefix strictly contains another prefix: the other is a smaller network inside this one.
    /// </summary>
    /// <remarks>
    ///     A prefix does not strictly contain itself; use <see cref="ContainsOrEqual" /> to include that case. A prefix of the other
    ///     IP version is never contained.
    /// </remarks>
    /// <param name="other">The prefix that might be contained.</param>
    /// <returns><see langword="true" /> when this prefix contains <paramref name="other" /> and is not equal to it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other" /> is <see langword="null" />.</exception>
    public bool Contains(NetworkPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // a strictly contained network has a longer prefix, which rules out the equal case
        return PrefixLength < other.PrefixLength && ContainsOrEqual(other);
    }

    /// <summary>
    ///     Determines whether this prefix contains another prefix or is equal to it.
    /// </summary>
    /// <remarks>
    ///     A prefix of the other IP version is never contained.
    /// </remarks>
    /// <param name="other">The prefix that might be contained.</param>
    /// <returns><see langword="true" /> when every address of <paramref name="other" /> is in this prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other" /> is <see langword="null" />.</exception>
    public bool ContainsOrEqual(NetworkPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Version != other.Version)
            return false;

        // a network can't contain a network that is bigger than it is
        if (PrefixLength > other.PrefixLength)
            return false;

        // the other network is inside this one when its address, cut down to this network's prefix length, is this network's address
        return (other.NetworkAddressBits & NetworkMaskBits) == NetworkAddressBits;
    }

    /// <summary>
    ///     Determines whether this prefix is equal to another prefix: both have the same IP version, network address and prefix length.
    /// </summary>
    /// <param name="other">The prefix to compare with.</param>
    /// <returns><see langword="true" /> when the prefixes are equal.</returns>
    public bool Equals(NetworkPrefix? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Version == other.Version && PrefixLength == other.PrefixLength && NetworkAddressBits == other.NetworkAddressBits;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as NetworkPrefix);
    }

    /// <summary>
    ///     Creates a prefix with the same network address and prefix length as an <see cref="IPNetwork" />.
    /// </summary>
    /// <param name="network">The network to copy.</param>
    /// <returns>A new prefix.</returns>
    public static NetworkPrefix FromIPNetwork(IPNetwork network)
    {
        return new NetworkPrefix(network);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Version, PrefixLength, NetworkAddressBits);
    }

    /// <summary>
    ///     Determines whether this prefix and another prefix share at least one address, which happens when one contains or
    ///     is equal to the other.
    /// </summary>
    /// <param name="other">The other prefix.</param>
    /// <returns><see langword="true" /> when the two prefixes overlap.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other" /> is <see langword="null" />.</exception>
    public bool Overlaps(NetworkPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // two prefixes can't partly overlap; either they are disjoint, or one of them lies entirely within the other
        return ContainsOrEqual(other) || other.ContainsOrEqual(this);
    }

    /// <summary>
    ///     Creates an <see cref="IPNetwork" /> with the same network address and prefix length as this prefix.
    /// </summary>
    /// <returns>A new <see cref="IPNetwork" />.</returns>
    public IPNetwork ToIPNetwork()
    {
        return new IPNetwork(NetworkAddress, PrefixLength);
    }

    /// <summary>
    ///     Compares two prefixes, given as their parts, to determine their sort order. See <see cref="CompareTo(NetworkPrefix)" />.
    /// </summary>
    /// <param name="firstVersion">The IP version of the first prefix.</param>
    /// <param name="firstAddressBits">The network address of the first prefix in integer form.</param>
    /// <param name="firstPrefixLength">The prefix length of the first prefix.</param>
    /// <param name="secondVersion">The IP version of the second prefix.</param>
    /// <param name="secondAddressBits">The network address of the second prefix in integer form.</param>
    /// <param name="secondPrefixLength">The prefix length of the second prefix.</param>
    /// <returns>A negative number, zero, or a positive number when the first prefix sorts before, the same as, or after the second.</returns>
    internal static int Compare(IPVersion firstVersion, UInt128 firstAddressBits, int firstPrefixLength, IPVersion secondVersion, UInt128 secondAddressBits, int secondPrefixLength)
    {
        // IPv4 sorts before IPv6
        var versionComparison = ((int)firstVersion).CompareTo((int)secondVersion);
        if (versionComparison != 0)
            return versionComparison;

        // then the lower network address sorts first
        var addressComparison = firstAddressBits.CompareTo(secondAddressBits);
        if (addressComparison != 0)
            return addressComparison;

        // and when the network addresses are the same, the bigger network, with the shorter prefix, sorts first
        return firstPrefixLength.CompareTo(secondPrefixLength);
    }

    /// <summary>
    ///     Gets the IP version of an address that is used to create a prefix, and validates the address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="parameterName">The name of the parameter that holds the address, for the exception message.</param>
    /// <returns>The IP version of the address.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address" /> is neither an IPv4 nor an IPv6 address.</exception>
    private static IPVersion GetVersionOf([NotNull] IPAddress? address, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(address, parameterName);

        if (!AddressBits.TryGetVersion(address.AddressFamily, out var version))
            throw new ArgumentException($"Only IPv4 and IPv6 addresses are supported. The address family {address.AddressFamily} is not.", parameterName);

        return version;
    }

    /// <summary>
    ///     Sets every property of the prefix. This is called by every constructor, and by XML deserialization.
    /// </summary>
    /// <param name="addressBits">An address within the network, in integer form. Any host bits that are set are cleared.</param>
    /// <param name="prefixLength">A prefix length that is valid for <paramref name="version" />.</param>
    /// <param name="version">The IP version.</param>
    [MemberNotNull(nameof(NetworkAddress))]
    private void Initialize(UInt128 addressBits, int prefixLength, IPVersion version)
    {
        Version = version;
        PrefixLength = prefixLength;
        AddressBitLength = AddressBits.GetBitLength(version);
        HostMaskBits = AddressBits.GetHostMaskBits(version, prefixLength);
        NetworkMaskBits = AddressBits.GetNetworkMaskBits(version, prefixLength);

        // clear the host bits so that the prefix always holds its network address; for example 10.1.2.3/8 becomes 10.0.0.0/8
        NetworkAddressBits = addressBits & NetworkMaskBits;
        NetworkAddress = ToIPAddress(NetworkAddressBits);
    }

    /// <summary>
    ///     Gets the prefix length of a network mask.
    /// </summary>
    /// <param name="networkMask">The network mask.</param>
    /// <param name="version">The IP version that the mask must have.</param>
    /// <param name="prefixLength">When this method returns <see langword="true" />, the prefix length of the mask.</param>
    /// <returns><see langword="true" /> when the mask is a valid network mask of the required IP version.</returns>
    private static bool TryGetPrefixLength(IPAddress networkMask, IPVersion version, out int prefixLength)
    {
        // a mask of the other IP version does not apply to the address
        if (!AddressBits.TryGetVersion(networkMask.AddressFamily, out var maskVersion) || maskVersion != version)
        {
            prefixLength = 0;
            return false;
        }

        return AddressBits.TryGetPrefixLength(AddressBits.FromIPAddress(networkMask), version, out prefixLength);
    }

    /// <summary>
    ///     Converts an address of this prefix's IP version from integer form to an <see cref="IPAddress" />.
    /// </summary>
    /// <param name="addressBits">The address in integer form.</param>
    /// <returns>A new <see cref="IPAddress" />.</returns>
    private IPAddress ToIPAddress(UInt128 addressBits)
    {
        return AddressBits.ToIPAddress(addressBits, Version);
    }
}
