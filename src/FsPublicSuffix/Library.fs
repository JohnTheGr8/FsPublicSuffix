namespace FsPublicSuffix

open System
open System.Globalization
open System.Net
open System.Text

[<AutoOpen>]
module Util =

    let splitLabels (domain: string) =
        domain.Split '.'

    let joinlabels : string array -> string =
        String.concat "."

    let countLabels =
        splitLabels >> Array.length

    let skipLabels count =
        Array.skip count >> joinlabels

    let skipEndLabels count =
        Array.rev >> Array.skip count >> Array.rev >> joinlabels

    let takeEndLabels count =
        Array.rev >> Array.take count >> Array.rev >> joinlabels

    let tryNormalizeFqdn (str: string) =
        let url =
            let fqdn = str.ToString()
            if fqdn.StartsWith "https://" || fqdn.StartsWith "http://" then
                fqdn
            else
                // add protocol at the start of domain so that Uri.TryCreate doesn't fail
                "https://" + fqdn

        match Uri.TryCreate (url, UriKind.RelativeOrAbsolute) with
        | true, uri -> Some uri.Host
        | false,  _ -> None

    let private idn = new IdnMapping()

    let toAscii domain =
        try
            idn.GetAscii domain
        with _ ->
            domain

module PublicSuffix =

    type RegistrationRule =
        | SimpleRule of string
        | Exception of string
        | Wildcard of string

        member x.Value =
            match x with
            | SimpleRule r -> r
            | Exception e -> e
            | Wildcard w -> w

        static member Read (rule: string) =
            match rule.ToCharArray() |> List.ofArray with
            | '!' :: _        -> Exception rule.[1..]
            | '*' :: '.' :: _ -> Wildcard rule
            | _               -> SimpleRule rule

    let RuleSet = lazy (
        let url = "https://publicsuffix.org/list/public_suffix_list.dat"
        let client = new WebClient ()
        client.Encoding <- Encoding.UTF8

        // download rule list
        let text = client.DownloadString(url)

        // Parse all valid rules
        text.Split '\n'
        |> Array.filter (String.IsNullOrEmpty >> not)        // skip empty lines
        |> Array.filter (fun x -> x.StartsWith("//") |> not) // skip comments
        |> Array.map (fun x -> x.Trim())
        |> Array.map RegistrationRule.Read
    )

    /// Try to match a domain to a given Public Suffix rule
    let tryMatch (domain: string) (rule: RegistrationRule) =
        let ruleLabels   = toAscii rule.Value |> splitLabels |> Array.rev
        let domainLabels = toAscii domain     |> splitLabels |> Array.rev

        if domainLabels.Length < ruleLabels.Length
        then
            // The domain must contain as many or more labels than the rule
            None
        else
            let domainLabels = domainLabels |> Array.take ruleLabels.Length

            // for every pair of domain and rule label, either they
            // are identical, or the label from the rule is "*"
            if Array.zip ruleLabels domainLabels
               |> Array.forall (fun (r,d) -> r = d || r = "*")
            then Some rule
            else None

    /// Find all rules that match a given domain
    let findMatches (domain: string) =
        RuleSet.Value
        |> Array.choose (tryMatch domain)

    /// Find the best matching rule for the given domain
    let findMatch (domain: string) =
        let matchingRules = findMatches domain

        matchingRules
        |> Array.tryFind (function Exception _ -> true | _ -> false)
        |> Option.defaultValue (
            matchingRules
            |> Array.sortByDescending (fun rule -> countLabels rule.Value)
            |> Array.tryHead
            |> Option.defaultValue (SimpleRule "*"))

module Parser =

    open PublicSuffix

    /// Try to parse the registrable part of a hostname
    let getRegistrablePart (domain: string) =

        let domainLabels = splitLabels domain

        let registrableLabels =
            match findMatch domain with
            | SimpleRule  "*" -> 0
            | SimpleRule rule -> countLabels rule + 1
            | Wildcard   rule -> countLabels rule + 1
            | Exception    ex -> countLabels ex

        if registrableLabels = 0 || registrableLabels > domainLabels.Length
        then None
        else Some (takeEndLabels registrableLabels domainLabels)

    let internal parseTld =
        splitLabels >> skipLabels 1

    /// Try to extract the Top Level Domain
    let getTld (domain: string) =
        getRegistrablePart domain
        |> Option.map parseTld

    let internal tryParseSubdomain (domain: string) (registrable: string) =
        let domainLabels = splitLabels domain
        let regLabels    = splitLabels registrable

        if domainLabels.Length > regLabels.Length
        then Some (skipEndLabels regLabels.Length domainLabels)
        else None

    /// Try to extract the Sub Domain
    let getSubdomain (domain: string) =
        getRegistrablePart domain
        |> Option.map (tryParseSubdomain domain)
        |> Option.flatten

    let internal parseDomain =
        splitLabels >> Array.head

    /// Try to extract the Domain
    let getDomain (domain: string) =
        getRegistrablePart domain
        |> Option.map parseDomain

    type Domain =
        { Registrable    : string
          TopLevelDomain : string
          Domain         : string
          SubDomain      : string option }

        static member TryParse (domain: string) =
            match tryNormalizeFqdn domain with
            | Some domain ->
                getRegistrablePart domain
                |> Option.map (fun registrable ->
                    { Registrable    = registrable
                      TopLevelDomain = parseTld registrable
                      Domain         = parseDomain registrable
                      SubDomain      = tryParseSubdomain domain registrable })
            | None ->
                None

        static member Parse (domain: string) : ParsedDomain =
            match Domain.TryParse domain with
            | Some x -> ValidDomain x
            | _ -> InvalidDomain

        member x.Hostname =
            match x.SubDomain with
            | Some sub -> sprintf "%s.%s.%s" sub x.Domain x.TopLevelDomain
            | None     -> sprintf "%s.%s" x.Domain x.TopLevelDomain

    and ParsedDomain =
        | ValidDomain of Domain
        | InvalidDomain
