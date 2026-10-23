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
using static Test.TestNetworks;

namespace Test;

public class AddressTests
{
    [Test]
    public void AddressBits_IPv4_01()
    {
        Assert.That(Address("10.0.0.1").AddressBits, Is.EqualTo((UInt128)0x0A_00_00_01));
        Assert.That(IPAddress.FromAddressBits(0x0A_00_00_01, IPVersion.IPv4), Is.EqualTo(Address("10.0.0.1")));

        // the integer form is the same one that networks use
        Assert.That(Network("10.0.0.0/8").BaseAddressBits, Is.EqualTo(Address("10.0.0.0").AddressBits));
    }

    [Test]
    public void AddressBits_IPv6_01()
    {
        var address = Address("2001:db8::1");

        Assert.That(address.AddressBits, Is.EqualTo(new UInt128(0x2001_0db8_0000_0000, 1)));
        Assert.That(IPAddress.FromAddressBits(address.AddressBits, IPVersion.IPv6), Is.EqualTo(address));
    }

    [Test]
    public void AddressBits_IPv4_02()
    {
        // an IPv4 address must fit in 32 bits, and the version must be IPv4 or IPv6
        Assert.Throws<ArgumentOutOfRangeException>(() => IPAddress.FromAddressBits(0x1_0000_0000, IPVersion.IPv4));
        Assert.Throws<ArgumentOutOfRangeException>(() => IPAddress.FromAddressBits(0, IPVersion.Undefined));
        Assert.Throws<ArgumentNullException>(() => _ = ((IPAddress)null!).AddressBits);
    }

