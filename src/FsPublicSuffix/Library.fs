namespace FsPublicSuffix

module Say =
    let nothing name =
        name |> ignore

    let hello name =
        sprintf "Hello %s" name

