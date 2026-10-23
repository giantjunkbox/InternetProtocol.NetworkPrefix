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
    public void Allocation_IPv4_03()
    {
        // blocks from the IANA special-purpose registry, including globally reachable assignments inside reserved blocks
        Assert.That(Network("192.0.0.0/24").Allocation, Is.EqualTo(AddressAllocation.ProtocolAssignments));
        Assert.That(Network("192.0.0.170").Allocation, Is.EqualTo(AddressAllocation.ProtocolAssignments));
        Assert.That(Network("192.0.0.9").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("192.0.0.10").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("192.0.0.8").Allocation, Is.EqualTo(AddressAllocation.Dummy));
        Assert.That(Network("192.88.99.0/24").Allocation, Is.EqualTo(AddressAllocation.SixToFour));
        Assert.That(Network("192.31.196.0/24").Allocation, Is.EqualTo(AddressAllocation.Global));
    }

    [Test]
    public void Allocation_IPv6_02()
    {
        Assert.That(Network("2001::/23").Allocation, Is.EqualTo(AddressAllocation.ProtocolAssignments));
        Assert.That(Network("2001::/32").Allocation, Is.EqualTo(AddressAllocation.Teredo));
        Assert.That(Network("2001:1::1").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("2001:20::/28").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("2002::/16").Allocation, Is.EqualTo(AddressAllocation.SixToFour));
        Assert.That(Network("64:ff9b::/96").Allocation, Is.EqualTo(AddressAllocation.IPv4Translation));
        Assert.That(Network("64:ff9b::a00:1").Allocation, Is.EqualTo(AddressAllocation.IPv4Translation));
        Assert.That(Network("100::/64").Allocation, Is.EqualTo(AddressAllocation.Discard));
        Assert.That(Network("100:0:0:1::/64").Allocation, Is.EqualTo(AddressAllocation.Dummy));
        Assert.That(Network("fec0::/10").Allocation, Is.EqualTo(AddressAllocation.SiteLocal));
        Assert.That(Network("5f00::/16").Allocation, Is.EqualTo(AddressAllocation.SegmentRouting));
    }

    [Test]
    public void Allocation_IPv6_03()
    {
        // an IPv4-mapped network is classified as the IPv4 network it maps, which is how a dual-stack server reports IPv4 clients
        Assert.That(Network("::ffff:10.0.0.1").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(Network("::ffff:8.8.8.8").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("::ffff:10.0.0.0/104").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(Network("::ffff:10.0.0.0/104").Class, Is.EqualTo(NetworkClass.A));

        // the whole mapped block covers every IPv4 address, so it has no single allocation
        Assert.That(Network("::ffff:0:0/96").Allocation, Is.EqualTo(AddressAllocation.Undefined));
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

    [Test]
    public void Allocation_Spans_01()
    {
        // a network is only global when all of it is; one that takes in a special-purpose block has no single allocation
        Assert.That(Network("126.0.0.0/7").Allocation, Is.EqualTo(AddressAllocation.Undefined), "includes 127.0.0.0/8, loopback");
        Assert.That(Network("64.0.0.0/2").Allocation, Is.EqualTo(AddressAllocation.Undefined), "includes 100.64.0.0/10 and others");
        Assert.That(Network("192.0.0.0/9").Allocation, Is.EqualTo(AddressAllocation.Undefined), "includes 192.0.2.0/24, documentation");
        Assert.That(Network("192.0.0.8/30").Allocation, Is.EqualTo(AddressAllocation.Undefined), "includes a dummy and two global addresses");
        Assert.That(Network("2000::/3").Allocation, Is.EqualTo(AddressAllocation.Undefined), "includes 2001:db8::/32 and 2002::/16");
        Assert.That(Network("2001:db8::/31").Allocation, Is.EqualTo(AddressAllocation.Undefined), "includes 2001:db8::/32, documentation");
        Assert.That(Network("4000::/3").Allocation, Is.EqualTo(AddressAllocation.Undefined), "not allocated");

        // global space with nothing special inside it is still global
        Assert.That(Network("8.0.0.0/7").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Network("2001:4860::/32").Allocation, Is.EqualTo(AddressAllocation.Global));
    }

    [Test]
    public void Allocation_Spans_02()
    {
        // a registered block keeps its own allocation, even with blocks of other allocations inside it
        Assert.That(Network("192.0.0.0/24").Allocation, Is.EqualTo(AddressAllocation.ProtocolAssignments));
        Assert.That(Network("2001::/23").Allocation, Is.EqualTo(AddressAllocation.ProtocolAssignments));
        Assert.That(Network("0.0.0.0/8").Allocation, Is.EqualTo(AddressAllocation.CurrentNetwork));
        Assert.That(Network("240.0.0.0/4").Allocation, Is.EqualTo(AddressAllocation.Experimental));

        // a network made up only of global blocks is global, even inside a block that is not
        Assert.That(Network("2001:20::/27").Allocation, Is.EqualTo(AddressAllocation.Global), "2001:20::/28 and 2001:30::/28");

        // part of a registered block that leaves out the blocks inside it takes the registered block's allocation
        Assert.That(Network("192.0.0.0/29").Allocation, Is.EqualTo(AddressAllocation.ProtocolAssignments));
        Assert.That(Network("::ffff:126.0.0.0/103").Allocation, Is.EqualTo(AddressAllocation.Undefined), "an IPv4-mapped 126.0.0.0/7");
    }

    [Test]
    public void GetNetworks_01()
    {
        Assert.That(IPNetwork.GetNetworks(AddressAllocation.Private), Is.EqualTo(Networks("10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "fc00::/7")));
        Assert.That(IPNetwork.GetNetworks(AddressAllocation.Documentation), Is.EqualTo(Networks("192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24", "2001:db8::/32", "3fff::/20")));
        Assert.That(IPNetwork.GetNetworks(AddressAllocation.Loopback), Is.EqualTo(Networks("127.0.0.0/8", "::1/128")));

        // a block with another allocation inside a registered block is left out of it
        var protocolAssignments = IPNetwork.GetNetworks(AddressAllocation.ProtocolAssignments);
        Assert.That(protocolAssignments, Does.Contain(Network("192.0.0.0/29")));
        Assert.That(protocolAssignments.Any(network => network.Contains(Address("192.0.0.9"))), Is.False);
        Assert.That(IPNetwork.GetNetworks(AddressAllocation.Global), Does.Contain(Network("192.0.0.9/32")));

        Assert.Throws<ArgumentOutOfRangeException>(() => IPNetwork.GetNetworks((AddressAllocation)999));
    }

    [Test]
    public void GetNetworks_02()
    {
        // every network has the allocation it was returned for, and together the allocations cover every address once
        var allNetworks = new List<IPNetwork>();
        foreach (var allocation in Enum.GetValues<AddressAllocation>())
        {
            var networks = IPNetwork.GetNetworks(allocation);
            Assert.That(networks.Select(network => network.Allocation), Is.All.EqualTo(allocation), allocation.ToString());
            allNetworks.AddRange(networks);
        }

        // apart from the IPv4-mapped block, whose addresses take the allocation of the IPv4 address that they map
        Assert.That(IPNetwork.Summarize(allNetworks), Is.EqualTo(IPNetwork.Exclude(Networks("0.0.0.0/0", "::/0"), Networks("::ffff:0:0/96"))));
        Assert.That(allNetworks.Aggregate(System.Numerics.BigInteger.Zero, (total, network) => total + network.TotalAddresses), Is.EqualTo(System.Numerics.BigInteger.Pow(2, 128)));
    }

    [Test]
    public void GetNetworks_03()
    {
        // an IPv4-mapped address is in no allocation's networks, and its IPv4 address is in the networks of its allocation
        var mappedAddress = Address("::ffff:8.8.8.8");
        Assert.That(mappedAddress.Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(Enum.GetValues<AddressAllocation>().SelectMany(IPNetwork.GetNetworks).Any(network => network.IsSupernetOf(IPNetwork.FromAddress(mappedAddress))), Is.False);
        Assert.That(IPNetwork.GetNetworks(AddressAllocation.Global).Any(network => network.IsSupernetOf(IPNetwork.FromAddress(mappedAddress.Unmap()))), Is.True);

        // the networks are worked out once, and can't be changed by a caller
        var privateNetworks = IPNetwork.GetNetworks(AddressAllocation.Private);
        Assert.That(IPNetwork.GetNetworks(AddressAllocation.Private), Is.SameAs(privateNetworks));
        Assert.That(privateNetworks is IList<IPNetwork> { IsReadOnly: true }, Is.True);
    }
}