    [Test]
    public void Addition_IPv4_01()
    {
        Assert.That(Address("10.0.0.255") + 1, Is.EqualTo(Address("10.0.1.0")));
        Assert.That(Address("10.0.1.0") - 1, Is.EqualTo(Address("10.0.0.255")));
        Assert.That(Address("10.0.0.1") + 0, Is.EqualTo(Address("10.0.0.1")));

        // moving past either end of the address space throws
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Address("255.255.255.255") + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Address("0.0.0.0") - 1);
        Assert.Throws<ArgumentNullException>(() => _ = (IPAddress)null! + 1);
    }

    [Test]
    public void Addition_IPv6_01()
    {
        Assert.That(Address("2001:db8::ffff") + 1, Is.EqualTo(Address("2001:db8::1:0")));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff") + 1);

        // a link-local address stays on the same interface
        var movedAddress = Address("fe80::1%3") + 1;
        Assert.That(movedAddress, Is.EqualTo(Address("fe80::2%3")));
        Assert.That(movedAddress.ScopeId, Is.EqualTo(3));
    }

    [Test]
    public void Classification_IPv4_01()
    {
        Assert.That(Address("10.1.2.3").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(Address("8.8.8.8").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Address("10.1.2.3").Class, Is.EqualTo(NetworkClass.A));
        Assert.That(Address("10.1.2.3").Version, Is.EqualTo(IPVersion.IPv4));
    }

    [Test]
    public void Classification_IPv6_01()
    {
        // an IPv4-mapped address, which is how a dual-stack server reports an IPv4 client, is classified as the IPv4 address
        Assert.That(Address("::ffff:10.1.2.3").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(Address("::ffff:10.1.2.3").Class, Is.EqualTo(NetworkClass.A));
        Assert.That(Address("::ffff:8.8.8.8").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Address("2001:db8::1").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(Address("fe80::1%3").Allocation, Is.EqualTo(AddressAllocation.LinkLocal));
        Assert.That(Address("2001:db8::1").Version, Is.EqualTo(IPVersion.IPv6));
    }

    [Test]
    public void CompareTo_IPv4_01()
    {
        Assert.That(Address("10.0.0.1").CompareTo(Address("10.0.0.2")), Is.Negative);
        Assert.That(Address("10.0.0.2").CompareTo(Address("10.0.0.1")), Is.Positive);
        Assert.That(Address("10.0.0.1").CompareTo(Address("10.0.0.1")), Is.Zero);

        // IPv4 sorts before IPv6, and every address sorts after null
        Assert.That(Address("255.255.255.255").CompareTo(Address("::")), Is.Negative);
        Assert.That(Address("10.0.0.1").CompareTo(null), Is.Positive);
    }

    [Test]
    public void CompareTo_IPv6_01()
    {
        // addresses that differ only in scope ID are not equal, so they do not compare as the same
        Assert.That(Address("fe80::1%3").CompareTo(Address("fe80::1%4")), Is.Negative);
        Assert.That(Address("fe80::1%3").CompareTo(Address("fe80::1%3")), Is.Zero);
    }

    [Test]
    public void Comparer_01()
    {
        // IPAddress does not implement IComparable, so sorting it needs the comparer
        var addresses = new[] { Address("10.0.0.10"), Address("2001:db8::1"), null, Address("10.0.0.9"), Address("10.0.0.100") };

        Assert.Throws<InvalidOperationException>(() => addresses.Order().ToArray());
        Assert.That(addresses.Order(IPAddress.Comparer), Is.EqualTo(new[] { null, Address("10.0.0.9"), Address("10.0.0.10"), Address("10.0.0.100"), Address("2001:db8::1") }));
    }

    [Test]
    public void ComparisonOperators_01()
    {
        var lowerAddress = Address("10.0.0.1");
        var higherAddress = Address("10.0.0.2");
        IPAddress? missingAddress = null;

        Assert.That(lowerAddress < higherAddress, Is.True);
        Assert.That(higherAddress > lowerAddress, Is.True);
        Assert.That(lowerAddress <= Address("10.0.0.1"), Is.True);
        Assert.That(lowerAddress >= Address("10.0.0.1"), Is.True);
        Assert.That(higherAddress < lowerAddress, Is.False);

        // null sorts before every address, and comparing with null does not throw
        Assert.That(missingAddress < lowerAddress, Is.True);
        Assert.That(lowerAddress > missingAddress, Is.True);
    }

    [Test]
    public void Masks_IPv4_01()
    {
        Assert.That(Address("255.255.255.0").IsNetworkMask, Is.True);
        Assert.That(Address("255.255.255.0").TryGetPrefixLength(out var prefixLength) && prefixLength == 24, Is.True);
        Assert.That(Address("0.0.0.255").IsHostMask, Is.True);
        Assert.That(Address("0.0.0.255").IsNetworkMask, Is.False);
        Assert.That(Address("255.0.255.0").IsNetworkMask, Is.False);
        Assert.That(Address("255.0.255.0").IsHostMask, Is.False);
        Assert.That(Address("255.0.255.0").TryGetPrefixLength(out _), Is.False);

        // all zeros is both a network mask, for a /0, and a host mask, for a /32
        Assert.That(Address("0.0.0.0").IsNetworkMask && Address("0.0.0.0").IsHostMask, Is.True);
        Assert.That(Address("ffff:ffff::").TryGetPrefixLength(out var ipv6PrefixLength) && ipv6PrefixLength == 32, Is.True);
    }

    [Test]
    public void ParseStrict_01()
    {
        Assert.That(IPAddress.ParseStrict("10.0.0.1"), Is.EqualTo(Address("10.0.0.1")));
        Assert.That(IPAddress.ParseStrict(" 2001:db8::1 "), Is.EqualTo(Address("2001:db8::1")));
        Assert.That(IPAddress.ParseStrict("10.0.0.1".AsSpan()), Is.EqualTo(Address("10.0.0.1")));

        // a scope ID is kept
        Assert.That(IPAddress.ParseStrict("fe80::1%3").ScopeId, Is.EqualTo(3));
    }

    [Test]
    public void ParseStrict_02()
    {
        // the forms that IPAddress.Parse accepts but are rarely intended are rejected, including the octal reading of a leading zero
        Assert.That(IPAddress.Parse("010.0.0.1"), Is.EqualTo(Address("8.0.0.1")));

        foreach (var nonstandardValue in new[] { "010.0.0.1", "10.1", "167772161", "0x0a.0.0.1", "10.0.0.1/24", "", "10.0.0.256" })
        {
            Assert.That(IPAddress.TryParseStrict(nonstandardValue, out var address), Is.False, nonstandardValue);
            Assert.That(address, Is.Null, nonstandardValue);
            Assert.Throws<FormatException>(() => IPAddress.ParseStrict(nonstandardValue), nonstandardValue);
        }

        Assert.That(IPAddress.TryParseStrict((string?)null, out _), Is.False);
        Assert.Throws<ArgumentNullException>(() => IPAddress.ParseStrict((string)null!));
    }

    [Test]
    public void ParseStrict_Utf8_01()
    {
        Assert.That(IPAddress.ParseStrict("10.0.0.1"u8), Is.EqualTo(Address("10.0.0.1")));
        Assert.That(IPAddress.TryParseStrict(" 2001:db8::1 "u8, out var address) && address.Equals(Address("2001:db8::1")), Is.True);
        Assert.That(IPAddress.TryParseStrict("010.0.0.1"u8, out var octalAddress), Is.False);
        Assert.That(octalAddress, Is.Null);
        Assert.That(IPAddress.TryParseStrict(new byte[] { 0x31, 0xff }, out _), Is.False);
        Assert.Throws<FormatException>(() => IPAddress.ParseStrict("10.1"u8));
    }

    [Test]
    public void ReversePointer_01()
    {
        Assert.That(Address("1.2.3.4").ReversePointer, Is.EqualTo("4.3.2.1.in-addr.arpa"));
        Assert.That(Address("2001:db8::1").ReversePointer, Is.EqualTo("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa"));
    }

    [Test]
    public void Unmap_01()
    {
        var unmappedAddress = Address("2001:db8::1");

        Assert.That(Address("::ffff:10.1.2.3").Unmap(), Is.EqualTo(Address("10.1.2.3")));
        Assert.That(unmappedAddress.Unmap(), Is.SameAs(unmappedAddress));
        Assert.That(Address("10.1.2.3").Unmap(), Is.EqualTo(Address("10.1.2.3")));

        // unlike MapToIPv4, which turns any IPv6 address into an IPv4 address
        Assert.That(unmappedAddress.MapToIPv4(), Is.EqualTo(Address("0.0.0.1")));
    }

    [Test]
    public void ParseStrict_IPv6_01()
    {
        // endpoint text, and a scope ID that IPAddress ignores because it can't read it, are rejected
        foreach (var addressText in new[] { "[::1]", "[::1]:80", "[fe80::1%3]:80", "fe80::1%", "fe80::1%3%4", "fe80::1%4294967296", "fe80::1%no-such-interface" })
            Assert.That(IPAddress.TryParseStrict(addressText, out _), Is.False, addressText);

        Assert.That(IPAddress.ParseStrict("fe80::1%3").ScopeId, Is.EqualTo(3));
        Assert.That(IPAddress.ParseStrict("fe80::1%0").ScopeId, Is.Zero);
        Assert.That(IPAddress.ParseStrict("fe80::1%4294967295").ScopeId, Is.EqualTo(uint.MaxValue));
        Assert.That(IPAddress.ParseStrict("::ffff:10.0.0.1"), Is.EqualTo(Address("::ffff:10.0.0.1")));
    }

    [Test]
    public void ParseStrict_IPv6_02()
    {
        // an IPv4 address embedded in IPv6 text follows the same rules as one on its own
        foreach (var addressText in new[] { "::ffff:10.0.0.08", "::ffff:10.0.0.01", "64:ff9b::010.0.0.1" })
            Assert.That(IPAddress.TryParseStrict(addressText, out _), Is.False, addressText);

        Assert.That(IPAddress.ParseStrict("64:ff9b::10.0.0.1"), Is.EqualTo(Address("64:ff9b::a00:1")));
        Assert.That(IPNetwork.TryParseStrict("::ffff:10.0.0.08/128", out _), Is.False);
    }

    [Test]
    public void Addition_Int_01()
    {
        // an int variable compiles, and a negative count moves the other way
        var count = 2;
        var negativeCount = -2;

        Assert.That(Address("10.0.0.1") + count, Is.EqualTo(Address("10.0.0.3")));
        Assert.That(Address("10.0.0.1") + negativeCount, Is.EqualTo(Address("9.255.255.255")));
        Assert.That(Address("10.0.0.1") - count, Is.EqualTo(Address("9.255.255.255")));
        Assert.That(Address("10.0.0.1") - negativeCount, Is.EqualTo(Address("10.0.0.3")));
        Assert.That(Address("fe80::1%3") + count, Is.EqualTo(Address("fe80::3%3")));

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Address("0.0.0.1") + int.MinValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Address("255.255.255.255") - -1);
    }

    [Test]
    public void ToExpandedString_01()
    {
        Assert.That(Address("2001:db8::1").ToExpandedString(), Is.EqualTo("2001:0db8:0000:0000:0000:0000:0000:0001"));
        Assert.That(Address("::").ToExpandedString(), Is.EqualTo("0000:0000:0000:0000:0000:0000:0000:0000"));
        Assert.That(Address("FFFF:ffff:ffff:ffff:ffff:ffff:ffff:ffff").ToExpandedString(), Is.EqualTo("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"));
        Assert.That(Address("::ffff:10.0.0.1").ToExpandedString(), Is.EqualTo("0000:0000:0000:0000:0000:ffff:0a00:0001"));
        Assert.That(Address("fe80::1%3").ToExpandedString(), Is.EqualTo("fe80:0000:0000:0000:0000:0000:0000:0001%3"));
        Assert.That(Address("10.0.0.1").ToExpandedString(), Is.EqualTo("10.0.0.1"));

        // the expanded text parses back to the same address
        Assert.That(IPAddress.ParseStrict(Address("2001:db8:1::ab9:c0a8:102").ToExpandedString()), Is.EqualTo(Address("2001:db8:1::ab9:c0a8:102")));
    }
}
