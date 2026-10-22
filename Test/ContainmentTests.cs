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
using Bitvantage.InternetProtocol;
using static Test.TestNetworks;

namespace Test;

public class ContainmentTests
{
    [Test]
    public void ContainedByOrEqual_IPv4_01()
    {
        var largerNetwork = Network("131.192.0.0/10");

        Assert.That(largerNetwork.ContainsOrEqual(Network("192.0.0.0/2")), Is.False);
        Assert.That(largerNetwork.ContainedByOrEqual(Network("192.0.0.0/2")), Is.False);
    }

    [Test]
    public void ContainedBy_IPv4_01()
    {
        var smallerNetwork = Network("10.20.30.0/24");

        Assert.That(smallerNetwork.ContainedBy(Network("10.20.0.0/16")), Is.True);
        Assert.That(smallerNetwork.ContainedBy(Network("10.20.30.0/24")), Is.False);
        Assert.That(smallerNetwork.ContainedBy(Network("10.20.30.0/25")), Is.False);
        Assert.That(smallerNetwork.ContainedBy(Network("10.21.0.0/16")), Is.False);

        Assert.That(smallerNetwork.ContainedByOrEqual(Network("10.20.0.0/16")), Is.True);
        Assert.That(smallerNetwork.ContainedByOrEqual(Network("10.20.30.0/24")), Is.True);
        Assert.That(smallerNetwork.ContainedByOrEqual(Network("10.20.30.0/25")), Is.False);
        Assert.That(smallerNetwork.ContainedByOrEqual(Network("10.21.0.0/16")), Is.False);
    }

    [Test]
    public void ContainedBy_IPv4_02()
    {
        // the contained-by pair is the inverse of the contains pair
        var largerNetwork = Network("10.20.0.0/16");
        var smallerNetwork = Network("10.20.30.0/24");

        Assert.That(smallerNetwork.ContainedBy(largerNetwork), Is.EqualTo(largerNetwork.Contains(smallerNetwork)));
        Assert.That(largerNetwork.ContainedBy(smallerNetwork), Is.EqualTo(smallerNetwork.Contains(largerNetwork)));
        Assert.That(smallerNetwork.ContainedByOrEqual(largerNetwork), Is.EqualTo(largerNetwork.ContainsOrEqual(smallerNetwork)));
        Assert.That(largerNetwork.ContainedByOrEqual(smallerNetwork), Is.EqualTo(smallerNetwork.ContainsOrEqual(largerNetwork)));
    }

    [Test]
    public void Contains_IPv4_01()
    {
        Assert.That(Network("0.0.0.0/1").Contains(Network("116.11.0.0/16")), Is.True);
    }

    [Test]
    public void Contains_IPv4_02()
    {
        var largerNetwork = Network("10.20.0.0/16");

        Assert.That(largerNetwork.Contains(Network("10.20.0.0/16")), Is.False);
        Assert.That(largerNetwork.Contains(Network("10.20.0.0/24")), Is.True);
        Assert.That(largerNetwork.Contains(Network("10.20.255.0/24")), Is.True);
        Assert.That(largerNetwork.Contains(Network("10.20.30.40/32")), Is.True);
        Assert.That(largerNetwork.Contains(Network("10.21.0.0/24")), Is.False);
        Assert.That(largerNetwork.Contains(Network("10.20.0.0/15")), Is.False);
    }

    [Test]
    public void Contains_IPv4_03()
    {
        // testing an address calls the built-in Contains, under which a host network contains the address it represents
        var hostNetwork = Network("10.20.30.40/32");

        Assert.That(hostNetwork.Contains(Address("10.20.30.40")), Is.True);
        Assert.That(hostNetwork.Contains(Address("10.20.30.41")), Is.False);

        var network = Network("10.20.30.0/24");

        Assert.That(network.Contains(Address("10.20.30.0")), Is.True);
        Assert.That(network.Contains(Address("10.20.30.255")), Is.True);
        Assert.That(network.Contains(Address("10.20.31.40")), Is.False);
    }

    [Test]
    public void Contains_IPv4_04()
    {
        // a network of the other IP version is never contained
        var everyIPv4Address = Network("0.0.0.0/0");

        Assert.That(everyIPv4Address.Contains(Network("::/128")), Is.False);
        Assert.That(everyIPv4Address.ContainsOrEqual(Network("::/0")), Is.False);
        Assert.That(everyIPv4Address.Contains(IPAddress.IPv6Any), Is.False);
    }

    [Test]
    public void Contains_IPv6_01()
    {
        var network = Network("2001:db8::/32");

        Assert.That(network.Contains(Network("2001:db8:1::/48")), Is.True);
        Assert.That(network.Contains(Network("2001:db9::/48")), Is.False);
        Assert.That(network.Contains(Address("2001:db8::1")), Is.True);
        Assert.That(network.Contains(Address("2001:db9::1")), Is.False);
    }

    [Test]
    public void ContainsOrEqual_IPv4_01()
    {
        var largerNetwork = Network("10.20.0.0/16");

        Assert.That(largerNetwork.ContainsOrEqual(Network("10.20.0.0/16")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(Network("10.20.0.0/24")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(Network("10.20.255.0/24")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(Network("10.20.30.40/32")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(Network("10.21.0.0/24")), Is.False);
        Assert.That(largerNetwork.ContainsOrEqual(Network("10.20.0.0/15")), Is.False);
    }

    [Test]
    public void Overlaps_IPv4_01()
    {
        var network = Network("10.20.0.0/16");

        Assert.That(network.Overlaps(Network("10.20.0.0/16")), Is.True);
        Assert.That(network.Overlaps(Network("10.20.30.0/24")), Is.True);
        Assert.That(network.Overlaps(Network("10.0.0.0/8")), Is.True);
        Assert.That(network.Overlaps(Network("10.21.0.0/16")), Is.False);
        Assert.That(network.Overlaps(Network("::/0")), Is.False);
    }
}
