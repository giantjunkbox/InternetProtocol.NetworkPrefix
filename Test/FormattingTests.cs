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

public class FormattingTests
{
    [Test]
    public void ToString_IPv4_01()
    {
        var network = Network("10.20.30.0/24");

        Assert.That(network.ToString(), Is.EqualTo("10.20.30.0/24"));
        Assert.That(network.ToString(NetworkFormat.AddressAndPrefix), Is.EqualTo("10.20.30.0/24"));
        Assert.That(network.ToString(NetworkFormat.AddressAndMask), Is.EqualTo("10.20.30.0 255.255.255.0"));
        Assert.That(network.ToString(NetworkFormat.AddressAndHostMask), Is.EqualTo("10.20.30.0 0.0.0.255"));
        Assert.That(network.ToString(NetworkFormat.Address), Is.EqualTo("10.20.30.0"));
        Assert.That(network.ToString(NetworkFormat.Mask), Is.EqualTo("255.255.255.0"));
        Assert.That(network.ToString(NetworkFormat.HostMask), Is.EqualTo("0.0.0.255"));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.ToString((NetworkFormat)99));
    }

    [Test]
    public void ReverseZone_IPv4_01()
    {
        Assert.That(Network("10.0.0.0/24").ReverseZone, Is.EqualTo("0.0.10.in-addr.arpa"));
        Assert.That(Network("10.0.0.0/8").ReverseZone, Is.EqualTo("10.in-addr.arpa"));
        Assert.That(Network("0.0.0.0/0").ReverseZone, Is.EqualTo("in-addr.arpa"));
        Assert.That(Network("10.1.2.3/32").ReverseZone, Is.EqualTo("3.2.1.10.in-addr.arpa"));

        // a network that does not end on a byte has no zone of its own
        Assert.Throws<InvalidOperationException>(() => _ = Network("10.0.0.0/23").ReverseZone);
    }

    [Test]
    public void ReverseZone_IPv6_01()
    {
        Assert.That(Network("2001:db8::/32").ReverseZone, Is.EqualTo("8.b.d.0.1.0.0.2.ip6.arpa"));
        Assert.That(Network("2001:db8:a::/52").ReverseZone, Is.EqualTo("0.a.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa"));
        Assert.That(Network("::/0").ReverseZone, Is.EqualTo("ip6.arpa"));

        // a network that does not end on a hexadecimal digit has no zone of its own
        Assert.Throws<InvalidOperationException>(() => _ = Network("2001:db8::/30").ReverseZone);
    }

    [Test]
    public void ToString_IPv4_02()
    {
        var actual = Network("10.20.30.0/24").ToString(NetworkFormat.Detail);
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
        // the host count of a host network is written as a plain number
        var hostCountLine = Network("10.20.30.40/32")
            .ToString(NetworkFormat.Detail)
            .Split(Environment.NewLine)
            .Single(line => line.StartsWith("Hosts/Net:"));

        Assert.That(hostCountLine, Is.EqualTo("Hosts/Net:  1"));
    }

    [Test]
    public void ToString_IPv6_01()
    {
        // IPv6 has no broadcast address, so the detail has no broadcast row and the last address is the last host
        var actual = Network("2001:0db8:0001:0000:0000:0ab9:C0A8:0102/45").ToString(NetworkFormat.Detail);
        var expected =
            """
            Address:    2001:db8::                           0010000000000001:0000110110111000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000
            Mask:       ffff:ffff:fff8::                     1111111111111111:1111111111111111:1111111111111000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000
            Wildcard:   ::7:ffff:ffff:ffff:ffff:ffff         0000000000000000:0000000000000000:0000000000000111:1111111111111111:1111111111111111:1111111111111111:1111111111111111:1111111111111111
            First Host: 2001:db8::1                          0010000000000001:0000110110111000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000000:0000000000000001
            Last Host:  2001:db8:7:ffff:ffff:ffff:ffff:ffff  0010000000000001:0000110110111000:0000000000000111:1111111111111111:1111111111111111:1111111111111111:1111111111111111:1111111111111111
            Hosts/Net:  9,671,406,556,917,033,397,649,407
            
            """;

        Assert.That(actual, Is.EqualTo(expected));
    }
}
