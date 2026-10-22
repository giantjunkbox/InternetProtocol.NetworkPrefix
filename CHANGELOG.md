# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - Unreleased

This release moves the library to .NET 10, fixes a number of defects, reworks the API for clarity, and adds the
library's features to `System.Net.IPNetwork`. Backwards compatibility was not a goal; see the migration notes.

### Changed

- The library targets .NET 10. .NET 7 is no longer supported by Microsoft.
- `NetworkPrefix` is now `sealed` and immutable. `Address`, `Length`, `Version`, `HostMaskBits` and
  `NetworkMaskBits` had public setters, so changing, for example, `Length` left the masks and network address
  describing a different network than the prefix length did.
- Members were renamed so that each name says what it holds, and so that they match `IPNetwork` where it has an
  equivalent:

  | Before              | After                 |
  |---------------------|-----------------------|
  | `Address`           | `NetworkAddress`      |
  | `Length`            | `PrefixLength`        |
  | `AddressLength`     | `AddressBitLength`    |
  | `NetworkBits`       | `NetworkAddressBits`  |
  | `Broadcast`         | `BroadcastAddress`    |
  | `Mask`              | `NetworkMask`         |
  | `Wildcard`          | `HostMask`            |
  | `RemoveNetwork`     | `Exclude`             |
  | `AddressAllocation.AutomaticPrivateIPAddressing` | `AddressAllocation.LinkLocal` |

- The conversion from `string` to `NetworkPrefix` is explicit, because it throws for invalid text, and the implicit
  conversion from `NetworkPrefix` to `string` was removed. Use `Parse` or a cast, and `ToString`.
- For IPv6, which has no broadcast address, `LastHost`, `Hosts()` and `TotalHosts` now include the last address, and
  only the subnet-router anycast address, which is the network address, is left out. A /64 has 2^64 - 1 hosts rather
  than 2^64 - 2. The detail format no longer shows a broadcast row for IPv6.
- `Parse` and `TryParse` no longer look up host names. Parsing a string made a blocking DNS query, which meant that
  JSON or XML deserialization, and the implicit conversion from `string`, could reach the network. Host names are now
  looked up only by the new `Resolve` and `ResolveAsync` methods.
- `Exclude` follows set semantics. It returns an empty list when the excluded network covers the whole network, and
  the network itself when the two do not overlap, where `RemoveNetwork` threw in both cases. It returns
  `IReadOnlyList<NetworkPrefix>`.
- `Summarize` returns the true minimal set: covered networks are dropped, and the result is sorted. It returns
  `IReadOnlyList<NetworkPrefix>`.
- `Split(int)` throws `ArgumentOutOfRangeException` for an invalid target, and both `Split` overloads throw when they
  are called rather than when the result is first enumerated.
- `ComplementaryPrefix` throws `InvalidOperationException` for a /0, rather than `ArgumentException`.
- `FirstHost` and `LastHost` work for a /0, rather than throwing. `TotalHosts` already treated a /0 as a network with
  hosts.
- The constructors throw `ArgumentException` for an address that is not IPv4 or IPv6, rather than
  `NotSupportedException`.
- `GetContainingNetwork(IEnumerable<NetworkPrefix>)` throws `ArgumentException` for an empty sequence, rather than
  `ArgumentNullException`.
- `IXmlSerializable` is implemented explicitly, so its methods no longer appear on the class. An empty element throws
  `XmlException` rather than `NullReferenceException`.
- `NetworkPrefixJsonConverter` is `sealed`, and throws `JsonException` for a value that is not a string or not a
  valid prefix.
- The `[Serializable]` attribute was removed; .NET 10 no longer includes the binary formatter it was for.

### Added

- Extension members on `System.Net.IPNetwork`, using C# 14 extension members, that provide the features of
  `NetworkPrefix`: masks, broadcast and host addresses, totals, `Contains(IPNetwork)` and the other containment tests,
  `Overlaps`, `Addresses`, `Hosts`, `Split`, `GetSupernet`, `Exclude`, `Allocation`, `Class`, and
  `ToString(NetworkFormat)`; and the static members `IPNetwork.Summarize`, `IPNetwork.GetContainingNetwork` and
  `IPNetwork.Comparer`.
- Implicit conversions between `NetworkPrefix` and `IPNetwork`, `ToIPNetwork`, `FromIPNetwork`, a constructor that
  takes an `IPNetwork`, and `==` and `!=` between the two types.
