module Tests


open Expecto
open FsPublicSuffix
open PublicSuffix

[<Tests>]
let tests =
  testList "samples" [

    testCase "Test Punycode Conversion" <| fun _ ->
      let (===) actual expected =
        Expect.equal actual expected "Punycode values should be equal"

      let punycode = Util.toAscii

      punycode "食狮.com.cn" === "xn--85x722f.com.cn"
      punycode "食狮.公司.cn" === "xn--85x722f.xn--55qx5d.cn"
      punycode "www.食狮.公司.cn" === "www.xn--85x722f.xn--55qx5d.cn"
      punycode "shishi.公司.cn" === "shishi.xn--55qx5d.cn"
      punycode "公司.cn" === "xn--55qx5d.cn"
      punycode "食狮.中国" === "xn--85x722f.xn--fiqs8s"
      punycode "www.食狮.中国" === "www.xn--85x722f.xn--fiqs8s"
      punycode "shishi.中国" === "shishi.xn--fiqs8s"
      punycode "中国" === "xn--fiqs8s"

    testCase "Test Rule Matches" <| fun _ ->

      let rule = RegistrationRule.Read

      let (=*=) (rule: RegistrationRule) domain =
        Expect.isSome (tryMatch domain rule) "Domain should match the rule"

      rule "com" =*= "foo.com"
      rule "*.jp" =*= "foo.bar.jp"
      rule "*.jp" =*= "bar.jp"

      rule "公司.cn" =*= "xn--55qx5d.cn"
      rule "公司.cn" =*= "xn--85x722f.xn--55qx5d.cn"
      rule "公司.cn" =*= "公司.cn"
  ]

