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

public class OrderingTests
{
    [Test]
    public void Addition_IPv4_01()
    {
        Assert.That(Network("10.20.30.0/24") + 5, Is.EqualTo(Network("10.20.35.0/24")));
        Assert.That(Network("255.255.255.0/28") + 12, Is.EqualTo(Network("255.255.255.192/28")));
        Assert.That(Network("10.20.30.0/24") + 0, Is.EqualTo(Network("10.20.30.0/24")));
    }

    [Test]
    public void Addition_IPv4_02()
    {
        // moving past the end of the address space throws
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Network("255.255.255.0/28") + 16);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Network("255.255.255.0/28") + UInt128.MaxValue);
    }

    [Test]
    public void Addition_IPv6_01()
    {
        // ::/0 spans every IPv6 address, so it can only be moved by zero networks
        var everyAddress = Network("::/0");

        Assert.That(everyAddress + 0, Is.EqualTo(everyAddress));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = everyAddress + 1);

        // moving past the last /64 runs off the end of the address space
        Assert.That(Network("ffff:ffff:ffff:fffe::/64") + 1, Is.EqualTo(Network("ffff:ffff:ffff:ffff::/64")));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Network("ffff:ffff:ffff:ffff::/64") + 1);
    }

    [Test]
    public void CompareTo_IPv4_01()
    {
        var lowerNetwork = Network("10.0.0.0/16");
        var higherNetwork = Network("10.1.0.0/16");

        Assert.That(lowerNetwork.CompareTo(higherNetwork), Is.Negative);
        Assert.That(higherNetwork.CompareTo(lowerNetwork), Is.Positive);
        Assert.That(lowerNetwork.CompareTo(Network("10.0.0.0/16")), Is.Zero);
    }

    [Test]
    public void CompareTo_IPv4_02()
    {
        // with the same base address, the bigger network sorts first
        Assert.That(Network("10.1.0.0/16").CompareTo(Network("10.1.0.0/17")), Is.Negative);
    }

    [Test]
    public void CompareTo_IPv4_03()
    {
        // IPv4 sorts before IPv6
        Assert.That(Network("255.0.0.0/8").CompareTo(Network("::/0")), Is.Negative);
    }

    [Test]
    public void Comparer_IPv4_01()
    {
        // IPNetwork does not implement IComparable, so sorting needs the comparer
        var networks = Networks(
                "10.0.128.0/17",
                "2001:db8::/32",
                "10.0.64.0/18",
                "10.0.32.0/19",
                "10.0.16.0/20",
                "10.0.0.0/21",
                "10.0.12.0/22",
                "10.0.8.0/23",
                "10.0.11.0/24")
            .Order(IPNetwork.Comparer)
            .ToArray();

        var expectedResult = Networks(
            "10.0.0.0/21",
            "10.0.8.0/23",
            "10.0.11.0/24",
            "10.0.12.0/22",
            "10.0.16.0/20",
            "10.0.32.0/19",
            "10.0.64.0/18",
            "10.0.128.0/17",
            "2001:db8::/32");

        Assert.That(networks, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Comparer_IPv4_02()
    {
        // without the comparer, sorting IPNetwork fails at runtime
        var networks = Networks("10.0.1.0/24", "10.0.0.0/24");

        Assert.Throws<InvalidOperationException>(() => networks.Order().ToArray());

        var sortedNetworks = new SortedSet<IPNetwork>(networks, IPNetwork.Comparer);
        Assert.That(sortedNetworks.Min, Is.EqualTo(Network("10.0.0.0/24")));
    }

    [Test]
    public void ComparisonOperators_IPv4_01()
    {
        var lowerNetwork = Network("10.0.0.0/16");
        var higherNetwork = Network("10.1.0.0/16");
        var lowerNetworkCopy = Network("10.0.0.0/16");

        Assert.That(lowerNetwork < higherNetwork, Is.True);
        Assert.That(lowerNetwork <= lowerNetworkCopy, Is.True);
        Assert.That(higherNetwork > lowerNetwork, Is.True);
        Assert.That(higherNetwork >= lowerNetwork, Is.True);
        Assert.That(lowerNetwork >= lowerNetworkCopy, Is.True);
        Assert.That(higherNetwork < lowerNetwork, Is.False);
    }

    [Test]
    public void Subtraction_IPv4_01()
    {
        Assert.That(Network("10.20.30.0/24") - 15, Is.EqualTo(Network("10.20.15.0/24")));
        Assert.That(Network("128.0.0.0/1") - 1, Is.EqualTo(Network("0.0.0.0/1")));
    }

    [Test]
    public void Subtraction_IPv4_02()
    {
        // moving before the start of the address space throws
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Network("0.0.0.0/24") - 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Network("0.0.0.0/24") - UInt128.MaxValue);
    }
}
