# FsPublicSuffix [![Build status](https://ci.appveyor.com/api/projects/status/github/JohnTheGr8/FsPublicSuffix?svg=true)](https://ci.appveyor.com/project/JohnTheGr8/FsPublicSuffix) [![NuGet Badge](https://buildstats.info/nuget/FsPublicSuffix)](https://www.nuget.org/packages/FsPublicSuffix/)

Parse and separate **Fully Qualified Domain Names** accurately, using the [Public Suffix List](https://publicsuffix.org/).

## Usage

The interesting part of this library is the `FullyQualifiedDomainName` record type and the two parsing methods available:

1. `FullyQualifiedDomainName.TryParse`: safely parse the input string into a FQDN (returns an `option`)
2. `FullyQualifiedDomainName.Parse` : unsafe version of `TryParse`, throws an exception if the input string cannot be parsed

Here's a simple F# Interactive session that demonstrates how you can use this library:

```fsharp
> #load "nuget: FsPublicSuffix"
> open FsPublicSuffix

> FullyQualifiedDomainName.TryParse "www.google.co.uk"
val it : FullyQualifiedDomainName option = Some { TopLevelDomain = "co.uk"
                                                  Domain = "google"
                                                  SubDomain = Some "www" }

```

You can also pass full URLs as input:

```fsharp
> let fqdn = FullyQualifiedDomainName.Parse "https://www.youtube.com/feed/subscriptions"
val fqdn : FullyQualifiedDomainName = { TopLevelDomain = "com"
                                        Domain = "youtube"
                                        SubDomain = Some "www" }
```

There are two other available members:

```fsharp
// get the FQDN of the parsed record
fqdn.FQDN // www.youtube.com

// get the registrable domain
fqdn.Registrable // youtube.com
```

Here's a few more quick examples:

```fsharp
// non-existent TLDs are not accepted
FullyQualifiedDomainName.TryParse "google.nope" // None

// domains that cannot be registered are not accepted
FullyQualifiedDomainName.TryParse "co.uk" // None
FullyQualifiedDomainName.TryParse "uk.com" // None

// IDN and punycode are also supported
FullyQualifiedDomainName.TryParse "ουτοπία.δπθ.gr" // Some { ... }
FullyQualifiedDomainName.TryParse "xn--pxaix.gr" // Some { ... }
```

---

## Builds

[![Build History](https://buildstats.info/appveyor/chart/JohnTheGr8/FsPublicSuffix)](https://ci.appveyor.com/project/JohnTheGr8/FsPublicSuffix)

[![Build History](https://buildstats.info/github/chart/JohnTheGr8/FsPublicSuffix)](https://github.com/JohnTheGr8/FsPublicSuffix/actions)
