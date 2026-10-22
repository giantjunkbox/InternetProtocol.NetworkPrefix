# Bitvantage.IPNetworkExtensions
CIDR, subnet and supernet math for .NET's built-in `System.Net.IPNetwork`.

`IPNetwork` can parse a network and test whether an address is in it. This library adds the rest as C# 14 extension
members: masks, host ranges, subnet and supernet tests, splitting, supernets, exclusion, summarization, address ranges,
address planning, sorting, classification, strict parsing, reverse DNS names and JSON serialization. It also rounds out
`IPAddress` with sorting, arithmetic, classification and strict parsing. The members appear on `IPNetwork` and
`IPAddress` themselves, so there is no separate type to convert to and from.

Requires .NET 10 or later. This package replaces `Bitvantage.InternetProtocol.NetworkPrefix`; see the changelog for the
migration from its `NetworkPrefix` class.

## Installing via NuGet Package Manager
```sh
PM> NuGet\Install-Package Bitvantage.IPNetworkExtensions
```

## Quick Start
```csharp
using System.Net;
using Bitvantage.InternetProtocol;

var network = IPNetwork.ParseStrict("10.0.0.0/24");

var mask = network.NetworkMask;                  // 255.255.255.0
var broadcast = network.BroadcastAddress;        // 10.0.0.255
var firstHost = network.FirstHost;               // 10.0.0.1
var totalHosts = network.TotalHosts;             // 254

foreach (var address in network.Hosts())
    Console.WriteLine(address);

var containsAddress = network.Contains(IPAddress.Parse("10.0.0.100"));    // true, built into IPNetwork
var isSupernet = network.IsSupernetOf(IPNetwork.Parse("10.0.0.0/25"));    // true
var overlaps = network.Overlaps(IPNetwork.Parse("10.0.0.128/25"));        // true
```

The subnet and supernet tests follow the names of .NET's set methods, such as `HashSet<T>.IsSubsetOf`. A network is a
subnet and a supernet of itself, but not a proper one.

| Method               | True when                                        |
|----------------------|--------------------------------------------------|
| `IsSupernetOf`       | the other network is inside this one, or equal   |
| `IsProperSupernetOf` | the other network is inside this one, not equal  |
| `IsSubnetOf`         | this network is inside the other, or equal       |
| `IsProperSubnetOf`   | this network is inside the other, not equal      |
| `Overlaps`           | the two networks share any address               |

## Parsing
`IPNetwork.Parse` accepts every IPv4 form that `IPAddress.Parse` does, and reads a number with a leading zero as octal,
so `010.0.0.0/8` becomes `8.0.0.0/8`. Use `ParseStrict` for text from users, files or other systems. It accepts only
the standard dotted-decimal form for IPv4, and it also accepts a network mask or a single address.

```csharp
var fromMask = IPNetwork.ParseStrict("10.0.0.0 255.255.255.0");      // 10.0.0.0/24
var fromHost = IPNetwork.ParseStrict("10.0.0.7");                    // 10.0.0.7/32
var isValid = IPNetwork.TryParseStrict("10.0.0.0/33", out _);        // false, never throws
var isOctal = IPNetwork.TryParseStrict("010.0.0.0/8", out _);        // false
var builtIn = IPNetwork.Parse("010.0.0.0/8");                        // 8.0.0.0/8

var hostNetwork = IPNetwork.FromAddress(IPAddress.Parse("2001:db8::1"));                          // 2001:db8::1/128
var maskNetwork = IPNetwork.FromMask(IPAddress.Parse("10.1.2.3"), IPAddress.Parse("255.255.0.0")); // 10.1.0.0/16

// wildcard masks, as Cisco access control lists write them, parse too
var wildcard = IPNetwork.ParseStrict("10.0.0.0 0.0.0.255");                                       // 10.0.0.0/24
var fromHostMask = IPNetwork.FromHostMask(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("0.0.0.255")); // 10.0.0.0/24
var mask = IPNetwork.GetNetworkMask(IPVersion.IPv4, 24);                                          // 255.255.255.0

// parsing never touches the network; host names are looked up with Resolve or ResolveAsync
var resolved = await IPNetwork.ResolveAsync("example.com/24");
```

## Address ranges
Firewalls, access control lists and cloud security groups usually take networks, while people, DHCP scopes and vendor
allow-lists often give ranges.

```csharp
var networks = IPNetwork.Summarize(IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.9")); // 10.0.0.1/32, 10.0.0.2/31, 10.0.0.4/30, 10.0.0.8/31
var parsed = IPNetwork.ParseRange("192.0.2.0 - 192.0.2.130");                                 // 192.0.2.0/25, 192.0.2.128/31, 192.0.2.130/32
var around = IPNetwork.GetContainingNetwork(IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.9")); // 10.0.0.0/28
```

## Working with sets of networks
```csharp
var subnets = network.Split(26);                                   // 10.0.0.0/26, 10.0.0.64/26, 10.0.0.128/26, 10.0.0.192/26
var supernet = network.GetSupernet(16);                            // 10.0.0.0/16
var remaining = network.Exclude(IPNetwork.Parse("10.0.0.0/26"));   // 10.0.0.64/26, 10.0.0.128/25

// the fewest networks that cover exactly the same addresses
var summary = IPNetwork.Summarize([
    IPNetwork.Parse("10.0.0.0/25"),
    IPNetwork.Parse("10.0.0.128/25"),
    IPNetwork.Parse("10.0.0.7/32")]);                              // 10.0.0.0/24

// the smallest network that covers both
var containing = IPNetwork.GetContainingNetwork(
    IPNetwork.Parse("10.0.0.0/24"),
    IPNetwork.Parse("10.0.3.0/24"));                               // 10.0.0.0/22
```

