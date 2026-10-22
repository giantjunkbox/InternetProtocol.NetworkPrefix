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

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Bitvantage.InternetProtocol;

/// <summary>
///     Converts between <see cref="IPAddress" /> objects and the <see cref="UInt128" /> integers that the library uses for bit
///     arithmetic, and computes the network and host masks for each prefix length.
/// </summary>
/// <remarks>
///     <para>
///     An address is held as an unsigned integer whose most significant bit is the first bit of the address, so the integer
///     value of an address increases in the same order as the address itself. IPv4 addresses occupy the low 32 bits of the
///     integer, and the upper 96 bits are always zero.
///     </para>
///     <para>
///     Every extension member converts its networks to integer form on each call, so these methods are on the hot path. The masks
///     come from tables built once, and the methods that run on every call keep their error paths in separate methods so that
///     the compiler can inline them.
///     </para>
/// </remarks>
internal static class AddressBits
{
    /// <summary>
    ///     The number of bits in an IPv4 address.
    /// </summary>
    internal const int IPv4BitLength = 32;

    /// <summary>
    ///     The number of bits in an IPv6 address.
    /// </summary>
    internal const int IPv6BitLength = 128;

    /// <summary>
    ///     The IPv4 host mask for each prefix length, from /0 to /32.
    /// </summary>
    private static readonly UInt128[] IPv4HostMasks = CreateHostMasks(IPv4BitLength);

    /// <summary>
    ///     The IPv4 network mask for each prefix length, from /0 to /32.
    /// </summary>
    private static readonly UInt128[] IPv4NetworkMasks = CreateNetworkMasks(IPv4HostMasks, uint.MaxValue);

    /// <summary>
    ///     The IPv6 host mask for each prefix length, from /0 to /128.
    /// </summary>
    private static readonly UInt128[] IPv6HostMasks = CreateHostMasks(IPv6BitLength);

    /// <summary>
    ///     The IPv6 network mask for each prefix length, from /0 to /128.
    /// </summary>
    private static readonly UInt128[] IPv6NetworkMasks = CreateNetworkMasks(IPv6HostMasks, UInt128.MaxValue);

    /// <summary>
    ///     Converts an IP address to its integer form.
    /// </summary>
    /// <param name="address">An IPv4 or IPv6 address.</param>
    /// <returns>The address as an unsigned integer. An IPv6 scope identifier is not part of the result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UInt128 FromIPAddress(IPAddress address)
    {
        // the bytes of an address are written in network order, which is big endian, so they are read back the same way. Each
        // version gets a buffer of its own size, which is cheaper than one buffer large enough for both
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            Span<byte> ipv4Bytes = stackalloc byte[IPv4BitLength / 8];
            address.TryWriteBytes(ipv4Bytes, out _);
            return BinaryPrimitives.ReadUInt32BigEndian(ipv4Bytes);
        }

