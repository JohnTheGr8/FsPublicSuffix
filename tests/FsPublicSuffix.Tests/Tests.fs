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
        Expect.isOk (isMatch rule.Value domain) "Domain should match the rule"

      rule "com" =*= "foo.com"
      rule "*.jp" =*= "foo.bar.jp"
      rule "*.jp" =*= "bar.jp"
      
      rule "公司.cn" =*= "xn--55qx5d.cn"
      rule "公司.cn" =*= "xn--85x722f.xn--55qx5d.cn"
      rule "公司.cn" =*= "公司.cn"
  ]

[<Tests>]
let RegistrableTests =
  testList "Registrable Domains" [
    // test with the Test Data from https://raw.githubusercontent.com/publicsuffix/list/master/tests/test_psl.txt
    testCase "Test Ddata" <| fun _ ->
      
      let checkPublicSuffix = Parser.getRegistrablePart

      let (===) actual expected =
        Expect.equal actual expected "Registrable domain doesn't match"

      // null input.
      checkPublicSuffix "" === None

      // Mixed case.
      checkPublicSuffix "COM" === None
      checkPublicSuffix "example.COM" === Some "example.com"
      checkPublicSuffix "WwW.example.COM" === Some "example.com"

      // Leading dot.
      checkPublicSuffix ".com" === None
      checkPublicSuffix ".example" === None
      checkPublicSuffix ".example.com" === None
      checkPublicSuffix ".example.example" === None

      // Unlisted TLD.
      checkPublicSuffix "example" === None
      checkPublicSuffix "example.example" === Some "example.example"
      checkPublicSuffix "b.example.example" === Some "example.example"
      checkPublicSuffix "a.b.example.example" === Some "example.example"

      // TLD with only 1 rule.
      checkPublicSuffix "biz" === None
      checkPublicSuffix "domain.biz" === Some "domain.biz"
      checkPublicSuffix "b.domain.biz" === Some "domain.biz"
      checkPublicSuffix "a.b.domain.biz" === Some "domain.biz"

      // TLD with some 2-level rules.
      checkPublicSuffix "com" === None
      checkPublicSuffix "example.com" === Some "example.com"
      checkPublicSuffix "b.example.com" === Some "example.com"
      checkPublicSuffix "a.b.example.com" === Some "example.com"
      checkPublicSuffix "uk.com" === None
      checkPublicSuffix "example.uk.com" === Some "example.uk.com"
      checkPublicSuffix "b.example.uk.com" === Some "example.uk.com"
      checkPublicSuffix "a.b.example.uk.com" === Some "example.uk.com"
      checkPublicSuffix "test.ac" === Some "test.ac"

      // TLD with only 1 (wildcard) rule.
      checkPublicSuffix "mm" === None
      checkPublicSuffix "c.mm" === None
      checkPublicSuffix "b.c.mm" === Some "b.c.mm"
      checkPublicSuffix "a.b.c.mm" === Some "b.c.mm"

      // More complex TLD.
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

      // TLD with a wildcard rule and exceptions.
      checkPublicSuffix "ck" === None
      checkPublicSuffix "test.ck" === None
      checkPublicSuffix "b.test.ck" === Some "b.test.ck"
      checkPublicSuffix "a.b.test.ck" === Some "b.test.ck"
      checkPublicSuffix "www.ck" === Some "www.ck"
      checkPublicSuffix "www.www.ck" === Some "www.ck"

      // US K12.
      checkPublicSuffix "us" === None
      checkPublicSuffix "test.us" === Some "test.us"
      checkPublicSuffix "www.test.us" === Some "test.us"
      checkPublicSuffix "ak.us" === None
      checkPublicSuffix "test.ak.us" === Some "test.ak.us"
      checkPublicSuffix "www.test.ak.us" === Some "test.ak.us"
      checkPublicSuffix "k12.ak.us" === None
      checkPublicSuffix "test.k12.ak.us" === Some "test.k12.ak.us"
      checkPublicSuffix "www.test.k12.ak.us" === Some "test.k12.ak.us"

      // IDN labels.
      checkPublicSuffix "食狮.com.cn" === Some "食狮.com.cn"
      checkPublicSuffix "食狮.公司.cn" === Some "食狮.公司.cn"
      checkPublicSuffix "www.食狮.公司.cn" === Some "食狮.公司.cn"
      checkPublicSuffix "shishi.公司.cn" === Some "shishi.公司.cn"
      checkPublicSuffix "公司.cn" === None
      checkPublicSuffix "食狮.中国" === Some "食狮.中国"
      checkPublicSuffix "www.食狮.中国" === Some "食狮.中国"
      checkPublicSuffix "shishi.中国" === Some "shishi.中国"
      checkPublicSuffix "中国" === None

      // Same as above, but punycoded.
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
