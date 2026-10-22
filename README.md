# Bitvantage.InternetProtocol.NetworkPrefix
A library for working with IPv4 and IPv6 network prefixes. It also adds its features to .NET's built-in
`System.Net.IPNetwork`, which on its own can only parse a network and test whether an address is in it.

Requires .NET 10 or later.

## Installing via NuGet Package Manager
```sh
PM> NuGet\Install-Package Bitvantage.InternetProtocol.NetworkPrefix
```

## Quick Start
```csharp
using System.Net;
using Bitvantage.InternetProtocol;

var network = NetworkPrefix.Parse("10.0.0.0/24");

var networkAddress = network.NetworkAddress;     // 10.0.0.0
var mask = network.NetworkMask;                  // 255.255.255.0
var broadcast = network.BroadcastAddress;        // 10.0.0.255
var firstHost = network.FirstHost;               // 10.0.0.1
var totalHosts = network.TotalHosts;             // 254

// iterate through each host address
foreach (var address in network.Hosts())
    Console.WriteLine(address);

var containsAddress = network.Contains(IPAddress.Parse("10.0.0.100"));      // true
var containsNetwork = network.Contains(NetworkPrefix.Parse("10.0.0.0/25")); // true
var overlaps = network.Overlaps(NetworkPrefix.Parse("10.0.0.128/25"));      // true
```

## Parsing and formatting
```csharp
var fromMask = NetworkPrefix.Parse("10.0.0.0 255.255.255.0");    // 10.0.0.0/24
var fromHost = NetworkPrefix.Parse("10.0.0.7");                  // 10.0.0.7/32
var truncated = NetworkPrefix.Parse("10.1.2.3/8");               // 10.0.0.0/8, host bits are cleared
var isValid = NetworkPrefix.TryParse("10.0.0.0/33", out _);      // false, TryParse never throws

// Parse never touches the network; host names are looked up with Resolve or ResolveAsync
var resolved = await NetworkPrefix.ResolveAsync("example.com/24");

Console.WriteLine($"{network:AddressAndMask}");                        // 10.0.0.0 255.255.255.0
Console.WriteLine(network.ToString(NetworkFormat.AddressAndHostMask)); // 10.0.0.0 0.0.0.255
Console.WriteLine(network.ToString(NetworkFormat.Detail));
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

## Working with sets of networks
```csharp
var subnets = network.Split(26);                                      // 10.0.0.0/26, 10.0.0.64/26, 10.0.0.128/26, 10.0.0.192/26
var supernet = network.GetSupernet(16);                               // 10.0.0.0/16
var remaining = network.Exclude(NetworkPrefix.Parse("10.0.0.0/26"));  // 10.0.0.128/25, 10.0.0.64/26
var nextNetwork = network + 1;                                        // 10.0.1.0/24

// the fewest prefixes that cover exactly the same addresses
var summary = NetworkPrefix.Summarize([
    NetworkPrefix.Parse("10.0.0.0/25"),
    NetworkPrefix.Parse("10.0.0.128/25"),
    NetworkPrefix.Parse("10.0.0.7/32")]);                             // 10.0.0.0/24

// the smallest prefix that covers both
var containing = NetworkPrefix.GetContainingNetwork(
    NetworkPrefix.Parse("10.0.0.0/24"),
    NetworkPrefix.Parse("10.0.3.0/24"));                              // 10.0.0.0/22
```

## Classifying address space
```csharp
NetworkPrefix.Parse("10.1.0.0/16").Allocation;    // Private
NetworkPrefix.Parse("100.64.0.0/10").Allocation;  // SharedAddressSpace
NetworkPrefix.Parse("fe80::/64").Allocation;      // LinkLocal
NetworkPrefix.Parse("2001:db8::/32").Allocation;  // Documentation
NetworkPrefix.Parse("10.1.0.0/16").Class;         // A
```

## Using System.Net.IPNetwork
`NetworkPrefix` converts implicitly to and from `IPNetwork`, so it can be passed to any API that takes one.
The same features are also available directly on `IPNetwork` as C# 14 extension members.

```csharp
var ipNetwork = IPNetwork.Parse("192.168.0.0/16");

var mask = ipNetwork.NetworkMask;                                       // 255.255.0.0
var halves = ipNetwork.Split();                                         // 192.168.0.0/17, 192.168.128.0/17
var containsSubnet = ipNetwork.Contains(IPNetwork.Parse("192.168.1.0/24")); // true
var allocation = ipNetwork.Allocation;                                  // Private

// IPNetwork is not IComparable, so sorting needs a comparer
var sorted = networks.Order(IPNetwork.Comparer);
var summarized = IPNetwork.Summarize(networks);

NetworkPrefix prefix = ipNetwork;
IPNetwork backAgain = prefix;
```

## Serialization
`NetworkPrefix` serializes to and from a JSON string and XML text in CIDR notation, and works as a dictionary key in
JSON. System.Text.Json cannot serialize an `IPNetwork` on its own, so the library includes a converter for it.

```csharp
var json = JsonSerializer.Serialize(new Dictionary<NetworkPrefix, string> { [network] = "office" }); // {"10.0.0.0/24":"office"}

var options = new JsonSerializerOptions();
options.Converters.Add(new IPNetworkJsonConverter());
var ipNetworkJson = JsonSerializer.Serialize(ipNetwork, options);                                  // "192.168.0.0/16"
```