        Span<byte> ipv6Bytes = stackalloc byte[IPv6BitLength / 8];
        address.TryWriteBytes(ipv6Bytes, out _);
        return BinaryPrimitives.ReadUInt128BigEndian(ipv6Bytes);
    }

    /// <summary>
    ///     Gets a value with every bit of an address of the specified version set.
    /// </summary>
    /// <param name="version">The IP version.</param>
    /// <returns><c>255.255.255.255</c> for IPv4, or <c>ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff</c> for IPv6, in integer form.</returns>
    internal static UInt128 GetAllBits(IPVersion version)
    {
        return version == IPVersion.IPv4 ? uint.MaxValue : UInt128.MaxValue;
    }

    /// <summary>
    ///     Gets the number of bits in an address of the specified version.
    /// </summary>
    /// <param name="version">The IP version, which must be IPv4 or IPv6.</param>
    /// <returns>32 for IPv4, or 128 for IPv6.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetBitLength(IPVersion version)
    {
        return version == IPVersion.IPv4 ? IPv4BitLength : IPv6BitLength;
    }

    /// <summary>
    ///     Gets the host mask for a prefix length, which has a one in every bit position that identifies a host.
    /// </summary>
    /// <param name="version">The IP version, which must be IPv4 or IPv6.</param>
    /// <param name="prefixLength">The prefix length, which must be valid for the version.</param>
    /// <returns>The host mask in integer form, for example <c>0.0.0.255</c> for an IPv4 /24.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UInt128 GetHostMaskBits(IPVersion version, int prefixLength)
    {
        return version == IPVersion.IPv4 ? IPv4HostMasks[prefixLength] : IPv6HostMasks[prefixLength];
    }

    /// <summary>
    ///     Gets the network mask for a prefix length, which has a one in every bit position that identifies the network.
    /// </summary>
    /// <param name="version">The IP version, which must be IPv4 or IPv6.</param>
    /// <param name="prefixLength">The prefix length, which must be valid for the version.</param>
    /// <returns>The network mask in integer form, for example <c>255.255.255.0</c> for an IPv4 /24.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UInt128 GetNetworkMaskBits(IPVersion version, int prefixLength)
    {
        return version == IPVersion.IPv4 ? IPv4NetworkMasks[prefixLength] : IPv6NetworkMasks[prefixLength];
    }

    /// <summary>
    ///     Gets the IP version of an address family.
    /// </summary>
    /// <param name="addressFamily">The address family.</param>
    /// <returns><see cref="IPVersion.IPv4" /> or <see cref="IPVersion.IPv6" />.</returns>
    /// <exception cref="NotSupportedException">Thrown when the address family is neither IPv4 nor IPv6.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IPVersion GetVersion(AddressFamily addressFamily)
    {
        if (addressFamily == AddressFamily.InterNetwork)
            return IPVersion.IPv4;

        if (addressFamily == AddressFamily.InterNetworkV6)
            return IPVersion.IPv6;

        ThrowUnsupportedAddressFamily(addressFamily);
        return IPVersion.Undefined;
    }

    /// <summary>
    ///     Builds the table of host masks for every prefix length of an address of a given width.
    /// </summary>
    /// <param name="bitLength">The number of bits in an address.</param>
    /// <returns>The host mask for each prefix length, indexed by prefix length.</returns>
    private static UInt128[] CreateHostMasks(int bitLength)
    {
        var hostMasks = new UInt128[bitLength + 1];
        for (var prefixLength = 0; prefixLength <= bitLength; prefixLength++)
        {
            var hostBitLength = bitLength - prefixLength;

            // a run of n ones in the least significant bits is 2^n - 1. A UInt128 shift count is taken modulo 128, so shifting
            // by 128 would shift by zero; the host mask of an IPv6 /0, which has every bit set, is the one case that needs the
            // full width
            hostMasks[prefixLength] = hostBitLength == IPv6BitLength ? UInt128.MaxValue : (UInt128.One << hostBitLength) - UInt128.One;
        }

        return hostMasks;
    }

    /// <summary>
    ///     Builds the table of network masks from the table of host masks.
    /// </summary>
    /// <param name="hostMasks">The host mask for each prefix length.</param>
    /// <param name="allBits">A value with every bit of an address set.</param>
    /// <returns>The network mask for each prefix length, indexed by prefix length.</returns>
    private static UInt128[] CreateNetworkMasks(UInt128[] hostMasks, UInt128 allBits)
    {
        // the network mask is every bit of the address that is not part of the host mask
        return hostMasks.Select(hostMask => allBits ^ hostMask).ToArray();
    }

    /// <summary>
    ///     Throws the exception for an address family that is neither IPv4 nor IPv6. It is kept out of the methods that call it
    ///     so that they stay small enough to inline.
    /// </summary>
    /// <param name="addressFamily">The address family.</param>
    [DoesNotReturn]
    private static void ThrowUnsupportedAddressFamily(AddressFamily addressFamily)
    {
        throw new NotSupportedException($"Only IPv4 and IPv6 addresses are supported. The address family {addressFamily} is not.");
    }

    /// <summary>
    ///     Converts an address in integer form to an <see cref="IPAddress" />.
    /// </summary>
    /// <param name="addressBits">The address in integer form.</param>
    /// <param name="version">The IP version of the address.</param>
    /// <returns>A new <see cref="IPAddress" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the version is neither IPv4 nor IPv6.</exception>
    internal static IPAddress ToIPAddress(UInt128 addressBits, IPVersion version)
    {
        switch (version)
        {
            case IPVersion.IPv4:
            {
                // an IPv4 address is only the low 32 bits of the integer
                Span<byte> addressBytes = stackalloc byte[IPv4BitLength / 8];
                BinaryPrimitives.WriteUInt32BigEndian(addressBytes, (uint)addressBits);
                return new IPAddress(addressBytes);
            }

            case IPVersion.IPv6:
            {
                Span<byte> addressBytes = stackalloc byte[IPv6BitLength / 8];
                BinaryPrimitives.WriteUInt128BigEndian(addressBytes, addressBits);
                return new IPAddress(addressBytes);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(version), version, "Only IPv4 and IPv6 are supported.");
        }
    }

    /// <summary>
    ///     Converts a network mask in integer form to a prefix length.
    /// </summary>
    /// <param name="networkMaskBits">The network mask in integer form.</param>
    /// <param name="version">The IP version of the mask.</param>
    /// <param name="prefixLength">When this method returns <see langword="true" />, the number of leading one bits in the mask.</param>
    /// <returns>
    ///     <see langword="true" /> when the mask is a run of ones followed by a run of zeros, such as <c>255.255.255.0</c>;
    ///     <see langword="false" /> for a mask with gaps, such as <c>255.0.255.0</c>.
    /// </returns>
    internal static bool TryGetPrefixLength(UInt128 networkMaskBits, IPVersion version, out int prefixLength)
    {
        // the host part of a valid mask is a run of ones in the least significant bits, such as 0.0.0.255. Adding one to such a
        // run carries all the way through it, so the run and the run plus one have no bits in common. Any gap in the run stops the
        // carry early and leaves a bit in common. For an IPv6 /0 the addition wraps around to zero, which also has no bits in common
        var hostMaskBits = ~networkMaskBits & GetAllBits(version);
        if ((hostMaskBits & (hostMaskBits + UInt128.One)) != UInt128.Zero)
        {
            prefixLength = 0;
            return false;
        }

        prefixLength = GetBitLength(version) - (int)UInt128.PopCount(hostMaskBits);
        return true;
    }

    /// <summary>
    ///     Gets the IP version of an address family.
    /// </summary>
    /// <param name="addressFamily">The address family.</param>
    /// <param name="version">When this method returns <see langword="true" />, the IP version of the address family.</param>
    /// <returns><see langword="true" /> when the address family is IPv4 or IPv6; otherwise <see langword="false" />.</returns>
    internal static bool TryGetVersion(AddressFamily addressFamily, out IPVersion version)
    {
        version = addressFamily switch
        {
            AddressFamily.InterNetwork => IPVersion.IPv4,
            AddressFamily.InterNetworkV6 => IPVersion.IPv6,
            _ => IPVersion.Undefined
        };

        return version != IPVersion.Undefined;
    }
}
