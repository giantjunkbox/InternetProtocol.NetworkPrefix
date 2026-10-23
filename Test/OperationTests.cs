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

public class OperationTests
{
    [Test]
    public void Addresses_IPv4_01()
    {
        Assert.That(Network("10.0.0.0/31").Addresses(), Is.EqualTo(new[] { Address("10.0.0.0"), Address("10.0.0.1") }));
        Assert.That(Network("10.0.0.0/32").Addresses(), Is.EqualTo(new[] { Address("10.0.0.0") }));
    }

    [Test]
    public void Addresses_IPv4_02()
    {
        var addresses = Network("10.0.0.0/28").Addresses().ToList();

        var expectedResult = Enumerable
            .Range(0, 16)
            .Select(lastOctet => Address($"10.0.0.{lastOctet}"))
            .ToList();

        Assert.That(addresses, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Addresses_IPv6_01()
    {
        // the last address of the IPv6 space is UInt128.MaxValue; enumeration must end there instead of wrapping around to ::
        var lastAddress = Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");

        Assert.That(IPNetwork.FromAddress(lastAddress).Addresses().ToList(), Is.EqualTo(new[] { lastAddress }));
        Assert.That(Network("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe/127").Addresses().Count(), Is.EqualTo(2));
    }

    [Test]
    public void Exclude_IPv4_01()
    {
        // the result is sorted
        var expectedResult = Networks(
            "10.0.0.0/21",
            "10.0.8.0/23",
            "10.0.11.0/24",
            "10.0.12.0/22",
            "10.0.16.0/20",
            "10.0.32.0/19",
            "10.0.64.0/18",
            "10.0.128.0/17");

        Assert.That(Network("10.0.0.0/16").Exclude(Network("10.0.10.0/24")), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Exclude_IPv4_02()
    {
        var expectedResult = Networks("10.20.30.0/32", "10.20.30.2/31", "10.20.30.4/30");

        Assert.That(Network("10.20.30.0/29").Exclude(Network("10.20.30.1/32")), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Exclude_IPv4_03()
    {
        var network = Network("10.20.30.0/24");

        // excluding the whole network, or a network that contains it, leaves nothing
        Assert.That(network.Exclude(Network("10.20.30.0/24")), Is.Empty);
        Assert.That(network.Exclude(Network("10.0.0.0/8")), Is.Empty);

        // excluding a network that does not overlap, including one of the other IP version, leaves the network unchanged
        Assert.That(network.Exclude(Network("10.20.31.0/24")), Is.EqualTo(new[] { network }));
        Assert.That(network.Exclude(Network("::/0")), Is.EqualTo(new[] { network }));
    }

    [Test]
    public void Exclude_IPv6_01()
    {
        var expectedResult = Networks("2001:db8::1/128", "2001:db8::2/127");

        Assert.That(Network("2001:db8::/126").Exclude(Network("2001:db8::/128")), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Exclude_IPv4_04()
    {
        // several networks at once, some of which lie outside the network or are of the other IP version
        var networksToExclude = Networks("10.0.0.0/26", "10.0.0.128/26", "10.0.0.200/32", "192.168.0.0/16", "2001:db8::/32");

        var expectedResult = Networks("10.0.0.64/26", "10.0.0.192/29", "10.0.0.201/32", "10.0.0.202/31", "10.0.0.204/30", "10.0.0.208/28", "10.0.0.224/27");

        Assert.That(Network("10.0.0.0/24").Exclude(networksToExclude), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Exclude_IPv4_05()
    {
        // overlapping networks to exclude are handled, and excluding nothing leaves the network
        Assert.That(Network("10.0.0.0/24").Exclude(Networks("10.0.0.0/25", "10.0.0.0/26")), Is.EqualTo(Networks("10.0.0.128/25")));
        Assert.That(Network("10.0.0.0/24").Exclude(Array.Empty<IPNetwork>()), Is.EqualTo(Networks("10.0.0.0/24")));
        Assert.Throws<ArgumentNullException>(() => Network("10.0.0.0/24").Exclude((IEnumerable<IPNetwork>)null!));
    }

    [Test]
    public void Exclude_IPv4_06()
    {
        // the set difference of two sets of networks
        var networks = Networks("10.0.0.0/24", "10.0.2.0/24", "2001:db8::/32");
        var networksToExclude = Networks("10.0.0.128/25", "10.0.1.0/24", "2001:db8::/33");

        Assert.That(IPNetwork.Exclude(networks, networksToExclude), Is.EqualTo(Networks("10.0.0.0/25", "10.0.2.0/24", "2001:db8:8000::/33")));

        // one network to exclude can cover several networks
        Assert.That(IPNetwork.Exclude(Networks("10.0.0.0/24", "10.0.2.0/24", "10.0.8.0/24"), Networks("10.0.0.0/22")), Is.EqualTo(Networks("10.0.8.0/24")));

        Assert.Throws<ArgumentNullException>(() => IPNetwork.Exclude(null!, networksToExclude));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.Exclude(networks, null!));
    }

    [Test]
    public void AvailableSubnets_IPv4_01()
    {
        // an address plan with the first /24 and half of the second allocated
        var plan = Network("10.0.0.0/22");
        var allocatedNetworks = Networks("10.0.0.0/24", "10.0.1.128/25", "2001:db8::/32");

        // the free half of the second /24 is too small for a /24, so the first free /24 is the third
        Assert.That(plan.AvailableSubnets(24, allocatedNetworks), Is.EqualTo(Networks("10.0.2.0/24", "10.0.3.0/24")));
        Assert.That(plan.AvailableSubnets(24, allocatedNetworks).First(), Is.EqualTo(Network("10.0.2.0/24")));
        Assert.That(plan.AvailableSubnets(25, allocatedNetworks), Is.EqualTo(Networks("10.0.1.0/25", "10.0.2.0/25", "10.0.2.128/25", "10.0.3.0/25", "10.0.3.128/25")));
    }

    [Test]
    public void AvailableSubnets_IPv4_02()
    {
        var plan = Network("10.0.0.0/22");

        Assert.That(plan.AvailableSubnets(22, Array.Empty<IPNetwork>()), Is.EqualTo(new[] { plan }));
        Assert.That(plan.AvailableSubnets(24, Networks("10.0.0.0/8")), Is.Empty);

        // the exceptions are thrown by the method itself, not when the result is enumerated
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AvailableSubnets(21, Array.Empty<IPNetwork>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AvailableSubnets(33, Array.Empty<IPNetwork>()));
        Assert.Throws<ArgumentNullException>(() => plan.AvailableSubnets(24, null!));
    }

    [Test]
    public void AvailableSubnets_IPv4_03()
    {
        // nothing is read until the result is enumerated, and each enumeration reads the allocations again, as LINQ methods do
        var enumerationCount = 0;
        IEnumerable<IPNetwork> CountedAllocations()
        {
            enumerationCount++;
            yield return Network("10.0.0.0/24");
        }

        var availableSubnets = Network("10.0.0.0/22").AvailableSubnets(24, CountedAllocations());
        Assert.That(enumerationCount, Is.Zero);

        Assert.That(availableSubnets.First(), Is.EqualTo(Network("10.0.1.0/24")));
        Assert.That(availableSubnets.Count(), Is.EqualTo(3));
        Assert.That(enumerationCount, Is.EqualTo(2));
    }

    [Test]
    public void AvailableSubnets_IPv6_01()
    {
        // the first free /64 of a /32 with one allocation comes straight away, though 2^32 - 1 more /64s follow it
        var plan = Network("2001:db8::/32");

        Assert.That(plan.AvailableSubnets(64, Networks("2001:db8::/64")).First(), Is.EqualTo(Network("2001:db8:0:1::/64")));
        Assert.That(plan.AvailableSubnets(64, Networks("2001:db8::/64")).Take(3), Is.EqualTo(Networks("2001:db8:0:1::/64", "2001:db8:0:2::/64", "2001:db8:0:3::/64")));
    }

    [Test]
    public void GetHalves_IPv4_01()
    {
        var (lowerHalf, upperHalf) = Network("10.0.0.0/24").GetHalves();

        Assert.That(lowerHalf, Is.EqualTo(Network("10.0.0.0/25")));
        Assert.That(upperHalf, Is.EqualTo(Network("10.0.0.128/25")));
        Assert.That(Network("10.0.0.0/24").GetHalves(), Is.EqualTo((Network("10.0.0.0/25"), Network("10.0.0.128/25"))));

        // the halves are the same two networks that Split returns
        Assert.That(new[] { lowerHalf, upperHalf }, Is.EqualTo(Network("10.0.0.0/24").Split()));
        Assert.Throws<InvalidOperationException>(() => Network("10.0.0.1/32").GetHalves());
    }

    [Test]
    public void GetHalves_IPv6_01()
    {
        Assert.That(Network("::/0").GetHalves(), Is.EqualTo((Network("::/1"), Network("8000::/1"))));
        Assert.That(Network("2001:db8::/127").GetHalves(), Is.EqualTo((Network("2001:db8::/128"), Network("2001:db8::1/128"))));
    }

    [Test]
    public void GetAddress_IPv4_01()
    {
        var network = Network("10.0.0.0/24");

        Assert.That(network.GetAddress(0), Is.EqualTo(Address("10.0.0.0")));
        Assert.That(network.GetAddress(10), Is.EqualTo(Address("10.0.0.10")));
        Assert.That(network.GetAddress(255), Is.EqualTo(Address("10.0.0.255")));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.GetAddress(256));
    }

    [Test]
    public void GetAddress_IPv6_01()
    {
        // every offset of ::/0 is valid, up to the last IPv6 address
        Assert.That(Network("::/0").GetAddress(UInt128.MaxValue), Is.EqualTo(Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));
        Assert.That(Network("2001:db8::/64").GetAddress(0x1_0000), Is.EqualTo(Address("2001:db8::1:0")));
    }

    [Test]
    public void GetOffset_IPv4_01()
    {
        var network = Network("10.0.0.0/24");

        Assert.That(network.GetOffset(Address("10.0.0.10")), Is.EqualTo((UInt128)10));
        Assert.That(network.GetOffset(network.GetAddress(200)), Is.EqualTo((UInt128)200));

        // an address outside the network, of the other IP version, or IPv4-mapped is not in the network
        Assert.Throws<ArgumentException>(() => network.GetOffset(Address("10.0.1.0")));
        Assert.Throws<ArgumentException>(() => network.GetOffset(Address("2001:db8::1")));
        Assert.Throws<ArgumentException>(() => network.GetOffset(Address("::ffff:10.0.0.10")));
        Assert.That(network.GetOffset(Address("::ffff:10.0.0.10").Unmap()), Is.EqualTo((UInt128)10));
        Assert.Throws<ArgumentNullException>(() => network.GetOffset(null!));
    }

    [Test]
    public void GetContainingNetwork_IPv4_01()
    {
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.0.0.0/24"), Network("10.0.1.0/24")), Is.EqualTo(Network("10.0.0.0/23")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("200.0.0.0/24"), Network("10.0.1.0/24")), Is.EqualTo(Network("0.0.0.0/0")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.0.1.0/24"), Network("10.0.1.0/24")), Is.EqualTo(Network("10.0.1.0/24")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.0.1.0/24"), Network("10.0.1.0/23")), Is.EqualTo(Network("10.0.0.0/23")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.0.1.0/32"), Network("0.0.0.0/0")), Is.EqualTo(Network("0.0.0.0/0")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("0.0.0.0/1"), Network("0.0.0.0/2")), Is.EqualTo(Network("0.0.0.0/1")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("0.0.0.0/2"), Network("255.255.255.0/24")), Is.EqualTo(Network("0.0.0.0/0")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_02()
    {
        // when one network contains the other, the containing network is the less specific of the two,
        // even when the two do not share the same base address
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.20.0.0/16"), Network("10.20.30.0/24")), Is.EqualTo(Network("10.20.0.0/16")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.20.30.0/24"), Network("10.20.0.0/16")), Is.EqualTo(Network("10.20.0.0/16")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("0.0.0.0/1"), Network("10.20.30.0/24")), Is.EqualTo(Network("0.0.0.0/1")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("10.20.30.0/24"), Network("10.20.30.40/32")), Is.EqualTo(Network("10.20.30.0/24")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_03()
    {
        Assert.That(IPNetwork.GetContainingNetwork(Networks("1.0.0.0/8", "2.0.0.0/8", "3.0.0.0/8")), Is.EqualTo(Network("0.0.0.0/6")));
        Assert.That(IPNetwork.GetContainingNetwork(Networks("10.0.0.0/24", "10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24")), Is.EqualTo(Network("10.0.0.0/22")));
        Assert.That(IPNetwork.GetContainingNetwork(Networks("10.0.0.0/24", "10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24", "127.255.255.0/24")), Is.EqualTo(Network("0.0.0.0/1")));
        Assert.That(IPNetwork.GetContainingNetwork(Networks("10.0.0.0/24", "10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24", "254.0.0.0/24")), Is.EqualTo(Network("0.0.0.0/0")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_04()
    {
        Assert.Throws<ArgumentException>(() => IPNetwork.GetContainingNetwork(Network("10.0.0.0/8"), Network("2001:db8::/32")));
        Assert.Throws<ArgumentException>(() => IPNetwork.GetContainingNetwork(Array.Empty<IPNetwork>()));
        Assert.Throws<ArgumentNullException>(() => IPNetwork.GetContainingNetwork((IEnumerable<IPNetwork>)null!));
    }

    [Test]
    public void GetContainingNetwork_IPv6_01()
    {
        Assert.That(IPNetwork.GetContainingNetwork(Network("2001:db8::/32"), Network("2001:db8:1::/48")), Is.EqualTo(Network("2001:db8::/32")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("2001:db8::/48"), Network("2001:db8:1::/48")), Is.EqualTo(Network("2001:db8::/47")));

        // a /0 stays IPv6
        Assert.That(IPNetwork.GetContainingNetwork(Network("::/0"), Network("2001:db8::/32")), Is.EqualTo(Network("::/0")));
        Assert.That(IPNetwork.GetContainingNetwork(Network("::/1"), Network("8000::/1")), Is.EqualTo(Network("::/0")));
    }

    [Test]
    public void GetSupernet_IPv4_01()
    {
        var network = Network("10.0.1.0/24");

        Assert.That(network.GetSupernet(), Is.EqualTo(Network("10.0.0.0/23")));
        Assert.That(network.GetSupernet(16), Is.EqualTo(Network("10.0.0.0/16")));
        Assert.That(network.GetSupernet(0), Is.EqualTo(Network("0.0.0.0/0")));
    }

    [Test]
    public void GetSupernet_IPv4_02()
    {
        var network = Network("10.0.1.0/24");

        // a supernet can't be longer than the network, a supernet of the same length is the network itself, and a /0 has no
        // supernet one bit shorter
        Assert.That(network.GetSupernet(24), Is.EqualTo(network));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.GetSupernet(25));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.GetSupernet(-1));
        Assert.Throws<InvalidOperationException>(() => Network("0.0.0.0/0").GetSupernet());
    }

    [Test]
    public void Hosts_IPv4_01()
    {
        Assert.That(Network("10.0.0.0/30").Hosts(), Is.EqualTo(new[] { Address("10.0.0.1"), Address("10.0.0.2") }));
        Assert.That(Network("10.0.0.0/31").Hosts(), Is.EqualTo(new[] { Address("10.0.0.0"), Address("10.0.0.1") }));
        Assert.That(Network("10.0.0.7/32").Hosts(), Is.EqualTo(new[] { Address("10.0.0.7") }));
    }

    [Test]
    public void Hosts_IPv6_01()
    {
        // the hosts of a point-to-point network at the very end of the address space must end rather than wrap around
        var hosts = Network("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe/127").Hosts();

        Assert.That(hosts, Is.EqualTo(new[] { Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe"), Address("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff") }));
    }

    [Test]
    public void Hosts_IPv6_02()
    {
        // every address but the subnet-router anycast address is a host
        var hosts = Network("2001:db8::/126").Hosts();

        Assert.That(hosts, Is.EqualTo(new[] { Address("2001:db8::1"), Address("2001:db8::2"), Address("2001:db8::3") }));
    }

    [Test]
    public void Split_IPv4_01()
    {
        var network = Network("10.20.30.0/24");

        Assert.That(network.Split(), Is.EqualTo(Networks("10.20.30.0/25", "10.20.30.128/25")));
        Assert.That(network.Split(26), Is.EqualTo(Networks("10.20.30.0/26", "10.20.30.64/26", "10.20.30.128/26", "10.20.30.192/26")));
    }

    [Test]
    public void Split_IPv4_02()
    {
        // a network can be split into host networks, and a point to point network splits into its two host networks
        Assert.That(Network("10.20.30.0/30").Split(32), Is.EqualTo(Networks("10.20.30.0/32", "10.20.30.1/32", "10.20.30.2/32", "10.20.30.3/32")));
        Assert.That(Network("10.20.30.0/31").Split(), Is.EqualTo(Networks("10.20.30.0/32", "10.20.30.1/32")));
    }

    [Test]
    public void Split_IPv4_03()
    {
        var network = Network("10.20.30.0/24");

        // the exceptions are thrown by Split itself, not when the result is enumerated
        Assert.Throws<InvalidOperationException>(() => Network("10.20.30.40/32").Split());
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(33));

        // splitting to the network's own length produces the network itself, including for the whole IPv6 space
        Assert.That(network.Split(24), Is.EqualTo(new[] { network }));
        Assert.That(Network("::/0").Split(0), Is.EqualTo(Networks("::/0")));
    }

    [Test]
    public void Split_IPv6_01()
    {
        Assert.That(Network("2001:db8::/127").Split(), Is.EqualTo(Networks("2001:db8::/128", "2001:db8::1/128")));
    }

    [Test]
    public void Split_IPv6_02()
    {
        var everyAddress = Network("::/0");

        Assert.That(everyAddress.Split(), Is.EqualTo(Networks("::/1", "8000::/1")));

        // splitting ::/0 into /128s produces 2^128 networks, one more than a UInt128 can count
        Assert.That(everyAddress.Split(128).Take(3), Is.EqualTo(Networks("::/128", "::1/128", "::2/128")));
    }

    [Test]
    public void Summarize_IPv4_01()
    {
        var networks = Networks(
            "10.0.8.0/23",
            "10.0.10.0/24",
            "10.0.11.0/24",
            "10.0.12.0/22",
            "10.0.128.0/18",
            "10.0.192.0/18",
            "100.0.0.100/32",
            "100.0.0.101/32",
            "100.0.0.102/32");

        var expectedResult = Networks("10.0.8.0/21", "10.0.128.0/17", "100.0.0.100/31", "100.0.0.102/32");

        Assert.That(IPNetwork.Summarize(networks), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Summarize_IPv4_02()
    {
        // a network that covers every other network is the whole summary
        var networks = Networks("0.0.0.0/0", "10.0.8.0/23", "10.0.128.0/18", "100.0.0.100/32");

        Assert.That(IPNetwork.Summarize(networks), Is.EqualTo(Networks("0.0.0.0/0")));
    }

    [Test]
    public void Summarize_IPv4_03()
    {
        // duplicates are removed, and a network listed together with its own halves is kept once
        Assert.That(IPNetwork.Summarize(Networks("10.0.0.0/24", "10.0.0.0/24")), Is.EqualTo(Networks("10.0.0.0/24")));
        Assert.That(IPNetwork.Summarize(Networks("10.0.0.0/24", "10.0.0.0/25", "10.0.0.128/25")), Is.EqualTo(Networks("10.0.0.0/24")));
    }

    [Test]
    public void Summarize_IPv4_04()
    {
        // merging cascades: two /26s merge into a /25, which then merges with the other /25
        Assert.That(IPNetwork.Summarize(Networks("10.0.0.192/26", "10.0.0.0/25", "10.0.0.128/26")), Is.EqualTo(Networks("10.0.0.0/24")));
    }

    [Test]
    public void Summarize_IPv4_05()
    {
        // two networks of the same size that are next to each other but not halves of the same supernet are not merged
        var networks = Networks("10.0.1.0/24", "10.0.2.0/24");

        Assert.That(IPNetwork.Summarize(networks), Is.EqualTo(networks));
        Assert.That(IPNetwork.Summarize(Array.Empty<IPNetwork>()), Is.Empty);
        Assert.Throws<ArgumentNullException>(() => IPNetwork.Summarize(null!));
    }

    [Test]
    public void Summarize_IPv6_01()
    {
        // IPv4 and IPv6 are summarized separately, and the result is sorted with IPv4 first
        var networks = Networks("2001:db8:1::/48", "10.0.0.0/9", "2001:db8::/48", "10.128.0.0/9");

        Assert.That(IPNetwork.Summarize(networks), Is.EqualTo(Networks("10.0.0.0/8", "2001:db8::/47")));
    }

    [Test]
    public void GetContainingNetwork_MixedVersions_01()
    {
        // the exception names the parameter of the overload that was called
        var twoNetworksException = Assert.Throws<ArgumentException>(() => IPNetwork.GetContainingNetwork(Network("10.0.0.0/8"), Network("2001:db8::/32")));
        var networkSetException = Assert.Throws<ArgumentException>(() => IPNetwork.GetContainingNetwork(Networks("10.0.0.0/8", "2001:db8::/32")));
        var twoAddressesException = Assert.Throws<ArgumentException>(() => IPNetwork.GetContainingNetwork(Address("10.0.0.1"), Address("::1")));

        Assert.That(twoNetworksException?.ParamName, Is.EqualTo("second"));
        Assert.That(networkSetException?.ParamName, Is.EqualTo("networks"));
        Assert.That(twoAddressesException?.ParamName, Is.EqualTo("secondAddress"));
    }
}