- `IPNetworkJsonConverter`, because System.Text.Json cannot serialize an `IPNetwork` without one.
- JSON dictionary key support for `NetworkPrefix`.
- `Overlaps`, `GetSupernet()`, `GetSupernet(int)`, `LastAddress`, `HostBitLength` and `IsHostPrefix`.
- The `<=` and `>=` operators. The comparison operators now handle `null` the same way `CompareTo` does.
- `IEquatable<NetworkPrefix>`, the non-generic `IComparable`, `IFormattable`, `IParsable<NetworkPrefix>` and
  `ISpanParsable<NetworkPrefix>`, with `Parse` and `TryParse` overloads that take a `ReadOnlySpan<char>`. A
  `NetworkFormat` name can be used as a format string, as in `$"{prefix:AddressAndMask}"`.
- `NetworkFormat.AddressAndHostMask`, the address and wildcard form used by Cisco access control lists.
- IPv6 allocations, which were all `Undefined`, and the `SharedAddressSpace`, `Documentation`, `Benchmarking` and
  `Unspecified` allocations.
- XML documentation for every public member.

### Fixed

- Parsing accepted every IPv4 form that `IPAddress` accepts, including shorthand such as `10.0.0`, whole numbers and
  hexadecimal, and read a number with a leading zero as octal, so `010.0.0.0/8` parsed as `8.0.0.0/8`. The octal
  reading is a known source of security bugs. Only the standard form of four decimal numbers without leading zeros is
  now accepted, for both addresses and masks.
- Converting an IPv4 address to an `IPAddress` wrote 16 bytes into a 4 byte array, past the end of the array. This
  could corrupt whatever object followed it in memory.
- `TryParse` threw, rather than returning `false`, for a prefix length out of range, such as `10.0.0.0/33`, a negative
  prefix length, or a network mask with gaps, such as `255.0.255.0`.
- Text with more than one separator, such as `10.0.0.0/24/5` or an address and mask separated by two spaces, parsed as
  a /0 rather than being rejected or parsed correctly.
- Text with leading whitespace, such as ` 10.0.0.0/24`, parsed as `0.0.0.0/0`. The empty text before the space was
  looked up in DNS as a host name, and the remaining parts were then miscounted.
- A network mask of the other IP version was accepted, so `10.0.0.0 ffff::` parsed as `10.0.0.0/16`.
- `GetContainingNetwork` returned the IPv4 network `0.0.0.0/0` when either IPv6 input was a /0.
- `Summarize` threw `ArgumentException` when the input held a duplicate, or held a network together with both of its
  halves, and kept networks that were covered by another network in the input.
- `Addresses()` and `Hosts()` never ended for a prefix that includes the last IPv6 address,
  `ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff`, because the counter wrapped around to `::`.
- `Split(128)` on `::/0` returned a single prefix, because the number of subnets, 2^128, overflowed.
- The constructors threw `IndexOutOfRangeException` for a negative prefix length.
- `ToString(NetworkFormat.Detail)` showed a host count of `01` for a host prefix and `02` for a point-to-point prefix.
- `<` and `>` threw `NullReferenceException` when either side was `null`.

### Migration

- Apply the renames in the table above.
- Replace `Parse` or `TryParse` calls that pass a host name with `Resolve` or `ResolveAsync`.
- Replace implicit conversions from text with `Parse` or an explicit cast, and implicit conversions to text with
  `ToString`.
- Write IPv4 addresses in the standard dotted-decimal form; text such as `10.0.0/24` or `010.0.0.0/8` is rejected.
- Expect IPv6 host ranges and counts to include the last address.
- Replace `RemoveNetwork` with `Exclude`, and remove any handling of the exceptions it threw for an equal, containing
  or disjoint network.
- If you relied on `Summarize` keeping covered networks, or on the order it returned, adjust for the sorted minimal
  result.
- Catch `ArgumentOutOfRangeException` rather than `InvalidOperationException` for an invalid `Split(int)` target.

## [2.0.0] - 2026-08-19

This release corrects four cases where a boundary condition produced the wrong answer, and renames one
method so that the containment API reads consistently. Every one of these changes can alter the result
of an existing call, so the major version has been incremented.

### Removed

