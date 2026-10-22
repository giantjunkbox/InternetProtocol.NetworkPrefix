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

using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.Json;
using System.Xml.Serialization;
using Bitvantage.InternetProtocol;

namespace Test;

public class NetworkPrefixTests
{
    public static readonly ReadOnlyCollection<NetworkPrefixTestRecord> TestAddresses = new List<NetworkPrefixTestRecord>
    {
        new("10.0.0.0", 16, "10.0.0.0", "255.255.0.0", "10.0.255.255", "0.0.255.255", "10.0.0.1", "10.0.255.254"),
        new("10.0.2.3", 16, "10.0.0.0", "255.255.0.0", "10.0.255.255", "0.0.255.255", "10.0.0.1", "10.0.255.254"),
        new("10.0.2.0", 30, "10.0.2.0", "255.255.255.252", "10.0.2.3", "0.0.0.3", "10.0.2.1", "10.0.2.2"),
        new("10.0.2.3", 30, "10.0.2.0", "255.255.255.252", "10.0.2.3", "0.0.0.3", "10.0.2.1", "10.0.2.2"),
        new("10.0.2.3", 31, "10.0.2.2", "255.255.255.254", "10.0.2.3", "0.0.0.1", "10.0.2.2", "10.0.2.3"),
        new("10.0.2.3", 32, "10.0.2.3", "255.255.255.255", "10.0.2.3", "0.0.0.0", "10.0.2.3", "10.0.2.3"),
        new("10.0.2.3", 0, "0.0.0.0", "0.0.0.0", "255.255.255.255", "255.255.255.255", "0.0.0.1", "255.255.255.254")
    }.AsReadOnly();

    [Test]
    public void Addition_IPv4_01()
    {
        Assert.That(NetworkPrefix.Parse("10.20.30.0/24") + 5, Is.EqualTo(NetworkPrefix.Parse("10.20.35.0/24")));
    }

    [Test]
    public void Addition_IPv4_02()
    {
        Assert.That(NetworkPrefix.Parse("255.255.255.0/28") + 12, Is.EqualTo(NetworkPrefix.Parse("255.255.255.192/28")));
    }

