module WatchModelTests

open Swensen.Unquote
open Xunit

open Swensen.FsEye.WatchModel

open Swensen.Utils

///Create a root watch, fast
let watch x = createRootWatch "x" x null
let findChildByName memberInfoName (w:Watch) = 
    w.Children 
    |> Seq.find (fun x -> x.MemberInfo |> function Some(mi) -> mi.Name = memberInfoName | _ -> false)

let tryFindChildByName memberInfoName (w:Watch) = 
    w.Children 
    |> Seq.tryFind (fun x -> x.MemberInfo |> function Some(mi) -> mi.Name = memberInfoName | _ -> false)

[<Fact>]
let ``Issue 13 Fix: GetEnumerator may be called multiple times without exhaustion bug fix`` () =
    let w = watch {1..100} |> findChildByName "GetEnumerator"
    test <@ w.Children |> Seq.length = 100 @>
    test <@ w.Children |> Seq.isEmpty |> not @>

//[<Fact>]
//let ``sequence with less than 100 elements has no Rest sub node`` () =
//    let w = watch {1..100}
//    let ws =
//        w.Children
//        |> Seq.find (fun x -> x.DefaultText.StartsWith "GetEnumerator")
//
//    test <@ ws.Children |> Seq.length = 100 @>
//    test <@ ws.Children |> Seq.length = 100 @>
//    test <@ (ws.Children |> Seq.nth 99).DefaultText <> "Rest" @>


    
