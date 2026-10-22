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

public class RangeTests
{
    [Test]
    public void GetContainingNetwork_IPv4_01()
    {
        Assert.That(IPNetwork.GetContainingNetwork(Address("10.0.0.1"), Address("10.0.0.9")), Is.EqualTo(Network("10.0.0.0/28")));
        Assert.That(IPNetwork.GetContainingNetwork(Address("10.0.0.1"), Address("10.0.0.1")), Is.EqualTo(Network("10.0.0.1/32")));
        Assert.Throws<ArgumentException>(() => IPNetwork.GetContainingNetwork(Address("10.0.0.1"), Address("2001:db8::1")));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.GetContainingNetwork(Address("10.0.0.1"), null!));
    }

    [Test]
    public void ParseRange_IPv4_01()
    {
        var expectedResult = Networks("10.0.0.1/32", "10.0.0.2/31", "10.0.0.4/30", "10.0.0.8/31");

        Assert.That(IPNetwork.ParseRange("10.0.0.1-10.0.0.9"), Is.EqualTo(expectedResult));
        Assert.That(IPNetwork.ParseRange(" 10.0.0.1 - 10.0.0.9 "), Is.EqualTo(expectedResult));
        Assert.That(IPNetwork.ParseRange("10.0.0.1-10.0.0.9".AsSpan()), Is.EqualTo(expectedResult));
    }

    [Test]
    public void ParseRange_IPv4_02()
    {
        // text without a dash is a single network
        Assert.That(IPNetwork.ParseRange("10.0.0.0/24"), Is.EqualTo(Networks("10.0.0.0/24")));
        Assert.That(IPNetwork.ParseRange("10.0.0.0 255.255.255.0"), Is.EqualTo(Networks("10.0.0.0/24")));
    }

    [Test]
    public void ParseRange_IPv4_03()
    {
        // shorthand, reversed ranges, mixed versions, nonstandard IPv4 and incomplete text are rejected
        var malformedValues = new[]
        {
            "192.168.10.10-20",
            "10.0.0.9-10.0.0.1",
            "10.0.0.1-2001:db8::1",
            "010.0.0.1-10.0.0.9",
            "10.0.0.1-",
            "-10.0.0.1",
            "10.0.0.1-10.0.0.2-10.0.0.3",
            "10.0.0.0/24-10.0.1.0/24",
            ""
        };

        foreach (var malformedValue in malformedValues)
        {
            Assert.That(IPNetwork.TryParseRange(malformedValue, out var networks), Is.False, malformedValue);
            Assert.That(networks, Is.Null, malformedValue);
            Assert.Throws<FormatException>(() => IPNetwork.ParseRange(malformedValue), malformedValue);
        }

        Assert.That(IPNetwork.TryParseRange((string?)null, out _), Is.False);
        Assert.Throws<ArgumentNullException>(() => IPNetwork.ParseRange((string)null!));
    }

    [Test]
    public void ParseRange_IPv6_01()
    {
        Assert.That(IPNetwork.ParseRange("2001:db8::-2001:db8::ff"), Is.EqualTo(Networks("2001:db8::/120")));
        Assert.That(IPNetwork.ParseRange("2001:db8::1-2001:db8::3"), Is.EqualTo(Networks("2001:db8::1/128", "2001:db8::2/127")));
    }

    [Test]
    public void Summarize_Range_IPv4_01()
    {
        // the example from Python's ipaddress.summarize_address_range
        var networks = IPNetwork.Summarize(Address("192.0.2.0"), Address("192.0.2.130"));

        Assert.That(networks, Is.EqualTo(Networks("192.0.2.0/25", "192.0.2.128/31", "192.0.2.130/32")));
    }

    [Test]
    public void Summarize_Range_IPv4_02()
    {
        Assert.That(IPNetwork.Summarize(Address("10.0.0.7"), Address("10.0.0.7")), Is.EqualTo(Networks("10.0.0.7/32")));
        Assert.That(IPNetwork.Summarize(Address("0.0.0.0"), Address("255.255.255.255")), Is.EqualTo(Networks("0.0.0.0/0")));
        Assert.That(IPNetwork.Summarize(Address("255.255.255.254"), Address("255.255.255.255")), Is.EqualTo(Networks("255.255.255.254/31")));
    }

    [Test]
    public void Summarize_Range_IPv4_03()
    {
        Assert.Throws<ArgumentException>(() => IPNetwork.Summarize(Address("10.0.0.9"), Address("10.0.0.1")));
        Assert.Throws<ArgumentException>(() => IPNetwork.Summarize(Address("10.0.0.1"), Address("2001:db8::1")));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.Summarize(null!, Address("10.0.0.1")));
    }

    [Test]
    public void Summarize_Range_IPv6_01()
    {
        // the whole IPv6 space holds 2^128 addresses, one more than a UInt128 can count
        Assert.That(IPNetwork.Summarize(Address("::"), Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")), Is.EqualTo(Networks("::/0")));
        Assert.That(IPNetwork.Summarize(Address("::1"), Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")).Count, Is.EqualTo(128));
        Assert.That(IPNetwork.Summarize(Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe"), Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")), Is.EqualTo(Networks("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe/127")));
    }
}
