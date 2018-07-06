namespace FsPublicSuffix

open System
open System.Net
open System.Text

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

module Parser =

    type Domain = 
        { Registrable    : string
          TopLevelDomain : string
          Domain         : string
          SubDomain      : string option }
    
    type DomainType =
        | ValidDomain of Domain
        | InvalidDomain