[<Tests>]
let RegistrablePartTests =

  let checkPublicSuffix = Parser.getRegistrablePart

  let (===) actual expected =
    Expect.equal actual expected "Registrable domain doesn't match"

  // test with the Test Data from https://raw.githubusercontent.com/publicsuffix/list/master/tests/test_psl.txt
  testList "Registrable Domain Part" [

    testCase "null input" <| fun _ ->
      checkPublicSuffix "" === None

    testCase "Mixed case" <| fun _ ->
      checkPublicSuffix "COM" === None
      checkPublicSuffix "example.COM" === Some "example.com"
      checkPublicSuffix "WwW.example.COM" === Some "example.com"

    testCase "Leading dot" <| fun _ ->
      checkPublicSuffix ".com" === None
      checkPublicSuffix ".example" === None
      checkPublicSuffix ".example.com" === None
      checkPublicSuffix ".example.example" === None

    testCase "Unlisted TLD" <| fun _ ->
      checkPublicSuffix "example" === None
      checkPublicSuffix "example.example" === None
      checkPublicSuffix "b.example.example" === None
      checkPublicSuffix "a.b.example.example" === None

    testCase "Listed, but non-Internet, TLD." <| fun _ ->
      checkPublicSuffix "local" === None
      checkPublicSuffix "example.local" === None
      checkPublicSuffix "b.example.local" === None
      checkPublicSuffix "a.b.example.local" === None

    testCase "TLD with only 1 rule" <| fun _ ->
      checkPublicSuffix "biz" === None
      checkPublicSuffix "domain.biz" === Some "domain.biz"
      checkPublicSuffix "b.domain.biz" === Some "domain.biz"
      checkPublicSuffix "a.b.domain.biz" === Some "domain.biz"

    testCase "TLD with some 2-level rules" <| fun _ ->
      checkPublicSuffix "com" === None
      checkPublicSuffix "example.com" === Some "example.com"
      checkPublicSuffix "b.example.com" === Some "example.com"
      checkPublicSuffix "a.b.example.com" === Some "example.com"
      checkPublicSuffix "uk.com" === None
      checkPublicSuffix "example.uk.com" === Some "example.uk.com"
      checkPublicSuffix "b.example.uk.com" === Some "example.uk.com"
      checkPublicSuffix "a.b.example.uk.com" === Some "example.uk.com"
      checkPublicSuffix "test.ac" === Some "test.ac"

    testCase "TLD with only 1 (wildcard) rule" <| fun _ ->
      checkPublicSuffix "mm" === None
      checkPublicSuffix "c.mm" === None
      checkPublicSuffix "b.c.mm" === Some "b.c.mm"
      checkPublicSuffix "a.b.c.mm" === Some "b.c.mm"

    testCase "More complex TLD" <| fun _ ->
      checkPublicSuffix "jp" === None
      checkPublicSuffix "test.jp" === Some "test.jp"
      checkPublicSuffix "www.test.jp" === Some "test.jp"
      checkPublicSuffix "ac.jp" === None
      checkPublicSuffix "test.ac.jp" === Some "test.ac.jp"
      checkPublicSuffix "www.test.ac.jp" === Some "test.ac.jp"
      checkPublicSuffix "kyoto.jp" === None
      checkPublicSuffix "test.kyoto.jp" === Some "test.kyoto.jp"
      checkPublicSuffix "ide.kyoto.jp" === None
      checkPublicSuffix "b.ide.kyoto.jp" === Some "b.ide.kyoto.jp"
      checkPublicSuffix "a.b.ide.kyoto.jp" === Some "b.ide.kyoto.jp"
      checkPublicSuffix "c.kobe.jp" === None
      checkPublicSuffix "b.c.kobe.jp" === Some "b.c.kobe.jp"
      checkPublicSuffix "a.b.c.kobe.jp" === Some "b.c.kobe.jp"
      checkPublicSuffix "city.kobe.jp" === Some "city.kobe.jp"
      checkPublicSuffix "www.city.kobe.jp" === Some "city.kobe.jp"

    testCase "TLD with a wildcard rule and exceptions" <| fun _ ->
      checkPublicSuffix "ck" === None
      checkPublicSuffix "test.ck" === None
      checkPublicSuffix "b.test.ck" === Some "b.test.ck"
      checkPublicSuffix "a.b.test.ck" === Some "b.test.ck"
      checkPublicSuffix "www.ck" === Some "www.ck"
      checkPublicSuffix "www.www.ck" === Some "www.ck"

    testCase "US K12" <| fun _ ->
      checkPublicSuffix "us" === None
      checkPublicSuffix "test.us" === Some "test.us"
      checkPublicSuffix "www.test.us" === Some "test.us"
      checkPublicSuffix "ak.us" === None
      checkPublicSuffix "test.ak.us" === Some "test.ak.us"
      checkPublicSuffix "www.test.ak.us" === Some "test.ak.us"
      checkPublicSuffix "k12.ak.us" === None
      checkPublicSuffix "test.k12.ak.us" === Some "test.k12.ak.us"
      checkPublicSuffix "www.test.k12.ak.us" === Some "test.k12.ak.us"

    testCase "IDN labels" <| fun _ ->
      checkPublicSuffix "食狮.com.cn" === Some "食狮.com.cn"
      checkPublicSuffix "食狮.公司.cn" === Some "食狮.公司.cn"
      checkPublicSuffix "www.食狮.公司.cn" === Some "食狮.公司.cn"
      checkPublicSuffix "shishi.公司.cn" === Some "shishi.公司.cn"
      checkPublicSuffix "公司.cn" === None
      checkPublicSuffix "食狮.中国" === Some "食狮.中国"
      checkPublicSuffix "www.食狮.中国" === Some "食狮.中国"
      checkPublicSuffix "shishi.中国" === Some "shishi.中国"
      checkPublicSuffix "中国" === None

    testCase "Same as above, but punycoded" <| fun _ ->
      checkPublicSuffix "xn--85x722f.com.cn" === Some "xn--85x722f.com.cn"
      checkPublicSuffix "xn--85x722f.xn--55qx5d.cn" === Some "xn--85x722f.xn--55qx5d.cn"
      checkPublicSuffix "www.xn--85x722f.xn--55qx5d.cn" === Some "xn--85x722f.xn--55qx5d.cn"
      checkPublicSuffix "shishi.xn--55qx5d.cn" === Some "shishi.xn--55qx5d.cn"
      checkPublicSuffix "xn--55qx5d.cn" === None
      checkPublicSuffix "xn--85x722f.xn--fiqs8s" === Some "xn--85x722f.xn--fiqs8s"
      checkPublicSuffix "www.xn--85x722f.xn--fiqs8s" === Some "xn--85x722f.xn--fiqs8s"
      checkPublicSuffix "shishi.xn--fiqs8s" === Some "shishi.xn--fiqs8s"
      checkPublicSuffix "xn--fiqs8s" === None
  ]

open Parser

[<Tests>]
let HostNameTests =

  let hostname =
    Domain.TryParse >> Option.map (fun x -> x.Hostname)

  let (===) actual expected =
    Expect.equal actual expected "Hostname is invalid"

  testList "Parsing Host Name" [

    testCase "Mixed case" <| fun _ ->
      hostname "example.COM" === Some "example.com"
      hostname "WwW.Example.COM" === Some "www.example.com"

    testCase "Domains with subdomains" <| fun _ ->
      hostname "b.domain.biz" === Some "b.domain.biz"
      hostname "a.b.domain.biz" === Some "a.b.domain.biz"

    testCase "IDN and punycode" <| fun _ ->
      hostname "食狮.中国" === Some "食狮.中国"
      hostname "www.食狮.中国" === Some "www.食狮.中国"
      hostname "xn--85x722f.xn--fiqs8s" === Some "xn--85x722f.xn--fiqs8s"
      hostname "www.xn--85x722f.xn--fiqs8s" === Some "www.xn--85x722f.xn--fiqs8s"

  ]
