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

using Bitvantage.InternetProtocol;
using static Test.TestNetworks;

namespace Test;

public class ClassificationTests
{
    [Test]
    public void Allocation_IPv4_01()
    {
        Assert.That(Network("10.20.30.0/24").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(Network("14.20.30.0/24").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("100.64.0.0/10").Allocation, Is.EqualTo(AddressAllocation.SharedAddressSpace));
        Assert.That(Network("192.0.2.0/24").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(Network("203.0.113.7").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(Network("198.18.0.0/15").Allocation, Is.EqualTo(AddressAllocation.Benchmarking));
        Assert.That(Network("169.254.1.0/24").Allocation, Is.EqualTo(AddressAllocation.LinkLocal));
        Assert.That(Network("127.0.0.1").Allocation, Is.EqualTo(AddressAllocation.Loopback));
        Assert.That(Network("0.0.0.0/32").Allocation, Is.EqualTo(AddressAllocation.Unspecified));
        Assert.That(Network("0.1.2.3").Allocation, Is.EqualTo(AddressAllocation.CurrentNetwork));
        Assert.That(Network("239.1.2.3").Allocation, Is.EqualTo(AddressAllocation.Multicast));
        Assert.That(Network("250.0.0.0/8").Allocation, Is.EqualTo(AddressAllocation.Experimental));
        Assert.That(Network("255.255.255.255").Allocation, Is.EqualTo(AddressAllocation.Broadcast));
    }

    [Test]
    public void Allocation_IPv4_02()
    {
        // a network that spans blocks with different purposes has no single allocation
        Assert.That(Network("0.0.0.0/1").Allocation, Is.EqualTo(AddressAllocation.Undefined));
        Assert.That(Network("192.0.0.0/8").Allocation, Is.EqualTo(AddressAllocation.Undefined));
    }

    [Test]
    public void Allocation_IPv6_01()
    {
        Assert.That(Network("2606:4700::/32").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("fd12:3456::/48").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(Network("fe80::/64").Allocation, Is.EqualTo(AddressAllocation.LinkLocal));
        Assert.That(Network("::1").Allocation, Is.EqualTo(AddressAllocation.Loopback));
        Assert.That(Network("::").Allocation, Is.EqualTo(AddressAllocation.Unspecified));
        Assert.That(Network("ff02::1").Allocation, Is.EqualTo(AddressAllocation.Multicast));
        Assert.That(Network("2001:db8:1::/48").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(Network("3fff:100::/24").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(Network("2001:2::/48").Allocation, Is.EqualTo(AddressAllocation.Benchmarking));
        Assert.That(Network("::/0").Allocation, Is.EqualTo(AddressAllocation.Undefined));
    }

    [Test]
    public void Class_IPv4_01()
    {
        Assert.That(Network("10.20.30.0/24").Class, Is.EqualTo(NetworkClass.A));
        Assert.That(Network("172.16.0.0/12").Class, Is.EqualTo(NetworkClass.B));
        Assert.That(Network("192.168.100.0/24").Class, Is.EqualTo(NetworkClass.C));
        Assert.That(Network("224.0.0.1").Class, Is.EqualTo(NetworkClass.D));
        Assert.That(Network("255.255.255.255/32").Class, Is.EqualTo(NetworkClass.E));
    }

    [Test]
    public void Class_IPv4_02()
    {
        // a network that spans more than one class has no class, and classes are an IPv4 concept
        Assert.That(Network("0.0.0.0/0").Class, Is.EqualTo(NetworkClass.Undefined));
        Assert.That(Network("2001:db8::/32").Class, Is.EqualTo(NetworkClass.Undefined));
    }
}
