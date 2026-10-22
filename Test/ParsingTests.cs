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
using System.Net.Sockets;
using Bitvantage.InternetProtocol;
using static Test.TestNetworks;

namespace Test;

public class ParsingTests
{
    [Test]
    public void FromAddress_01()
    {
        // a host network holds one address
        Assert.That(IPNetwork.FromAddress(Address("10.1.2.3")), Is.EqualTo(new IPNetwork(Address("10.1.2.3"), 32)));
        Assert.That(IPNetwork.FromAddress(Address("2001:db8::1")), Is.EqualTo(new IPNetwork(Address("2001:db8::1"), 128)));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.FromAddress(null!));
    }

    [Test]
    public void FromMask_01()
    {
        var address = Address("10.1.2.3");

        Assert.That(IPNetwork.FromMask(address, Address("255.255.0.0")), Is.EqualTo(Network("10.1.0.0/16")));

        // a mask with a gap, or a mask of the other IP version, is not a valid mask for the address
        Assert.Throws<ArgumentException>(() => IPNetwork.FromMask(address, Address("255.0.255.0")));
        Assert.Throws<ArgumentException>(() => IPNetwork.FromMask(address, Address("ffff::")));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.FromMask(address, null!));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.FromMask(null!, Address("255.0.0.0")));
    }

    [Test]
    public void FromHostMask_01()
    {
        var address = Address("10.1.2.3");

        Assert.That(IPNetwork.FromHostMask(address, Address("0.0.0.255")), Is.EqualTo(Network("10.1.2.0/24")));

        // as a host mask, all zeros is a host network and all ones is every address
        Assert.That(IPNetwork.FromHostMask(address, Address("0.0.0.0")), Is.EqualTo(Network("10.1.2.3/32")));
        Assert.That(IPNetwork.FromHostMask(address, Address("255.255.255.255")), Is.EqualTo(Network("0.0.0.0/0")));

        Assert.Throws<ArgumentException>(() => IPNetwork.FromHostMask(address, Address("0.0.255.0")));
        Assert.Throws<ArgumentException>(() => IPNetwork.FromHostMask(address, Address("::ff")));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.FromHostMask(address, null!));
    }

    [Test]
    public void GetMask_01()
    {
        Assert.That(IPNetwork.GetNetworkMask(IPVersion.IPv4, 24), Is.EqualTo(Address("255.255.255.0")));
        Assert.That(IPNetwork.GetHostMask(IPVersion.IPv4, 24), Is.EqualTo(Address("0.0.0.255")));
        Assert.That(IPNetwork.GetNetworkMask(IPVersion.IPv6, 32), Is.EqualTo(Address("ffff:ffff::")));
        Assert.That(IPNetwork.GetHostMask(IPVersion.IPv6, 128), Is.EqualTo(Address("::")));

        Assert.Throws<ArgumentOutOfRangeException>(() => IPNetwork.GetNetworkMask(IPVersion.IPv4, 33));
        Assert.Throws<ArgumentOutOfRangeException>(() => IPNetwork.GetNetworkMask(IPVersion.IPv4, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => IPNetwork.GetHostMask(IPVersion.Undefined, 0));
    }

    [Test]
    public void Parse_HostMask_01()
    {
        // a mask that is not a network mask is read as a host mask, which is the form Cisco access control lists use
        Assert.That(IPNetwork.ParseStrict("10.0.0.0 0.0.0.255"), Is.EqualTo(Network("10.0.0.0/24")));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0/0.0.0.255"), Is.EqualTo(Network("10.0.0.0/24")));
        Assert.That(IPNetwork.ParseStrict("2001:db8:: ::ffff:ffff:ffff:ffff:ffff:ffff"), Is.EqualTo(Network("2001:db8::/32")));
        Assert.That(IPNetwork.TryParseStrict("10.0.0.0 0.0.255.0", out _), Is.False);

        // the all-zero and all-one masks are network masks
        Assert.That(IPNetwork.ParseStrict("10.0.0.0 0.0.0.0"), Is.EqualTo(Network("0.0.0.0/0")));
        Assert.That(IPNetwork.ParseStrict("10.0.0.1 255.255.255.255"), Is.EqualTo(Network("10.0.0.1/32")));
    }

    [Test]
    public void Parse_HostMask_02()
    {
        // a network written with its host mask parses back to the same network, except a /0 or /32, whose host masks are
        // also network masks
        for (var prefixLength = 1; prefixLength <= 31; prefixLength++)
        {
            var network = new IPNetwork(Address("10.20.30.40"), prefixLength);
            Assert.That(IPNetwork.ParseStrict(network.ToString(NetworkFormat.AddressAndHostMask)), Is.EqualTo(network), network.ToString());
        }

        Assert.That(IPNetwork.ParseStrict(Network("10.0.0.1/32").ToString(NetworkFormat.AddressAndHostMask)), Is.EqualTo(Network("0.0.0.0/0")));
    }

    [Test]
    public void Parse_Utf8_01()
    {
        Assert.That(IPNetwork.ParseStrict("10.0.0.0/24"u8), Is.EqualTo(Network("10.0.0.0/24")));
        Assert.That(IPNetwork.TryParseStrict("2001:db8:: ffff:ffff::"u8, out var network) && network == Network("2001:db8::/32"), Is.True);
        Assert.That(IPNetwork.TryParseStrict("010.0.0.0/8"u8, out _), Is.False);
        Assert.Throws<FormatException>(() => IPNetwork.ParseStrict("10.0.0.0/33"u8));

        // invalid UTF-8 fails to parse rather than throwing
        Assert.That(IPNetwork.TryParseStrict(new byte[] { 0x31, 0xff, 0x2e }, out _), Is.False);

        // text longer than the stack buffer, padded with whitespace, still parses
        var paddedText = System.Text.Encoding.UTF8.GetBytes(new string(' ', 300) + "10.0.0.0/8" + new string(' ', 300));
        Assert.That(IPNetwork.ParseStrict(paddedText), Is.EqualTo(Network("10.0.0.0/8")));
    }

    [Test]
    public void Parse_IPv4_01()
    {
        // host bits are cleared, so networks written with different host bits are equal
        var network = Network("10.1.2.3/8");

        Assert.That(network.BaseAddress, Is.EqualTo(Address("10.0.0.0")));
        Assert.That(network, Is.EqualTo(Network("10.0.0.0/8")));
        Assert.That(network.GetHashCode(), Is.EqualTo(Network("10.0.0.0/8").GetHashCode()));
    }

    [Test]
    public void Parse_IPv4_02()
    {
        // malformed text is rejected; TryParseStrict returns false instead of throwing, and ParseStrict throws a FormatException
        var malformedValues = new[]
        {
            "",
            "   ",
            "10.0.0.0/",
            "10.0.0.0/24/5",
            "10.0.0.0/33",
            "10.0.0.0/-1",
            "10.0.0.0/+8",
            "10.0.0.0/ 8 8",
            "10.0.0.0/4294967295",
            "10.0.0.0 255.0.255.0",
            "10.0.0.0 ffff::",
            "example.com/24",
            "10.0.0.0/24 garbage"
        };

        foreach (var malformedValue in malformedValues)
        {
            Assert.That(IPNetwork.TryParseStrict(malformedValue, out var network), Is.False, malformedValue);
            Assert.That(network, Is.EqualTo(default(IPNetwork)), malformedValue);
            Assert.Throws<FormatException>(() => IPNetwork.ParseStrict(malformedValue), malformedValue);
        }

        Assert.That(IPNetwork.TryParseStrict((string?)null, out _), Is.False);
        Assert.Throws<ArgumentNullException>(() => IPNetwork.ParseStrict((string)null!));
    }

    [Test]
    public void Parse_IPv4_03()
    {
        // surrounding whitespace is ignored, and an address may be separated from its mask by a slash or any run of whitespace
        var expected = Network("10.0.0.0/24");

        Assert.That(IPNetwork.ParseStrict(" 10.0.0.0/24 "), Is.EqualTo(expected));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0  255.255.255.0"), Is.EqualTo(expected));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0\t255.255.255.0"), Is.EqualTo(expected));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0/255.255.255.0"), Is.EqualTo(expected));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0/024"), Is.EqualTo(expected));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0/24".AsSpan()), Is.EqualTo(expected));
        Assert.That(IPNetwork.TryParseStrict("10.0.0.0/24".AsSpan(), out var parsedNetwork) && parsedNetwork == expected, Is.True);

        // a single address is a host network
        Assert.That(IPNetwork.ParseStrict("10.0.0.7"), Is.EqualTo(new IPNetwork(Address("10.0.0.7"), 32)));
    }

    [Test]
    public void Parse_IPv4_04()
    {
        // only the standard dotted-decimal form of IPv4 is accepted. IPAddress reads a leading zero as octal, so 010.0.0.0
        // would otherwise be 8.0.0.0, and it also accepts shorthand, whole-number and hexadecimal forms
        var nonstandardValues = new[]
        {
            "010.0.0.0/8",
            "10.0.0.010",
            "10.0.0/24",
            "10/8",
            "167772160/8",
            "0x0a.0.0.0/8",
            "10.0.0.0 255.255.255.00",
            "10.0.0.0/0255.255.255.0"
        };

        // Resolve rejects them too, rather than passing them to DNS, whose resolver could read them as an address its own way
        foreach (var nonstandardValue in nonstandardValues)
        {
            Assert.That(IPNetwork.TryParseStrict(nonstandardValue, out _), Is.False, nonstandardValue);
            Assert.Throws<FormatException>(() => IPNetwork.Resolve(nonstandardValue), nonstandardValue);
        }

        // five numbers is not an address in any form
        Assert.That(IPNetwork.TryParseStrict("10.0.0.0.0/8", out _), Is.False);

        // a lone zero is not a leading zero
        Assert.That(IPNetwork.ParseStrict("0.0.0.0/0"), Is.EqualTo(new IPNetwork(IPAddress.Any, 0)));
        Assert.That(IPNetwork.ParseStrict("10.0.0.0 255.0.0.0"), Is.EqualTo(Network("10.0.0.0/8")));

        // IPv4 written inside IPv6 follows the IPv6 rules
        Assert.That(IPNetwork.ParseStrict("::ffff:10.0.0.0/104").Version, Is.EqualTo(IPVersion.IPv6));
    }

    [Test]
    public void Parse_IPv4_05()
    {
        // this is why ParseStrict exists: the built-in parser reads a leading zero as octal
        Assert.That(IPNetwork.Parse("010.0.0.0/8"), Is.EqualTo(IPNetwork.Parse("8.0.0.0/8")));
        Assert.That(IPNetwork.TryParseStrict("010.0.0.0/8", out _), Is.False);
    }

    [Test]
    public void Parse_IPv6_01()
    {
        Assert.That(IPNetwork.ParseStrict("2001:db8::1/64"), Is.EqualTo(new IPNetwork(Address("2001:db8::"), 64)));
        Assert.That(IPNetwork.ParseStrict("2001:db8:: ffff:ffff::"), Is.EqualTo(Network("2001:db8::/32")));
        Assert.That(IPNetwork.ParseStrict("2001:db8::1"), Is.EqualTo(Network("2001:db8::1/128")));
        Assert.That(IPNetwork.TryParseStrict("2001:db8::/129", out _), Is.False);
    }

    [Test]
    public void Parse_IPv6_02()
    {
        // a scope ID identifies an interface rather than part of an address, so it is dropped
        var network = IPNetwork.ParseStrict("fe80::1%3/64");

        Assert.That(network.BaseAddress.ScopeId, Is.Zero);
        Assert.That(network, Is.EqualTo(Network("fe80::/64")));
    }

    [Test]
    [Category("Network")]
    public void Resolve_01()
    {
        // text that is already a network is parsed without a lookup
        Assert.That(IPNetwork.Resolve("10.0.0.0/24"), Is.EqualTo(Network("10.0.0.0/24")));

        // an address with an invalid suffix is malformed, not a host name
        Assert.Throws<FormatException>(() => IPNetwork.Resolve("10.0.0.0/33"));
        Assert.Throws<FormatException>(() => IPNetwork.Resolve("one.one.one.one/"));
        Assert.Throws<ArgumentOutOfRangeException>(() => IPNetwork.Resolve("one.one.one.one", AddressFamily.AppleTalk));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.Resolve(null!));
    }

    [Test]
    [Category("Network")]
    public void Resolve_02()
    {
        // ParseStrict does not look up host names, which is a network operation; Resolve does
        Assert.Throws<FormatException>(() => IPNetwork.ParseStrict("one.one.one.one/32"));

        var network = IPNetwork.Resolve("one.one.one.one/32", AddressFamily.InterNetwork);
        Assert.That(network, Is.AnyOf(Network("1.1.1.1/32"), Network("1.0.0.1/32")));
    }

    [Test]
    [Category("Network")]
    public async Task Resolve_03()
    {
        // the IPv4 mask decides that an IPv4 address is looked up
        var network = await IPNetwork.ResolveAsync("one.one.one.one 255.255.255.255");
        Assert.That(network, Is.AnyOf(Network("1.1.1.1/32"), Network("1.0.0.1/32")));
    }
}