## Address planning
```csharp
var plan = IPNetwork.ParseStrict("10.0.0.0/22");
var allocated = new[] { IPNetwork.Parse("10.0.0.0/24"), IPNetwork.Parse("10.0.1.128/25") };

var free = plan.Exclude(allocated);                                       // 10.0.1.0/25, 10.0.2.0/23
var firstFreeSlash24 = plan.AvailableSubnets(24, allocated).First();      // 10.0.2.0/24
var difference = IPNetwork.Exclude([IPNetwork.Parse("10.0.0.0/23")], allocated); // 10.0.1.0/25, the set difference of two sets

var gateway = network.GetAddress(1);                                      // 10.0.0.1
var offset = network.GetOffset(IPAddress.Parse("10.0.0.100"));           // 100
```

## Sorting and operators
`IPNetwork` does not implement `IComparable`, so sorting it, or adding it to a sorted collection, throws unless a
comparer is given.

```csharp
var sorted = networks.Order(IPNetwork.Comparer);
var sortedSet = new SortedSet<IPNetwork>(networks, IPNetwork.Comparer);

var nextNetwork = network + 1;                                     // 10.0.1.0/24
var isBefore = network < nextNetwork;                              // true
```

## Classifying address space
```csharp
IPNetwork.Parse("10.1.0.0/16").Allocation;    // Private
IPNetwork.Parse("100.64.0.0/10").Allocation;  // SharedAddressSpace
IPNetwork.Parse("fe80::/64").Allocation;      // LinkLocal
IPNetwork.Parse("2001:db8::/32").Allocation;  // Documentation
IPNetwork.Parse("10.1.0.0/16").Class;         // A
```

## Addresses
`IPAddress` gains the same strict parsing, sort order and classification, plus arithmetic and its integer form.

```csharp
var address = IPAddress.ParseStrict("10.0.0.7");     // IPAddress.Parse would read 010.0.0.7 as octal; this rejects it
var next = address + 1;                              // 10.0.0.8
var isBefore = address < next;                       // true
var sorted = addresses.Order(IPAddress.Comparer);
var allocation = address.Allocation;                 // Private
var integer = address.AddressBits;                   // 167772167, the same form as network.BaseAddressBits
var isMask = IPAddress.Parse("255.255.255.0").TryGetPrefixLength(out var prefixLength); // true, 24
```

A dual-stack server reports IPv4 clients as IPv4-mapped IPv6 addresses, such as `::ffff:10.1.2.3`. Classification treats
them as the IPv4 address they carry. The subnet and supernet tests keep IPv4 and IPv6 apart, as Go's `net/netip` does,
while the built-in `Contains` maps them. `Unmap` converts such an address to IPv4.

```csharp
var client = IPAddress.Parse("::ffff:10.1.2.3");
client.Allocation;                                                      // Private
IPNetwork.Parse("10.0.0.0/8").Contains(client);                         // true, built into IPNetwork
IPNetwork.Parse("10.0.0.0/8").IsSupernetOf(IPNetwork.FromAddress(client));         // false
IPNetwork.Parse("10.0.0.0/8").IsSupernetOf(IPNetwork.FromAddress(client.Unmap())); // true
```

## Formatting
```csharp
network.ToString(NetworkFormat.AddressAndMask);       // 10.0.0.0 255.255.255.0
network.ToString(NetworkFormat.AddressAndHostMask);   // 10.0.0.0 0.0.0.255
network.ToString(NetworkFormat.HostMask);             // 0.0.0.255
network.ReverseZone;                                  // 0.0.10.in-addr.arpa
IPAddress.Parse("10.0.0.7").ReversePointer;           // 7.0.0.10.in-addr.arpa
network.ToString(NetworkFormat.Detail);
```
```
Address:    10.0.0.0       00001010.00000000.00000000.00000000
Mask:       255.255.255.0  11111111.11111111.11111111.00000000
Wildcard:   0.0.0.255      00000000.00000000.00000000.11111111
Broadcast:  10.0.0.255     00001010.00000000.00000000.11111111
First Host: 10.0.0.1       00001010.00000000.00000000.00000001
Last Host:  10.0.0.254     00001010.00000000.00000000.11111110
Hosts/Net:  254
```

## Performance
The members convert a network to integers on each call, which takes a few nanoseconds and allocates nothing. Subnet,
supernet and overlap tests cost about as much as the built-in `Contains` for an address. Members that return an
`IPAddress`, such as `NetworkMask`, create a new address on every call. In a hot loop, use the integer members instead:
`BaseAddressBits`, `NetworkMaskBits` and `HostMaskBits` on the network, and `AddressBits` on the address being tested.
Set operations such as `Exclude` and `Summarize` run in time proportional to the number of networks; removing 100,000
host addresses from a /8 takes under a second.

## Serialization
System.Text.Json cannot serialize an `IPNetwork` on its own. Register `IPNetworkJsonConverter`, or apply it to a single
property with `[JsonConverter(typeof(IPNetworkJsonConverter))]`. Networks then serialize as strings in CIDR notation,
and also work as dictionary keys.

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new IPNetworkJsonConverter());

var json = JsonSerializer.Serialize(new Dictionary<IPNetwork, string> { [network] = "office" }, options); // {"10.0.0.0/24":"office"}
```

`XmlSerializer` writes an empty element for an `IPNetwork` without reporting an error. To serialize a network as XML,
store it in a `string` property and convert it with `ToString` and `IPNetwork.ParseStrict`.