    [Test]
    public void Addition_IPv4_03()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (NetworkPrefix.Parse("255.255.255.0/28") + 16).ToString());
    }

    [Test]
    public void Addition_IPv4_04()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (NetworkPrefix.Parse("255.255.255.0/28") + UInt128.MaxValue).ToString());
    }

    [Test]
    public void Addition_IPv6_01()
    {
        // ::/0 spans every IPv6 address, so it can only be moved by zero networks
        var everyAddress = NetworkPrefix.Parse("::/0");

        Assert.That(everyAddress + 0, Is.EqualTo(everyAddress));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = everyAddress + 1);

        // moving past the last /64 runs off the end of the address space
        Assert.That(NetworkPrefix.Parse("ffff:ffff:ffff:fffe::/64") + 1, Is.EqualTo(NetworkPrefix.Parse("ffff:ffff:ffff:ffff::/64")));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = NetworkPrefix.Parse("ffff:ffff:ffff:ffff::/64") + 1);
    }

    [Test]
    public void Addresses_IPv4_01()
    {
        var addresses =
            NetworkPrefix
                .Parse("10.0.0.0/31")
                .Addresses()
                .ToList();

        var expectedResult = new List<IPAddress>
        {
            IPAddress.Parse("10.0.0.0"),
            IPAddress.Parse("10.0.0.1")
        };

        Assert.That(addresses, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Addresses_IPv4_02()
    {
        var addresses =
            NetworkPrefix
                .Parse("10.0.0.0/32")
                .Addresses()
                .ToList();

        var expectedResult = new List<IPAddress>
        {
            IPAddress.Parse("10.0.0.0")
        };

        Assert.That(addresses, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Addresses_IPv4_03()
    {
        var addresses =
            NetworkPrefix
                .Parse("10.0.0.0/30")
                .Addresses()
                .ToList();

        var expectedResult = new List<IPAddress>
        {
            IPAddress.Parse("10.0.0.0"),
            IPAddress.Parse("10.0.0.1"),
            IPAddress.Parse("10.0.0.2"),
            IPAddress.Parse("10.0.0.3")
        };

        Assert.That(addresses, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Addresses_IPv4_04()
    {
        var addresses =
            NetworkPrefix
                .Parse("10.0.0.0/28")
                .Addresses()
                .ToList();

        var expectedResult = Enumerable
            .Range(0, 16)
            .Select(lastOctet => IPAddress.Parse($"10.0.0.{lastOctet}"))
            .ToList();

        Assert.That(addresses, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Addresses_IPv6_01()
    {
        // the last address of the IPv6 space is UInt128.MaxValue; enumeration must end there instead of wrapping around to ::
        var lastAddress = IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");

        Assert.That(new NetworkPrefix(lastAddress).Addresses().ToList(), Is.EqualTo(new[] { lastAddress }));
        Assert.That(NetworkPrefix.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe/127").Addresses().Count(), Is.EqualTo(2));
    }

    [Test]
    public void Allocation_IPv4_01()
    {
        Assert.That(NetworkPrefix.Parse("10.20.30.0/24").Allocation, Is.EqualTo(AddressAllocation.Private));
    }

    [Test]
    public void Allocation_IPv4_02()
    {
        Assert.That(NetworkPrefix.Parse("14.20.30.0/24").Allocation, Is.EqualTo(AddressAllocation.Global));
    }

    [Test]
    public void Allocation_IPv4_03()
    {
        Assert.That(NetworkPrefix.Parse("100.64.0.0/10").Allocation, Is.EqualTo(AddressAllocation.SharedAddressSpace));
        Assert.That(NetworkPrefix.Parse("192.0.2.0/24").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(NetworkPrefix.Parse("203.0.113.7").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(NetworkPrefix.Parse("198.18.0.0/15").Allocation, Is.EqualTo(AddressAllocation.Benchmarking));
        Assert.That(NetworkPrefix.Parse("169.254.1.0/24").Allocation, Is.EqualTo(AddressAllocation.LinkLocal));
        Assert.That(NetworkPrefix.Parse("127.0.0.1").Allocation, Is.EqualTo(AddressAllocation.Loopback));
        Assert.That(NetworkPrefix.Parse("0.0.0.0/32").Allocation, Is.EqualTo(AddressAllocation.Unspecified));
        Assert.That(NetworkPrefix.Parse("0.1.2.3").Allocation, Is.EqualTo(AddressAllocation.CurrentNetwork));
        Assert.That(NetworkPrefix.Parse("239.1.2.3").Allocation, Is.EqualTo(AddressAllocation.Multicast));
        Assert.That(NetworkPrefix.Parse("250.0.0.0/8").Allocation, Is.EqualTo(AddressAllocation.Experimental));
        Assert.That(NetworkPrefix.Parse("255.255.255.255").Allocation, Is.EqualTo(AddressAllocation.Broadcast));

        // a prefix that spans blocks with different purposes has no single allocation
        Assert.That(NetworkPrefix.Parse("0.0.0.0/1").Allocation, Is.EqualTo(AddressAllocation.Undefined));
        Assert.That(NetworkPrefix.Parse("192.0.0.0/8").Allocation, Is.EqualTo(AddressAllocation.Undefined));
    }

    [Test]
    public void Allocation_IPv6_01()
    {
        Assert.That(NetworkPrefix.Parse("2606:4700::/32").Allocation, Is.EqualTo(AddressAllocation.Global));
        Assert.That(NetworkPrefix.Parse("fd12:3456::/48").Allocation, Is.EqualTo(AddressAllocation.Private));
        Assert.That(NetworkPrefix.Parse("fe80::/64").Allocation, Is.EqualTo(AddressAllocation.LinkLocal));
        Assert.That(NetworkPrefix.Parse("::1").Allocation, Is.EqualTo(AddressAllocation.Loopback));
        Assert.That(NetworkPrefix.Parse("::").Allocation, Is.EqualTo(AddressAllocation.Unspecified));
        Assert.That(NetworkPrefix.Parse("ff02::1").Allocation, Is.EqualTo(AddressAllocation.Multicast));
        Assert.That(NetworkPrefix.Parse("2001:db8:1::/48").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(NetworkPrefix.Parse("3fff:100::/24").Allocation, Is.EqualTo(AddressAllocation.Documentation));
        Assert.That(NetworkPrefix.Parse("2001:2::/48").Allocation, Is.EqualTo(AddressAllocation.Benchmarking));
        Assert.That(NetworkPrefix.Parse("::/0").Allocation, Is.EqualTo(AddressAllocation.Undefined));
    }

    [Test]
    public void Class_IPv4_01()
    {
        Assert.That(NetworkPrefix.Parse("10.20.30.0/24").Class, Is.EqualTo(NetworkClass.A));
    }

    [Test]
    public void Class_IPv4_02()
    {
        Assert.That(NetworkPrefix.Parse("192.168.100.0/24").Class, Is.EqualTo(NetworkClass.C));
    }

    [Test]
    public void Class_IPv4_03()
    {
        Assert.That(NetworkPrefix.Parse("255.255.255.255/32").Class, Is.EqualTo(NetworkClass.E));
    }

    [Test]
    public void Class_IPv4_04()
    {
        // a prefix that spans more than one class has no class
        Assert.That(NetworkPrefix.Parse("0.0.0.0/0").Class, Is.EqualTo(NetworkClass.Undefined));
    }

    [Test]
    public void Class_IPv6_01()
    {
        // classes are an IPv4 concept
        Assert.That(NetworkPrefix.Parse("2001:db8::/32").Class, Is.EqualTo(NetworkClass.Undefined));
    }

    [Test]
    public void CompareTo_IPv4_01()
    {
        var lowerNetwork = NetworkPrefix.Parse("10.0.0.0/16");
        var higherNetwork = NetworkPrefix.Parse("10.1.0.0/16");

        Assert.That(lowerNetwork.CompareTo(higherNetwork), Is.Negative);
    }

    [Test]
    public void CompareTo_IPv4_02()
    {
        var lowerNetwork = NetworkPrefix.Parse("10.0.0.0/16");
        var higherNetwork = NetworkPrefix.Parse("10.1.0.0/16");

        Assert.That(higherNetwork.CompareTo(lowerNetwork), Is.Positive);
    }

    [Test]
    public void CompareTo_IPv4_03()
    {
        var firstNetwork = NetworkPrefix.Parse("10.1.0.0/16");
        var secondNetwork = NetworkPrefix.Parse("10.1.0.0/16");

        Assert.That(firstNetwork.CompareTo(secondNetwork), Is.Zero);
    }

    [Test]
    public void CompareTo_IPv4_04()
    {
        // with the same network address, the bigger network sorts first
        var biggerNetwork = NetworkPrefix.Parse("10.1.0.0/16");
        var smallerNetwork = NetworkPrefix.Parse("10.1.0.0/17");

        Assert.That(biggerNetwork.CompareTo(smallerNetwork), Is.Negative);
    }

    [Test]
    public void CompareTo_IPv4_05()
    {
        var networks = new List<NetworkPrefix?>
            {
                null,
                NetworkPrefix.Parse("10.0.128.0/17"),
                NetworkPrefix.Parse("10.0.64.0/18"),
                NetworkPrefix.Parse("10.0.32.0/19"),
                NetworkPrefix.Parse("10.0.16.0/20"),
                NetworkPrefix.Parse("10.0.0.0/21"),
                NetworkPrefix.Parse("10.0.12.0/22"),
                NetworkPrefix.Parse("10.0.8.0/23"),
                NetworkPrefix.Parse("10.0.11.0/24")
            }.OrderBy(network => network)
            .ToArray();

        var expectedResult = new List<NetworkPrefix?>
        {
            null,
            NetworkPrefix.Parse("10.0.0.0/21"),
            NetworkPrefix.Parse("10.0.8.0/23"),
            NetworkPrefix.Parse("10.0.11.0/24"),
            NetworkPrefix.Parse("10.0.12.0/22"),
            NetworkPrefix.Parse("10.0.16.0/20"),
            NetworkPrefix.Parse("10.0.32.0/19"),
            NetworkPrefix.Parse("10.0.64.0/18"),
            NetworkPrefix.Parse("10.0.128.0/17")
        };

        Assert.That(networks, Is.EqualTo(expectedResult));
    }

    [Test]
    public void CompareTo_IPv4_06()
    {
        Assert.That(NetworkPrefix.Parse("10.20.30.0/24").CompareTo(null), Is.GreaterThan(0));
    }

    [Test]
    public void CompareTo_IPv4_07()
    {
        // IPv4 sorts before IPv6, and the non-generic IComparable sorts the same way
        var ipv4Network = NetworkPrefix.Parse("255.0.0.0/8");
        var ipv6Network = NetworkPrefix.Parse("::/0");

        Assert.That(ipv4Network.CompareTo(ipv6Network), Is.Negative);
        Assert.That(((IComparable)ipv6Network).CompareTo(ipv4Network), Is.Positive);
        Assert.That(((IComparable)ipv4Network).CompareTo(null), Is.Positive);
        Assert.Throws<ArgumentException>(() => ((IComparable)ipv4Network).CompareTo("255.0.0.0/8"));
    }

    [Test]
    public void ComparisonOperators_IPv4_01()
    {
        var lowerNetwork = NetworkPrefix.Parse("10.0.0.0/16");
        var higherNetwork = NetworkPrefix.Parse("10.1.0.0/16");
        var lowerNetworkCopy = NetworkPrefix.Parse("10.0.0.0/16");
        NetworkPrefix? missingNetwork = null;
        NetworkPrefix? otherMissingNetwork = null;

        Assert.That(lowerNetwork < higherNetwork, Is.True);
        Assert.That(lowerNetwork <= lowerNetworkCopy, Is.True);
        Assert.That(higherNetwork > lowerNetwork, Is.True);
        Assert.That(higherNetwork >= lowerNetwork, Is.True);
        Assert.That(lowerNetwork >= lowerNetworkCopy, Is.True);

        // null sorts before every prefix, and comparing with null does not throw
        Assert.That(missingNetwork < lowerNetwork, Is.True);
        Assert.That(lowerNetwork > missingNetwork, Is.True);
        Assert.That(missingNetwork <= otherMissingNetwork, Is.True);
    }

    [Test]
    public void ComplementaryPrefix_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.0.10.0/24");

        Assert.That(network.ComplementaryPrefix, Is.EqualTo(NetworkPrefix.Parse("10.0.11.0/24")));
    }

    [Test]
    public void ComplementaryPrefix_IPv4_02()
    {
        var network = NetworkPrefix.Parse("10.0.11.0/24");

        Assert.That(network.ComplementaryPrefix, Is.EqualTo(NetworkPrefix.Parse("10.0.10.0/24")));
    }

    [Test]
    public void ComplementaryPrefix_IPv4_03()
    {
        Assert.Throws<InvalidOperationException>(() => _ = NetworkPrefix.Parse("0.0.0.0/0").ComplementaryPrefix);
    }

    [Test]
    public void ComplementaryPrefix_IPv4_04()
    {
        var network = NetworkPrefix.Parse("10.0.0.0/16");

        Assert.That(network.ComplementaryPrefix, Is.EqualTo(NetworkPrefix.Parse("10.1.0.0/16")));
    }

    [Test]
    public void ComplementaryPrefix_IPv4_05()
    {
        var network = NetworkPrefix.Parse("10.1.0.0/16");

        Assert.That(network.ComplementaryPrefix, Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/16")));
    }

    [Test]
    public void Constructor_01()
    {
        var network = NetworkPrefix.Parse("10.1.2.3/8");

        Assert.That(network.NetworkAddress, Is.EqualTo(IPAddress.Parse("10.0.0.0")));
    }

    [Test]
    public void Constructor_02()
    {
        foreach (var testAddress in TestAddresses)
        {
            var network = new NetworkPrefix(testAddress.Address, testAddress.PrefixLength);

            AssertMatches(network, testAddress);
        }
    }

    [Test]
    public void Constructor_03()
    {
        var address = IPAddress.Parse("10.0.0.0");

        // a negative prefix length used to fail with an IndexOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new NetworkPrefix(address, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new NetworkPrefix(address, 33));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new NetworkPrefix(IPAddress.IPv6Any, 129));
        Assert.Throws<ArgumentNullException>(() => _ = new NetworkPrefix(null!, 8));
        Assert.Throws<ArgumentNullException>(() => _ = new NetworkPrefix(null!));
    }

    [Test]
    public void Constructor_04()
    {
        var address = IPAddress.Parse("10.1.2.3");

        Assert.That(new NetworkPrefix(address, IPAddress.Parse("255.255.0.0")), Is.EqualTo(NetworkPrefix.Parse("10.1.0.0/16")));

        // a mask with a gap, or a mask of the other IP version, is not a valid mask for the address
        Assert.Throws<ArgumentException>(() => _ = new NetworkPrefix(address, IPAddress.Parse("255.0.255.0")));
        Assert.Throws<ArgumentException>(() => _ = new NetworkPrefix(address, IPAddress.Parse("ffff::")));
        Assert.Throws<ArgumentNullException>(() => _ = new NetworkPrefix(address, (IPAddress)null!));
    }

    [Test]
    public void Constructor_05()
    {
        // a host prefix holds one address
        Assert.That(new NetworkPrefix(IPAddress.Parse("10.1.2.3")), Is.EqualTo(NetworkPrefix.Parse("10.1.2.3/32")));
        Assert.That(new NetworkPrefix(IPAddress.Parse("2001:db8::1")), Is.EqualTo(NetworkPrefix.Parse("2001:db8::1/128")));

        // an IPv6 scope identifier is not part of a prefix
        Assert.That(NetworkPrefix.Parse("fe80::1%3/64").NetworkAddress.ScopeId, Is.Zero);
    }

    [Test]
    public void ContainedByOrEqual_IPv4_01()
    {
        var largerNetwork = NetworkPrefix.Parse("131.192.0.0/10");

        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("192.0.0.0/2")), Is.False);
        Assert.That(largerNetwork.ContainedByOrEqual(NetworkPrefix.Parse("192.0.0.0/2")), Is.False);
    }

    [Test]
    public void ContainedBy_IPv4_01()
    {
        var smallerNetwork = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That(smallerNetwork.ContainedBy(NetworkPrefix.Parse("10.20.0.0/16")), Is.True);
        Assert.That(smallerNetwork.ContainedBy(NetworkPrefix.Parse("10.20.30.0/24")), Is.False);
        Assert.That(smallerNetwork.ContainedBy(NetworkPrefix.Parse("10.20.30.0/25")), Is.False);
        Assert.That(smallerNetwork.ContainedBy(NetworkPrefix.Parse("10.21.0.0/16")), Is.False);

        Assert.That(smallerNetwork.ContainedByOrEqual(NetworkPrefix.Parse("10.20.0.0/16")), Is.True);
        Assert.That(smallerNetwork.ContainedByOrEqual(NetworkPrefix.Parse("10.20.30.0/24")), Is.True);
        Assert.That(smallerNetwork.ContainedByOrEqual(NetworkPrefix.Parse("10.20.30.0/25")), Is.False);
        Assert.That(smallerNetwork.ContainedByOrEqual(NetworkPrefix.Parse("10.21.0.0/16")), Is.False);
    }

    [Test]
    public void ContainedBy_IPv4_02()
    {
        // the contained-by pair is the inverse of the contains pair
        var largerNetwork = NetworkPrefix.Parse("10.20.0.0/16");
        var smallerNetwork = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That(smallerNetwork.ContainedBy(largerNetwork), Is.EqualTo(largerNetwork.Contains(smallerNetwork)));
        Assert.That(largerNetwork.ContainedBy(smallerNetwork), Is.EqualTo(smallerNetwork.Contains(largerNetwork)));
        Assert.That(smallerNetwork.ContainedByOrEqual(largerNetwork), Is.EqualTo(largerNetwork.ContainsOrEqual(smallerNetwork)));
        Assert.That(largerNetwork.ContainedByOrEqual(smallerNetwork), Is.EqualTo(smallerNetwork.ContainsOrEqual(largerNetwork)));
    }

    [Test]
    public void Contains_IPv4_01()
    {
        var largerNetwork = NetworkPrefix.Parse("0.0.0.0/1");

        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("116.11.0.0/16")), Is.True);
    }

    [Test]
    public void Contains_IPv4_02()
    {
        var largerNetwork = NetworkPrefix.Parse("10.20.0.0/16");

        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("10.20.0.0/16")), Is.False);
        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("10.20.0.0/24")), Is.True);
        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("10.20.255.0/24")), Is.True);
        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("10.20.30.40/32")), Is.True);
        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("10.21.0.0/24")), Is.False);
        Assert.That(largerNetwork.Contains(NetworkPrefix.Parse("10.20.0.0/15")), Is.False);
    }

    [Test]
    public void Contains_IPv4_03()
    {
        // a host prefix contains the single address that it represents
        var hostPrefix = NetworkPrefix.Parse("10.20.30.40/32");

        Assert.That(hostPrefix.Contains(IPAddress.Parse("10.20.30.40")), Is.True);
        Assert.That(hostPrefix.Contains(IPAddress.Parse("10.20.30.41")), Is.False);

        // every address within a network is contained by that network
        var networkPrefix = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That(networkPrefix.Contains(IPAddress.Parse("10.20.30.0")), Is.True);
        Assert.That(networkPrefix.Contains(IPAddress.Parse("10.20.30.40")), Is.True);
        Assert.That(networkPrefix.Contains(IPAddress.Parse("10.20.30.255")), Is.True);
        Assert.That(networkPrefix.Contains(IPAddress.Parse("10.20.31.40")), Is.False);
    }

    [Test]
    public void Contains_IPv4_04()
    {
        // an address or a prefix of the other IP version is never contained
        var everyIPv4Address = NetworkPrefix.Parse("0.0.0.0/0");

        Assert.That(everyIPv4Address.Contains(IPAddress.IPv6Any), Is.False);
        Assert.That(everyIPv4Address.Contains(NetworkPrefix.Parse("::/128")), Is.False);
        Assert.That(everyIPv4Address.ContainsOrEqual(NetworkPrefix.Parse("::/0")), Is.False);
        Assert.Throws<ArgumentNullException>(() => everyIPv4Address.Contains((IPAddress)null!));
        Assert.Throws<ArgumentNullException>(() => everyIPv4Address.Contains((NetworkPrefix)null!));
    }

    [Test]
    public void Contains_IPv6_01()
    {
        // a host prefix contains the single address that it represents
        var hostPrefix = NetworkPrefix.Parse("2001:db8::1/128");

        Assert.That(hostPrefix.Contains(IPAddress.Parse("2001:db8::1")), Is.True);
        Assert.That(hostPrefix.Contains(IPAddress.Parse("2001:db8::2")), Is.False);

        // every address within a network is contained by that network
        var networkPrefix = NetworkPrefix.Parse("2001:db8::/32");

        Assert.That(networkPrefix.Contains(IPAddress.Parse("2001:db8::1")), Is.True);
        Assert.That(networkPrefix.Contains(IPAddress.Parse("2001:db9::1")), Is.False);
    }

    [Test]
    public void ContainsOrEqual_IPv4_03()
    {
        var largerNetwork = NetworkPrefix.Parse("10.20.0.0/16");

        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("10.20.0.0/16")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("10.20.0.0/24")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("10.20.255.0/24")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("10.20.30.40/32")), Is.True);
        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("10.21.0.0/24")), Is.False);
        Assert.That(largerNetwork.ContainsOrEqual(NetworkPrefix.Parse("10.20.0.0/15")), Is.False);
    }

    [Test]
    public void Equals_IPv4_01()
    {
        var firstNetwork = NetworkPrefix.Parse("10.20.30.0/24");
        var secondNetwork = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That(firstNetwork, Is.EqualTo(secondNetwork));
        Assert.That(firstNetwork == secondNetwork, Is.True);
        Assert.That(firstNetwork != secondNetwork, Is.False);
    }

    [Test]
    public void Equals_IPv4_02()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That(network.Equals(network), Is.True);
        Assert.That(network.Equals((object)"10.20.30.0/24"), Is.False);
        Assert.That(network.Equals(null), Is.False);
    }

    [Test]
    public void Equals_IPv4_03()
    {
        Assert.That(NetworkPrefix.Parse("10.20.30.0/24"), Is.Not.EqualTo(NetworkPrefix.Parse("10.20.36.0/24")));
    }

    [Test]
    public void Equals_IPv4_04()
    {
        var network = NetworkPrefix.Parse("10.0.0.0/16");
        var sameNetworkWithHostBits = NetworkPrefix.Parse("10.0.1.2/16");

        Assert.That(network, Is.EqualTo(sameNetworkWithHostBits));
    }

    [Test]
    public void Equals_IPv4_05()
    {
        // the same address bits with a different prefix length, or a different IP version, are different prefixes
        Assert.That(NetworkPrefix.Parse("10.0.0.0/16"), Is.Not.EqualTo(NetworkPrefix.Parse("10.0.0.0/17")));
        Assert.That(NetworkPrefix.Parse("0.0.0.0/0"), Is.Not.EqualTo(NetworkPrefix.Parse("::/0")));
    }

    [Test]
    public void Equals_IPNetwork_01()
    {
        // a prefix and an IPNetwork can be compared directly in either order
        var network = NetworkPrefix.Parse("10.0.0.0/16");
        var sameIPNetwork = IPNetwork.Parse("10.0.0.0/16");
        var otherIPNetwork = IPNetwork.Parse("10.0.0.0/17");

        Assert.That(network == sameIPNetwork, Is.True);
        Assert.That(sameIPNetwork == network, Is.True);
        Assert.That(network != otherIPNetwork, Is.True);
        Assert.That(otherIPNetwork != network, Is.True);
    }

    [Test]
    public void Exclude_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.0.0.0/16");

        var expectedResult = new List<NetworkPrefix>
        {
            NetworkPrefix.Parse("10.0.128.0/17"),
            NetworkPrefix.Parse("10.0.64.0/18"),
            NetworkPrefix.Parse("10.0.32.0/19"),
            NetworkPrefix.Parse("10.0.16.0/20"),
            NetworkPrefix.Parse("10.0.0.0/21"),
            NetworkPrefix.Parse("10.0.12.0/22"),
            NetworkPrefix.Parse("10.0.8.0/23"),
            NetworkPrefix.Parse("10.0.11.0/24")
        };

        Assert.That(network.Exclude(NetworkPrefix.Parse("10.0.10.0/24")), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Exclude_IPv4_02()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/29");

        var expectedResult = new List<NetworkPrefix>
        {
            NetworkPrefix.Parse("10.20.30.4/30"),
            NetworkPrefix.Parse("10.20.30.2/31"),
            NetworkPrefix.Parse("10.20.30.0/32")
        };

        Assert.That(network.Exclude(NetworkPrefix.Parse("10.20.30.1/32")), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Exclude_IPv4_03()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");

        // excluding the whole network, or a network that contains it, leaves nothing
        Assert.That(network.Exclude(NetworkPrefix.Parse("10.20.30.0/24")), Is.Empty);
        Assert.That(network.Exclude(NetworkPrefix.Parse("10.0.0.0/8")), Is.Empty);

        // excluding a network that does not overlap, including one of the other IP version, leaves the network unchanged
        Assert.That(network.Exclude(NetworkPrefix.Parse("10.20.31.0/24")), Is.EqualTo(new[] { network }));
        Assert.That(network.Exclude(NetworkPrefix.Parse("::/0")), Is.EqualTo(new[] { network }));
    }

    [Test]
    public void Exclude_IPv6_01()
    {
        var network = NetworkPrefix.Parse("2001:db8::/126");

        var expectedResult = new[]
        {
            NetworkPrefix.Parse("2001:db8::2/127"),
            NetworkPrefix.Parse("2001:db8::1/128")
        };

        Assert.That(network.Exclude(NetworkPrefix.Parse("2001:db8::/128")), Is.EqualTo(expectedResult));
    }

    [Test]
    public void FirstAndLastHost_IPv4_01()
    {
        // a /0 has a first and last host like any other network; this used to throw
        var everyAddress = NetworkPrefix.Parse("0.0.0.0/0");

        Assert.That(everyAddress.FirstHost, Is.EqualTo(IPAddress.Parse("0.0.0.1")));
        Assert.That(everyAddress.LastHost, Is.EqualTo(IPAddress.Parse("255.255.255.254")));
    }

    [Test]
    public void FirstAndLastHost_IPv6_01()
    {
        var network = NetworkPrefix.Parse("2001:db8::/64");

        Assert.That(network.FirstHost, Is.EqualTo(IPAddress.Parse("2001:db8::1")));
        Assert.That(network.LastHost, Is.EqualTo(IPAddress.Parse("2001:db8::ffff:ffff:ffff:fffe")));
        Assert.That(network.LastAddress, Is.EqualTo(IPAddress.Parse("2001:db8::ffff:ffff:ffff:ffff")));
    }

    [Test]
    public void Format_IPv4_01()
    {
        // the name of a NetworkFormat value can be used as a format string
        var network = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That($"{network}", Is.EqualTo("10.20.30.0/24"));
        Assert.That($"{network:G}", Is.EqualTo("10.20.30.0/24"));
        Assert.That($"{network:AddressAndMask}", Is.EqualTo("10.20.30.0 255.255.255.0"));
        Assert.That($"{network:addressandhostmask}", Is.EqualTo("10.20.30.0 0.0.0.255"));
        Assert.That($"{network:Address}", Is.EqualTo("10.20.30.0"));
        Assert.That($"{network:Mask}", Is.EqualTo("255.255.255.0"));
        Assert.Throws<FormatException>(() => _ = $"{network:X}");
    }

    [Test]
    public void GetContainingNetwork_IPv4_01()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("10.0.0.0/24", "10.0.1.0/24"), Is.EqualTo((NetworkPrefix)"10.0.0.0/23"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_02()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("200.0.0.0/24", "10.0.1.0/24"), Is.EqualTo((NetworkPrefix)"0.0.0.0/0"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_03()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("10.0.1.0/24", "10.0.1.0/24"), Is.EqualTo((NetworkPrefix)"10.0.1.0/24"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_04()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("10.0.1.0/24", "10.0.1.0/23"), Is.EqualTo((NetworkPrefix)"10.0.1.0/23"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_05()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("10.0.1.0/32", "0.0.0.0/0"), Is.EqualTo((NetworkPrefix)"0.0.0.0/0"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_06()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("0.0.0.0/1", "0.0.0.0/2"), Is.EqualTo((NetworkPrefix)"0.0.0.0/1"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_07()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("0.0.0.0/2", "255.255.255.0/24"), Is.EqualTo((NetworkPrefix)"0.0.0.0/0"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_08()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork(new NetworkPrefix[] { "1.0.0.0/8", "2.0.0.0/8", "3.0.0.0/8" }), Is.EqualTo((NetworkPrefix)"0.0.0.0/6"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_09()
    {
        var networks = new NetworkPrefix[]
        {
            "10.0.0.0/24",
            "10.0.1.0/24",
            "10.0.2.0/24",
            "10.0.3.0/24",
            "254.0.0.0/24"
        };

        var containingNetwork = NetworkPrefix.GetContainingNetwork(networks);

        Assert.That(containingNetwork, Is.EqualTo(NetworkPrefix.Parse("0.0.0.0/0")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_10()
    {
        var networks = new NetworkPrefix[]
        {
            "10.0.0.0/24",
            "10.0.1.0/24",
            "10.0.2.0/24",
            "10.0.3.0/24"
        };

        var containingNetwork = NetworkPrefix.GetContainingNetwork(networks);

        Assert.That(containingNetwork, Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/22")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_11()
    {
        var networks = new NetworkPrefix[]
        {
            "10.0.0.0/24",
            "10.0.1.0/24",
            "10.0.2.0/24",
            "10.0.3.0/24",
            "127.255.255.0/24"
        };

        var containingNetwork = NetworkPrefix.GetContainingNetwork(networks);

        Assert.That(containingNetwork, Is.EqualTo(NetworkPrefix.Parse("0.0.0.0/1")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_12()
    {
        // when one network contains the other, the containing network is the less specific of the two,
        // even when the two do not share the same network address
        Assert.That(NetworkPrefix.GetContainingNetwork("10.20.0.0/16", "10.20.30.0/24"), Is.EqualTo((NetworkPrefix)"10.20.0.0/16"));
        Assert.That(NetworkPrefix.GetContainingNetwork("10.20.30.0/24", "10.20.0.0/16"), Is.EqualTo((NetworkPrefix)"10.20.0.0/16"));
        Assert.That(NetworkPrefix.GetContainingNetwork("0.0.0.0/1", "10.20.30.0/24"), Is.EqualTo((NetworkPrefix)"0.0.0.0/1"));
        Assert.That(NetworkPrefix.GetContainingNetwork("10.20.30.0/24", "10.20.30.40/32"), Is.EqualTo((NetworkPrefix)"10.20.30.0/24"));
    }

    [Test]
    public void GetContainingNetwork_IPv4_13()
    {
        // the instance method gives the same answer as the static method
        var network = NetworkPrefix.Parse("10.0.0.0/24");

        Assert.That(network.GetContainingNetwork("10.0.3.0/24"), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/22")));
    }

    [Test]
    public void GetContainingNetwork_IPv4_14()
    {
        Assert.Throws<ArgumentException>(() => NetworkPrefix.GetContainingNetwork("10.0.0.0/8", "2001:db8::/32"));
        Assert.Throws<ArgumentException>(() => NetworkPrefix.GetContainingNetwork(Array.Empty<NetworkPrefix>()));
        Assert.Throws<ArgumentNullException>(() => NetworkPrefix.GetContainingNetwork(new NetworkPrefix[] { "10.0.0.0/8", null! }));
    }

    [Test]
    public void GetContainingNetwork_IPv6_01()
    {
        Assert.That(NetworkPrefix.GetContainingNetwork("2001:db8::/32", "2001:db8:1::/48"), Is.EqualTo((NetworkPrefix)"2001:db8::/32"));
        Assert.That(NetworkPrefix.GetContainingNetwork("2001:db8::/48", "2001:db8:1::/48"), Is.EqualTo((NetworkPrefix)"2001:db8::/47"));
    }

    [Test]
    public void GetContainingNetwork_IPv6_02()
    {
        // a /0 used to produce the IPv4 network 0.0.0.0/0 even for IPv6 input
        Assert.That(NetworkPrefix.GetContainingNetwork("::/0", "2001:db8::/32"), Is.EqualTo((NetworkPrefix)"::/0"));
        Assert.That(NetworkPrefix.GetContainingNetwork("::/1", "8000::/1"), Is.EqualTo((NetworkPrefix)"::/0"));
    }

    [Test]
    public void GetHashCode_IPv4_01()
    {
        // equal prefixes have equal hash codes, so they work as dictionary keys
        var networksByName = new Dictionary<NetworkPrefix, string>
        {
            [NetworkPrefix.Parse("10.0.0.0/8")] = "private",
            [NetworkPrefix.Parse("2001:db8::/32")] = "documentation"
        };

        Assert.That(NetworkPrefix.Parse("10.1.2.3/8").GetHashCode(), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/8").GetHashCode()));
        Assert.That(networksByName[NetworkPrefix.Parse("10.1.2.3/8")], Is.EqualTo("private"));
        Assert.That(networksByName[NetworkPrefix.Parse("2001:db8::/32")], Is.EqualTo("documentation"));
    }

    [Test]
    public void GetSupernet_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.0.1.0/24");

        Assert.That(network.GetSupernet(), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/23")));
        Assert.That(network.GetSupernet(16), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/16")));
        Assert.That(network.GetSupernet(0), Is.EqualTo(NetworkPrefix.Parse("0.0.0.0/0")));
    }

    [Test]
    public void GetSupernet_IPv4_02()
    {
        var network = NetworkPrefix.Parse("10.0.1.0/24");

        // a supernet must be shorter than the network, and a /0 has no supernet
        Assert.Throws<ArgumentOutOfRangeException>(() => network.GetSupernet(24));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.GetSupernet(25));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.GetSupernet(-1));
        Assert.Throws<InvalidOperationException>(() => NetworkPrefix.Parse("0.0.0.0/0").GetSupernet());
    }

    [Test]
    public void GreaterThan_IPv4_01()
    {
        var lowerNetwork = NetworkPrefix.Parse("10.0.0.0/16");
        var higherNetwork = NetworkPrefix.Parse("10.1.0.0/16");

        Assert.That(higherNetwork, Is.GreaterThan(lowerNetwork));
    }

    [Test]
    public void Hosts_IPv4_01()
    {
        var hosts = NetworkPrefix.Parse("10.0.0.0/30").Hosts().ToList();

        Assert.That(hosts, Is.EqualTo(new[] { IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.2") }));
    }

    [Test]
    public void Hosts_IPv6_01()
    {
        // the hosts of a point-to-point prefix at the very end of the address space must end rather than wrap around
        var hosts = NetworkPrefix.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe/127").Hosts().ToList();

        Assert.That(hosts, Is.EqualTo(new[] { IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe"), IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff") }));
    }

    [Test]
    public void ImplicitConversion_01()
    {
        NetworkPrefix actual = "192.168.13.0/24";
        var expected = new NetworkPrefix(new IPAddress(new byte[] { 192, 168, 13, 0 }), 24);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ImplicitConversion_02()
    {
        string actual = new NetworkPrefix(new IPAddress(new byte[] { 192, 168, 13, 0 }), 24);
        var expected = "192.168.13.0/24";

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ImplicitConversion_03()
    {
        // a prefix converts to and from IPNetwork in both directions without loss
        IPNetwork ipNetwork = NetworkPrefix.Parse("10.20.0.0/16");
        NetworkPrefix network = IPNetwork.Parse("2001:db8::/32");

        Assert.That(ipNetwork, Is.EqualTo(IPNetwork.Parse("10.20.0.0/16")));
        Assert.That(network, Is.EqualTo(NetworkPrefix.Parse("2001:db8::/32")));
        Assert.That(NetworkPrefix.FromIPNetwork(ipNetwork).ToIPNetwork(), Is.EqualTo(ipNetwork));
        Assert.That(new NetworkPrefix(ipNetwork), Is.EqualTo(NetworkPrefix.Parse("10.20.0.0/16")));
    }

    [Test]
    public void JsonConverter_01()
    {
        var network = NetworkPrefix.Parse("10.20.30.40");

        var actual = JsonSerializer.Serialize(network);

        var expected =
            """
            "10.20.30.40/32"
            """;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void JsonConverter_02()
    {
        var networks = new List<NetworkPrefix> { "10.20.30.40", "5.10.15.20" };

        var actual = JsonSerializer.Serialize(networks);

        var expected =
            """
            ["10.20.30.40/32","5.10.15.20/32"]
            """;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void JsonConverter_03()
    {
        var json =
            """
            "10.20.30.40/32"
            """;

        var actual = JsonSerializer.Deserialize<NetworkPrefix>(json);
        var expected = NetworkPrefix.Parse("10.20.30.40");

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void JsonConverter_04()
    {
        var json =
            """
            ["10.20.30.40/32","5.10.15.20/32"]
            """;

        var actual = JsonSerializer.Deserialize<List<NetworkPrefix>>(json);
        var expected = new List<NetworkPrefix> { "10.20.30.40", "5.10.15.20" };

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void JsonConverter_05()
    {
        // prefixes can be the keys of a dictionary
        var namesByNetwork = new Dictionary<NetworkPrefix, string>
        {
            ["10.0.0.0/8"] = "private",
            ["2001:db8::/32"] = "documentation"
        };

        var json = JsonSerializer.Serialize(namesByNetwork);
        var roundTripped = JsonSerializer.Deserialize<Dictionary<NetworkPrefix, string>>(json);

        Assert.That(json, Is.EqualTo("""{"10.0.0.0/8":"private","2001:db8::/32":"documentation"}"""));
        Assert.That(roundTripped, Is.EqualTo(namesByNetwork));
    }

    [Test]
    public void JsonConverter_06()
    {
        // a value that is not a string, or a string that is not a prefix, is a JSON error rather than a crash
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NetworkPrefix>("5"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NetworkPrefix>("\"10.0.0.0/33\""));
        Assert.That(JsonSerializer.Deserialize<NetworkPrefix?>("null"), Is.Null);
    }

    [Test]
    public void LessThan_IPv4_01()
    {
        var lowerNetwork = NetworkPrefix.Parse("10.0.0.0/16");
        var higherNetwork = NetworkPrefix.Parse("10.1.0.0/16");

        Assert.That(lowerNetwork, Is.LessThan(higherNetwork));
    }

    [Test]
    public void Overlaps_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.20.0.0/16");

        Assert.That(network.Overlaps(NetworkPrefix.Parse("10.20.0.0/16")), Is.True);
        Assert.That(network.Overlaps(NetworkPrefix.Parse("10.20.30.0/24")), Is.True);
        Assert.That(network.Overlaps(NetworkPrefix.Parse("10.0.0.0/8")), Is.True);
        Assert.That(network.Overlaps(NetworkPrefix.Parse("10.21.0.0/16")), Is.False);
        Assert.That(network.Overlaps(NetworkPrefix.Parse("::/0")), Is.False);
    }

    [Test]
    public void Parse_IPv4_01()
    {
        foreach (var testAddress in TestAddresses)
        {
            var network = NetworkPrefix.Parse($"{testAddress.Address}/{testAddress.PrefixLength}");

            AssertMatches(network, testAddress);
        }
    }

    [Test]
    [Category("Network")]
    public void Parse_IPv4_02()
    {
        // Parse no longer looks up host names, which is a network operation; Resolve does
        Assert.Throws<FormatException>(() => NetworkPrefix.Parse("one.one.one.one/32"));

        var network = NetworkPrefix.Resolve("one.one.one.one/32", AddressFamily.InterNetwork);
        Assert.That(network, Is.AnyOf(NetworkPrefix.Parse("1.1.1.1/32"), NetworkPrefix.Parse("1.0.0.1/32")));
    }

    [Test]
    [Category("Network")]
    public async Task Parse_IPv4_03()
    {
        // the IPv4 mask decides that an IPv4 address is looked up
        var network = await NetworkPrefix.ResolveAsync("one.one.one.one 255.255.255.255");
        Assert.That(network, Is.AnyOf(NetworkPrefix.Parse("1.1.1.1/32"), NetworkPrefix.Parse("1.0.0.1/32")));
    }

    [Test]
    public void Parse_IPv4_04()
    {
        foreach (var testAddress in TestAddresses)
        {
            var network = NetworkPrefix.Parse($"{testAddress.Address} {testAddress.NetworkMask}");

            AssertMatches(network, testAddress);
        }
    }

    [Test]
    public void Parse_IPv4_05()
    {
        // malformed text is rejected; TryParse returns false instead of throwing, and Parse throws a FormatException
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
            Assert.That(NetworkPrefix.TryParse(malformedValue, out var network), Is.False, malformedValue);
            Assert.That(network, Is.Null, malformedValue);
            Assert.Throws<FormatException>(() => NetworkPrefix.Parse(malformedValue), malformedValue);
        }

        Assert.That(NetworkPrefix.TryParse((string?)null, out _), Is.False);
        Assert.Throws<ArgumentNullException>(() => NetworkPrefix.Parse((string)null!));
    }

    [Test]
    public void Parse_IPv4_06()
    {
        // surrounding whitespace is ignored, and an address may be separated from its mask by a slash or any run of whitespace
        var expected = NetworkPrefix.Parse("10.0.0.0/24");

        Assert.That(NetworkPrefix.Parse(" 10.0.0.0/24 "), Is.EqualTo(expected));
        Assert.That(NetworkPrefix.Parse("10.0.0.0  255.255.255.0"), Is.EqualTo(expected));
        Assert.That(NetworkPrefix.Parse("10.0.0.0\t255.255.255.0"), Is.EqualTo(expected));
        Assert.That(NetworkPrefix.Parse("10.0.0.0/255.255.255.0"), Is.EqualTo(expected));
        Assert.That(NetworkPrefix.Parse("10.0.0.0/024"), Is.EqualTo(expected));
        Assert.That(NetworkPrefix.Parse("10.0.0.0/24".AsSpan()), Is.EqualTo(expected));
    }

    [Test]
    public void Parse_IPv6_01()
    {
        Assert.That(NetworkPrefix.Parse("2001:db8::1/64"), Is.EqualTo(new NetworkPrefix(IPAddress.Parse("2001:db8::"), 64)));
        Assert.That(NetworkPrefix.Parse("2001:db8:: ffff:ffff::"), Is.EqualTo(NetworkPrefix.Parse("2001:db8::/32")));
        Assert.That(NetworkPrefix.Parse("2001:db8::1"), Is.EqualTo(NetworkPrefix.Parse("2001:db8::1/128")));
        Assert.That(NetworkPrefix.TryParse("2001:db8::/129", out _), Is.False);
    }

    [Test]
    public void Parse_Generic_01()
    {
        // the prefix works with code that is generic over IParsable and ISpanParsable
        Assert.That(ParseWithInterface<NetworkPrefix>("10.0.0.0/8"), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/8")));
        Assert.That(ParseSpanWithInterface<NetworkPrefix>("10.0.0.0/8"), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/8")));
    }

    [Test]
    [Category("Network")]
    public void Resolve_01()
    {
        // text that is already a prefix is parsed without a lookup
        Assert.That(NetworkPrefix.Resolve("10.0.0.0/24"), Is.EqualTo(NetworkPrefix.Parse("10.0.0.0/24")));

        // an address with an invalid suffix is malformed, not a host name
        Assert.Throws<FormatException>(() => NetworkPrefix.Resolve("10.0.0.0/33"));
        Assert.Throws<FormatException>(() => NetworkPrefix.Resolve("one.one.one.one/"));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkPrefix.Resolve("one.one.one.one", AddressFamily.AppleTalk));
    }

    [Test]
    public void Split_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");
        var halves = network.Split();

        var expectedResult = new[]
        {
            NetworkPrefix.Parse("10.20.30.0/25"),
            NetworkPrefix.Parse("10.20.30.128/25")
        };

        Assert.That(halves, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Split_IPv4_02()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");
        var subnets = network.Split(26);

        var expectedResult = new[]
        {
            NetworkPrefix.Parse("10.20.30.0/26"),
            NetworkPrefix.Parse("10.20.30.64/26"),
            NetworkPrefix.Parse("10.20.30.128/26"),
            NetworkPrefix.Parse("10.20.30.192/26")
        };

        Assert.That(subnets, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Split_IPv4_03()
    {
        var hostPrefix = NetworkPrefix.Parse("10.20.30.40/32");

        // the exception is thrown by Split itself, not when the result is enumerated
        Assert.Throws<InvalidOperationException>(() => hostPrefix.Split());
    }

    [Test]
    public void Split_IPv4_04()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/30");

        var expectedResult = new[]
        {
            NetworkPrefix.Parse("10.20.30.0/32"),
            NetworkPrefix.Parse("10.20.30.1/32"),
            NetworkPrefix.Parse("10.20.30.2/32"),
            NetworkPrefix.Parse("10.20.30.3/32")
        };

        Assert.That(network.Split(32), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Split_IPv4_05()
    {
        // a point to point network splits into the two host prefixes that make it up
        var network = NetworkPrefix.Parse("10.20.30.0/31");

        var expectedResult = new[]
        {
            NetworkPrefix.Parse("10.20.30.0/32"),
            NetworkPrefix.Parse("10.20.30.1/32")
        };

        Assert.That(network.Split(), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Split_IPv4_06()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");

        // a target that is not more specific than the network is not a split; the exception is thrown by Split itself
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(24));

        // a target more specific than a host prefix does not exist
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Split(33));
    }

    [Test]
    public void Split_IPv6_01()
    {
        var network = NetworkPrefix.Parse("2001:db8::/127");

        var expectedResult = new[]
        {
            NetworkPrefix.Parse("2001:db8::/128"),
            NetworkPrefix.Parse("2001:db8::1/128")
        };

        Assert.That(network.Split(), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Split_IPv6_02()
    {
        var everyAddress = NetworkPrefix.Parse("::/0");

        Assert.That(everyAddress.Split(), Is.EqualTo(new[] { NetworkPrefix.Parse("::/1"), NetworkPrefix.Parse("8000::/1") }));

        // splitting ::/0 into /128s produces 2^128 subnets, one more than a UInt128 can count; this used to produce only one
        var firstHostPrefixes = everyAddress.Split(128).Take(3).ToList();
        Assert.That(firstHostPrefixes, Is.EqualTo(new[] { NetworkPrefix.Parse("::/128"), NetworkPrefix.Parse("::1/128"), NetworkPrefix.Parse("::2/128") }));
    }

    [Test]
    public void Subtraction_IPv4_01()
    {
        Assert.That(NetworkPrefix.Parse("10.20.30.0/24") - 15, Is.EqualTo(NetworkPrefix.Parse("10.20.15.0/24")));
    }

    [Test]
    public void Subtraction_IPv4_02()
    {
        Assert.That(NetworkPrefix.Parse("128.0.0.0/1") - 1, Is.EqualTo(NetworkPrefix.Parse("0.0.0.0/1")));
    }

    [Test]
    public void Subtraction_IPv4_03()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (NetworkPrefix.Parse("0.0.0.0/24") - 1).ToString());
    }

    [Test]
    public void Subtraction_IPv4_04()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (NetworkPrefix.Parse("0.0.0.0/24") - UInt128.MaxValue).ToString());
    }

    [Test]
    public void Summarize_IPv4_01()
    {
        var networks = new List<NetworkPrefix>
        {
            NetworkPrefix.Parse("10.0.8.0/23"),
            NetworkPrefix.Parse("10.0.10.0/24"),
            NetworkPrefix.Parse("10.0.11.0/24"),
            NetworkPrefix.Parse("10.0.12.0/22"),

            NetworkPrefix.Parse("10.0.128.0/18"),
            NetworkPrefix.Parse("10.0.192.0/18"),

            NetworkPrefix.Parse("100.0.0.100/32"),
            NetworkPrefix.Parse("100.0.0.101/32"),

            NetworkPrefix.Parse("100.0.0.102/32")
        };

        var expectedResult = new List<NetworkPrefix>
        {
            NetworkPrefix.Parse("10.0.8.0/21"),
            NetworkPrefix.Parse("10.0.128.0/17"),
            NetworkPrefix.Parse("100.0.0.100/31"),
            NetworkPrefix.Parse("100.0.0.102/32")
        };

        Assert.That(NetworkPrefix.Summarize(networks), Is.EqualTo(expectedResult));
    }

    [Test]
    public void Summarize_IPv4_02()
    {
        // a network that covers every other network is the whole summary; overlapping networks used to be kept
        var networks = new NetworkPrefix[] { "0.0.0.0/0", "10.0.8.0/23", "10.0.128.0/18", "100.0.0.100/32" };

        Assert.That(NetworkPrefix.Summarize(networks), Is.EqualTo(new[] { NetworkPrefix.Parse("0.0.0.0/0") }));
    }

    [Test]
    public void Summarize_IPv4_03()
    {
        // duplicates, and a network listed together with its own halves, used to throw an ArgumentException
        Assert.That(NetworkPrefix.Summarize(new NetworkPrefix[] { "10.0.0.0/24", "10.0.0.0/24" }), Is.EqualTo(new[] { NetworkPrefix.Parse("10.0.0.0/24") }));
        Assert.That(NetworkPrefix.Summarize(new NetworkPrefix[] { "10.0.0.0/24", "10.0.0.0/25", "10.0.0.128/25" }), Is.EqualTo(new[] { NetworkPrefix.Parse("10.0.0.0/24") }));
    }

    [Test]
    public void Summarize_IPv4_04()
    {
        // merging cascades: two /26s merge into a /25, which then merges with the other /25
        var networks = new NetworkPrefix[] { "10.0.0.192/26", "10.0.0.0/25", "10.0.0.128/26" };

        Assert.That(NetworkPrefix.Summarize(networks), Is.EqualTo(new[] { NetworkPrefix.Parse("10.0.0.0/24") }));
    }

    [Test]
    public void Summarize_IPv4_05()
    {
        // two networks of the same size that are next to each other but not halves of the same supernet are not merged
        var networks = new NetworkPrefix[] { "10.0.1.0/24", "10.0.2.0/24" };

        Assert.That(NetworkPrefix.Summarize(networks), Is.EqualTo(networks));
        Assert.That(NetworkPrefix.Summarize(Array.Empty<NetworkPrefix>()), Is.Empty);
        Assert.Throws<ArgumentNullException>(() => NetworkPrefix.Summarize(new NetworkPrefix[] { "10.0.0.0/8", null! }));
    }

    [Test]
    public void Summarize_IPv6_01()
    {
        // IPv4 and IPv6 are summarized separately, and the result is sorted with IPv4 first
        var networks = new NetworkPrefix[] { "2001:db8:1::/48", "10.0.0.0/9", "2001:db8::/48", "10.128.0.0/9" };

        var expectedResult = new[] { NetworkPrefix.Parse("10.0.0.0/8"), NetworkPrefix.Parse("2001:db8::/47") };

        Assert.That(NetworkPrefix.Summarize(networks), Is.EqualTo(expectedResult));
    }

    [Test]
    public void ToString_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");

        Assert.That(network.ToString(), Is.EqualTo("10.20.30.0/24"));
        Assert.That(network.ToString(NetworkFormat.AddressAndPrefix), Is.EqualTo("10.20.30.0/24"));
        Assert.That(network.ToString(NetworkFormat.AddressAndMask), Is.EqualTo("10.20.30.0 255.255.255.0"));
        Assert.That(network.ToString(NetworkFormat.AddressAndHostMask), Is.EqualTo("10.20.30.0 0.0.0.255"));
        Assert.That(network.ToString(NetworkFormat.Address), Is.EqualTo("10.20.30.0"));
        Assert.That(network.ToString(NetworkFormat.Mask), Is.EqualTo("255.255.255.0"));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.ToString((NetworkFormat)99));
    }

    [Test]
    public void ToString_IPv4_02()
    {
        var network = NetworkPrefix.Parse("10.20.30.0/24");

        var actual = network.ToString(NetworkFormat.Detail);
        var expected =
            """
            Address:    10.20.30.0     00001010.00010100.00011110.00000000
            Mask:       255.255.255.0  11111111.11111111.11111111.00000000
            Wildcard:   0.0.0.255      00000000.00000000.00000000.11111111
            Broadcast:  10.20.30.255   00001010.00010100.00011110.11111111
            First Host: 10.20.30.1     00001010.00010100.00011110.00000001
            Last Host:  10.20.30.254   00001010.00010100.00011110.11111110
            Hosts/Net:  254

            """;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_IPv4_03()
    {
        // the host count of a host prefix used to be written as "01"
        var network = NetworkPrefix.Parse("10.20.30.40/32");

        var hostCountLine = network
            .ToString(NetworkFormat.Detail)
            .Split(Environment.NewLine)
            .Single(line => line.StartsWith("Hosts/Net:"));

        Assert.That(hostCountLine, Is.EqualTo("Hosts/Net:  1"));
    }

    [Test]
    public void ToString_IPv6_01()
    {
        var network = NetworkPrefix.Parse("2001:0db8:0001:0000:0000:0ab9:C0A8:0102/45");

        var actual = network.ToString(NetworkFormat.Detail);
        var expected =
            """
            Address:    2001:db8::                           0010000000000001:0000110110111000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000
            Mask:       ffff:ffff:fff8::                     1111111111111111:1111111111111111:1111111111111000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000
            Wildcard:   ::7:ffff:ffff:ffff:ffff:ffff         0000000000000000:0000000000000000:0000000000000111:1111111111111111:1111111111111111:1111111111111111:1111111111111111:1111111111111111
            Broadcast:  2001:db8:7:ffff:ffff:ffff:ffff:ffff  0010000000000001:0000110110111000:0000000000000111:1111111111111111:1111111111111111:1111111111111111:1111111111111111:1111111111111111
            First Host: 2001:db8::1                          0010000000000001:0000110110111000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000001
            Last Host:  2001:db8:7:ffff:ffff:ffff:ffff:fffe  0010000000000001:0000110110111000:0000000000000111:1111111111111111:1111111111111111:1111111111111111:1111111111111111:1111111111111110
            Hosts/Net:  9,671,406,556,917,033,397,649,406
            
            """;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Totals_IPv4_01()
    {
        Assert.That(NetworkPrefix.Parse("10.0.0.0/24").TotalAddresses, Is.EqualTo(new BigInteger(256)));
        Assert.That(NetworkPrefix.Parse("10.0.0.0/24").TotalHosts, Is.EqualTo((UInt128)254));
        Assert.That(NetworkPrefix.Parse("10.0.0.0/31").TotalHosts, Is.EqualTo((UInt128)2));
        Assert.That(NetworkPrefix.Parse("10.0.0.0/32").TotalHosts, Is.EqualTo((UInt128)1));
        Assert.That(NetworkPrefix.Parse("0.0.0.0/0").TotalAddresses, Is.EqualTo(BigInteger.Pow(2, 32)));
    }

    [Test]
    public void Totals_IPv6_01()
    {
        // ::/0 holds 2^128 addresses, one more than a UInt128 can hold
        var everyAddress = NetworkPrefix.Parse("::/0");

        Assert.That(everyAddress.TotalAddresses, Is.EqualTo(BigInteger.Pow(2, 128)));
        Assert.That(everyAddress.TotalHosts, Is.EqualTo(UInt128.MaxValue - 1));
    }

    [Test]
    public void Widths_IPv4_01()
    {
        var network = NetworkPrefix.Parse("10.0.0.0/24");

        Assert.That(network.AddressBitLength, Is.EqualTo(32));
        Assert.That(network.PrefixLength, Is.EqualTo(24));
        Assert.That(network.HostBitLength, Is.EqualTo(8));
        Assert.That(network.IsHostPrefix, Is.False);
        Assert.That(network.Version, Is.EqualTo(IPVersion.IPv4));
        Assert.That(network.NetworkAddressBits, Is.EqualTo((UInt128)0x0A_00_00_00));
        Assert.That(network.NetworkMaskBits, Is.EqualTo((UInt128)0xFF_FF_FF_00));
        Assert.That(network.HostMaskBits, Is.EqualTo((UInt128)0xFF));
        Assert.That(NetworkPrefix.Parse("10.0.0.1").IsHostPrefix, Is.True);
    }

    [Test]
    public void Widths_IPv6_01()
    {
        var network = NetworkPrefix.Parse("2001:db8::/32");

        Assert.That(network.AddressBitLength, Is.EqualTo(128));
        Assert.That(network.HostBitLength, Is.EqualTo(96));
        Assert.That(network.Version, Is.EqualTo(IPVersion.IPv6));
        Assert.That(network.NetworkMask, Is.EqualTo(IPAddress.Parse("ffff:ffff::")));
        Assert.That(network.HostMask, Is.EqualTo(IPAddress.Parse("::ffff:ffff:ffff:ffff:ffff:ffff")));
        Assert.That(NetworkPrefix.Parse("2001:db8::1").IsHostPrefix, Is.True);
    }

    [Test]
    public void XmlConverter_01()
    {
        var network = NetworkPrefix.Parse("10.20.30.40/15");

        var outputStream = new MemoryStream();
        var xmlSerializer = new XmlSerializer(typeof(NetworkPrefix));
        xmlSerializer.Serialize(outputStream, network);
        outputStream.Seek(0, SeekOrigin.Begin);

        var actual = new StreamReader(outputStream).ReadToEnd();

        var expected =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <NetworkPrefix>10.20.0.0/15</NetworkPrefix>
            """;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void XmlConverter_02()
    {
        var networks = new List<NetworkPrefix> { "10.20.30.40/15", "50.60.70.80/15" };

        var outputStream = new MemoryStream();
        var xmlSerializer = new XmlSerializer(typeof(List<NetworkPrefix>));
        xmlSerializer.Serialize(outputStream, networks);
        outputStream.Seek(0, SeekOrigin.Begin);

        var actual = new StreamReader(outputStream).ReadToEnd();

        var expected =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ArrayOfNetworkPrefix xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <NetworkPrefix>10.20.0.0/15</NetworkPrefix>
              <NetworkPrefix>50.60.0.0/15</NetworkPrefix>
            </ArrayOfNetworkPrefix>
            """;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void XmlConverter_03()
    {
        var xml =
            """
            <?xml version="1.0" encoding="utf-8"?>
              <NetworkPrefix>10.20.0.0/15</NetworkPrefix>
            """;

        var inputStream = new StringReader(xml);
        var xmlSerializer = new XmlSerializer(typeof(NetworkPrefix));
        var actual = (NetworkPrefix?)xmlSerializer.Deserialize(inputStream);
        var expected = NetworkPrefix.Parse("10.20.30.40/15");

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void XmlConverter_04()
    {
        var xml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ArrayOfNetworkPrefix xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <NetworkPrefix>10.20.0.0/15</NetworkPrefix>
              <NetworkPrefix>50.60.0.0/15</NetworkPrefix>
            </ArrayOfNetworkPrefix>
            """;

        var inputStream = new StringReader(xml);
        var xmlSerializer = new XmlSerializer(typeof(List<NetworkPrefix>));
        var actual = (List<NetworkPrefix>?)xmlSerializer.Deserialize(inputStream);
        var expected = new List<NetworkPrefix> { "10.20.0.0/15", "50.60.0.0/15" };

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void XmlConverter_05()
    {
        // XmlSerializer wraps the exception that the prefix throws in an InvalidOperationException
        var xmlSerializer = new XmlSerializer(typeof(NetworkPrefix));

        var emptyElementException = Assert.Throws<InvalidOperationException>(() => xmlSerializer.Deserialize(new StringReader("<NetworkPrefix />")));
        Assert.That(emptyElementException?.InnerException, Is.TypeOf<System.Xml.XmlException>());

        var malformedPrefixException = Assert.Throws<InvalidOperationException>(() => xmlSerializer.Deserialize(new StringReader("<NetworkPrefix>10.0.0.0/33</NetworkPrefix>")));
        Assert.That(malformedPrefixException?.InnerException, Is.TypeOf<FormatException>());
    }

    /// <summary>
    ///     Asserts that a prefix has every property that a test case expects.
    /// </summary>
    private static void AssertMatches(NetworkPrefix network, NetworkPrefixTestRecord testAddress)
    {
        Assert.That(network.NetworkAddress, Is.EqualTo(testAddress.NetworkAddress));
        Assert.That(network.PrefixLength, Is.EqualTo(testAddress.PrefixLength));
        Assert.That(network.NetworkMask, Is.EqualTo(testAddress.NetworkMask));
        Assert.That(network.BroadcastAddress, Is.EqualTo(testAddress.BroadcastAddress));
        Assert.That(network.HostMask, Is.EqualTo(testAddress.HostMask));
        Assert.That(network.FirstHost, Is.EqualTo(testAddress.FirstHost));
        Assert.That(network.LastHost, Is.EqualTo(testAddress.LastHost));
    }

    /// <summary>
    ///     Parses text through the <see cref="IParsable{TSelf}" /> interface.
    /// </summary>
    private static TParsable ParseWithInterface<TParsable>(string value) where TParsable : IParsable<TParsable>
    {
        return TParsable.Parse(value, null);
    }

    /// <summary>
    ///     Parses text through the <see cref="ISpanParsable{TSelf}" /> interface.
    /// </summary>
    private static TParsable ParseSpanWithInterface<TParsable>(string value) where TParsable : ISpanParsable<TParsable>
    {
        return TParsable.Parse(value.AsSpan(), null);
    }
}
