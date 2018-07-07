module Tests


open Expecto
open FsPublicSuffix

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

  ]
