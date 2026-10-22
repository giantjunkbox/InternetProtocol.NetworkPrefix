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
    public void IsSubnetOf_IPv4_01()
    {
        var largerNetwork = Network("131.192.0.0/10");

        Assert.That(largerNetwork.IsSupernetOf(Network("192.0.0.0/2")), Is.False);
        Assert.That(largerNetwork.IsSubnetOf(Network("192.0.0.0/2")), Is.False);
    }

    [Test]
    public void IsProperSubnetOf_IPv4_01()
    {
        var smallerNetwork = Network("10.20.30.0/24");

        Assert.That(smallerNetwork.IsProperSubnetOf(Network("10.20.0.0/16")), Is.True);
        Assert.That(smallerNetwork.IsProperSubnetOf(Network("10.20.30.0/24")), Is.False);
        Assert.That(smallerNetwork.IsProperSubnetOf(Network("10.20.30.0/25")), Is.False);
        Assert.That(smallerNetwork.IsProperSubnetOf(Network("10.21.0.0/16")), Is.False);

        Assert.That(smallerNetwork.IsSubnetOf(Network("10.20.0.0/16")), Is.True);
        Assert.That(smallerNetwork.IsSubnetOf(Network("10.20.30.0/24")), Is.True);
        Assert.That(smallerNetwork.IsSubnetOf(Network("10.20.30.0/25")), Is.False);
        Assert.That(smallerNetwork.IsSubnetOf(Network("10.21.0.0/16")), Is.False);
    }

    [Test]
    public void IsProperSubnetOf_IPv4_02()
    {
        // the subnet tests are the inverse of the supernet tests
        var largerNetwork = Network("10.20.0.0/16");
        var smallerNetwork = Network("10.20.30.0/24");

        Assert.That(smallerNetwork.IsProperSubnetOf(largerNetwork), Is.EqualTo(largerNetwork.IsProperSupernetOf(smallerNetwork)));
        Assert.That(largerNetwork.IsProperSubnetOf(smallerNetwork), Is.EqualTo(smallerNetwork.IsProperSupernetOf(largerNetwork)));
        Assert.That(smallerNetwork.IsSubnetOf(largerNetwork), Is.EqualTo(largerNetwork.IsSupernetOf(smallerNetwork)));
        Assert.That(largerNetwork.IsSubnetOf(smallerNetwork), Is.EqualTo(smallerNetwork.IsSupernetOf(largerNetwork)));
    }

    [Test]
    public void IsProperSupernetOf_IPv4_01()
    {
        Assert.That(Network("0.0.0.0/1").IsProperSupernetOf(Network("116.11.0.0/16")), Is.True);
    }

    [Test]
    public void IsProperSupernetOf_IPv4_02()
    {
        var largerNetwork = Network("10.20.0.0/16");

        Assert.That(largerNetwork.IsProperSupernetOf(Network("10.20.0.0/16")), Is.False);
        Assert.That(largerNetwork.IsProperSupernetOf(Network("10.20.0.0/24")), Is.True);
        Assert.That(largerNetwork.IsProperSupernetOf(Network("10.20.255.0/24")), Is.True);
        Assert.That(largerNetwork.IsProperSupernetOf(Network("10.20.30.40/32")), Is.True);
        Assert.That(largerNetwork.IsProperSupernetOf(Network("10.21.0.0/24")), Is.False);
        Assert.That(largerNetwork.IsProperSupernetOf(Network("10.20.0.0/15")), Is.False);
    }

    [Test]
    public void ContainsAddress_IPv4_01()
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
    public void IsProperSupernetOf_IPv4_03()
    {
        // a network of the other IP version is never a subnet, and an address of the other version is never a member
        var everyIPv4Address = Network("0.0.0.0/0");

        Assert.That(everyIPv4Address.IsProperSupernetOf(Network("::/128")), Is.False);
        Assert.That(everyIPv4Address.IsSupernetOf(Network("::/0")), Is.False);
        Assert.That(everyIPv4Address.Contains(IPAddress.IPv6Any), Is.False);
    }

    [Test]
    public void IsProperSupernetOf_IPv6_01()
    {
        var network = Network("2001:db8::/32");

        Assert.That(network.IsProperSupernetOf(Network("2001:db8:1::/48")), Is.True);
        Assert.That(network.IsProperSupernetOf(Network("2001:db9::/48")), Is.False);
        Assert.That(network.Contains(Address("2001:db8::1")), Is.True);
        Assert.That(network.Contains(Address("2001:db9::1")), Is.False);
    }

    [Test]
    public void IsSupernetOf_IPv4_01()
    {
        var largerNetwork = Network("10.20.0.0/16");

        Assert.That(largerNetwork.IsSupernetOf(Network("10.20.0.0/16")), Is.True);
        Assert.That(largerNetwork.IsSupernetOf(Network("10.20.0.0/24")), Is.True);
        Assert.That(largerNetwork.IsSupernetOf(Network("10.20.255.0/24")), Is.True);
        Assert.That(largerNetwork.IsSupernetOf(Network("10.20.30.40/32")), Is.True);
        Assert.That(largerNetwork.IsSupernetOf(Network("10.21.0.0/24")), Is.False);
        Assert.That(largerNetwork.IsSupernetOf(Network("10.20.0.0/15")), Is.False);
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
