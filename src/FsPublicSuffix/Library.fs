namespace FsPublicSuffix

open System
open System.Globalization
open System.Net
open System.Text

[<AutoOpen>]
module Util =
    let splitLabels (domain: string) =
        domain.Split '.'

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

    let private loadRuleSet =
        let url = "https://publicsuffix.org/list/effective_tld_names.dat"
        let client = new WebClient ()
        client.Encoding <- Encoding.UTF8
        
        // download rule list
        let text = client.DownloadString(url)

        // Parse all valid rules
        text.Split '\n'
        |> Array.filter (String.IsNullOrEmpty >> not)
        |> Array.filter (fun x -> x.StartsWith("//") |> not)
        |> Array.map (fun x -> x.Trim())
    
    let RuleSet =
        loadRuleSet |> Array.map RegistrationRule.Read

    let isMatch (rule: string) (domain: string) =
        let domain = toAscii domain
        let ruleLabels   = (toAscii rule) |> splitLabels |> Array.rev
        let domainLabels = domain |> splitLabels |> Array.rev

        if domainLabels.Length < ruleLabels.Length
        then 
            Error "The domain must contain as many or more labels than the rule"
        else 
            let domainLabels = domainLabels |> Array.take ruleLabels.Length
                
            if Array.zip ruleLabels domainLabels
               |> Array.forall (fun (r,d) -> r = d || r = "*")
            then Ok rule
            else Error "For every pair of domain and rule label, either they are identical, or that the label from the rule is"

    let findMatches (domain: string) =
        RuleSet
        |> Array.filter (fun rule ->
            match isMatch rule.Value domain with
            | Ok _ -> true
            | _ -> false )

module Parser =

    type Domain = 
        { Registrable    : string
          TopLevelDomain : string
          Domain         : string
          SubDomain      : string option }
    
    type DomainType =
        | ValidDomain of Domain
        | InvalidDomain
