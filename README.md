# FsPublicSuffix [![Build status](https://ci.appveyor.com/api/projects/status/github/JohnTheGr8/FsPublicSuffix?svg=true)](https://ci.appveyor.com/project/JohnTheGr8/FsPublicSuffix) [![NuGet Badge](https://buildstats.info/nuget/FsPublicSuffix)](https://www.nuget.org/packages/FsPublicSuffix/)

Parse and separate hostnames accurately, using the [Public Suffix List](https://publicsuffix.org/).

## Usage

Here's some sample code to demonstrate how to parse domains and extract their individual parts.

First, load the assembly (F# Interactive):

```fsharp
> #load "Library.fs";;
> open FsPublicSuffix.Parser;;
```

The `Domain.Parse` method returns a value of type `ParsedDomain` which can be a `ValidDomain` of `Domain` (which contains all extracted hostname parts):

```fsharp
> Domain.Parse "www.google.co.uk";;
val it : ParsedDomain = ValidDomain {Registrable = "google.co.uk";
                                     TopLevelDomain = "co.uk";
                                     Domain = "google";
                                     SubDomain = Some "www";}
```

or returns an `InvalidDomain` :

```fsharp
> Domain.Parse ".google.co.uk";;
val it : ParsedDomain = InvalidDomain
```

The `Domain.TryParse` method is similar but returns a `Domain option` value. 

```fsharp
Domain.TryParse "uk.com" // None

let domain = Domain.TryParse "a.b.example.example" // Some { ... }

domain.Value.Hostname // "a.b.example.example"
```

---

## Builds

[![Build History](https://buildstats.info/appveyor/chart/JohnTheGr8/FsPublicSuffix)](https://ci.appveyor.com/project/JohnTheGr8/FsPublicSuffix)                    