- `ContainsBy(NetworkPrefix)` has been renamed to `ContainedBy(NetworkPrefix)`. The old name read as
  though it were a variant of `Contains`, when in fact it is the inverse of it and the strict
  counterpart to `ContainedByOrEqual`. There is no compatibility shim; callers must be updated.

  ```csharp
  // before
  smallerNetwork.ContainsBy(largerNetwork);

  // after
  smallerNetwork.ContainedBy(largerNetwork);
  ```

  The containment API is now two matched pairs, plus address membership:

  | Method                  | Meaning                                          |
  |-------------------------|--------------------------------------------------|
  | `Contains`              | strictly contains the other prefix                |
  | `ContainsOrEqual`       | contains the other prefix, or is equal to it      |
  | `ContainedBy`           | is strictly contained by the other prefix         |
  | `ContainedByOrEqual`    | is contained by the other prefix, or equal to it  |
  | `Contains(IPAddress)`   | the address is a member of this prefix            |

### Fixed

- `Contains(IPAddress)` returned `false` when a host prefix was asked about the single address it
  represents. An address is a member of a prefix rather than a prefix nested inside of it, so the
  overload now resolves through `ContainsOrEqual`. `Contains(NetworkPrefix)` is unchanged and remains
  strict; use `ContainsOrEqual` for the inclusive comparison between two prefixes.

  ```csharp
  NetworkPrefix.Parse("10.20.30.40/32").Contains(IPAddress.Parse("10.20.30.40")); // was false, now true
  ```

  Only `/32` and `/128` prefixes were affected; every shorter prefix already answered correctly.

- `GetContainingNetwork` derived the result from the differing bits of the two network addresses
  without consulting their prefix lengths. When one network contained the other but the two did not
  share the same network address, the differing bit fell inside the host portion of the less specific
  network and the result was too specific to cover it. The result is now never more specific than the
  less specific of the two networks. `GetContainingNetwork(IEnumerable<NetworkPrefix>)` is fixed by
  the same change.

  ```csharp
  NetworkPrefix.GetContainingNetwork("10.20.0.0/16", "10.20.30.0/24"); // was 10.20.0.0/19, now 10.20.0.0/16
  ```

- `Split(int)` rejected a host prefix as a split target, so a network could not be split into `/32` or
  `/128` prefixes and `Split()` on a `/31` or `/127` always threw. A host prefix is now a valid target.

  ```csharp
  NetworkPrefix.Parse("10.20.30.0/31").Split(); // was InvalidOperationException, now 10.20.30.0/32 and 10.20.30.1/32
  ```

- `RemoveNetwork(NetworkPrefix)` threw `InvalidOperationException` whenever the network being removed
  was a host prefix, because it splits down to the target and could not reach host length. Removing a
  host route from a network now works. This was a consequence of the `Split` defect above and needed
  no separate change.

  ```csharp
  NetworkPrefix.Parse("10.20.30.0/29").RemoveNetwork("10.20.30.1/32"); // was InvalidOperationException,
                                                                      // now 10.20.30.4/30, 10.20.30.2/31, 10.20.30.0/32
  ```

- `Split(int)` did not validate that the target was more specific than the network being split. A
  target equal to or shorter than the network produced a negative shift, which wrapped and yielded an
  effectively unbounded sequence of prefixes lying outside the network. Such a target now throws
  `InvalidOperationException`, consistent with a target longer than a host prefix.

  ```csharp
  NetworkPrefix.Parse("10.20.30.0/24").Split(16); // was 10.20.0.0/16, 10.21.0.0/16, ... without end,
                                                  // now InvalidOperationException
  ```

- `CompareTo(null)` returned `-1`, which violates the `IComparable<T>` contract that every instance
  compares greater than `null`. It now returns a positive value, so sorting a collection that contains
  nulls places them first. Collections sorted through `Comparer<T>.Default`, such as `OrderBy`, were
  never affected because that comparer handles nulls before delegating.

### Migration

- Replace calls to `ContainsBy` with `ContainedBy`.
- If you relied on `Contains(IPAddress)` returning `false` for a host prefix holding that address,
  compare the prefixes directly instead.
- If you relied on `Split` throwing for a target shorter than the network, that call now throws a
  different way round: it throws at enumeration rather than yielding wrong prefixes.

## [1.1.0] - 2024-09-16

### Changed

- The `Prefix` property was renamed to `Length`.

## [1.0.0] - 2024-06-28

- Initial release.

[3.0.0]: https://github.com/Bitvantage/InternetProtocol.NetworkPrefix/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/Bitvantage/InternetProtocol.NetworkPrefix/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/Bitvantage/InternetProtocol.NetworkPrefix/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Bitvantage/InternetProtocol.NetworkPrefix/releases/tag/v1.0.0
