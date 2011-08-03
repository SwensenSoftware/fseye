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

///Get the last element of a sequence with 1 or more elements
let last xs = xs |> Seq.skip ((xs |> Seq.length) - 1) |> Seq.head

[<Fact>]
let ``Issue 13 Fix: GetEnumerator may be called multiple times without exhaustion bug fix`` () =
    let w = watch {1..100} |> findChildByName "GetEnumerator"
    test <@ w.Children |> Seq.length = 100 @>
    test <@ w.Children |> Seq.isEmpty |> not @>

[<Fact>]
let ``sequence with less than or equal to 100 elements has no Rest sub node`` () =
    let w = watch {1..100} |> findChildByName "GetEnumerator"

    test <@ w.Children |> Seq.length = 100 @>
    test <@ (w.Children |> last).DefaultText <> "Rest" @>

[<Fact>]
let ``sequence with 101 elements has Rest node in 101st position which itself contains the remaining last element node nodes`` () =
    let w = watch {1..101} |> findChildByName "GetEnumerator"

    test <@ w.Children |> Seq.length = 101 @>
    test <@ (w.Children |> last).DefaultText = "Rest" @>
    test <@ (w.Children |> last).Children |> Seq.length = 1 @>

[<Fact>]
let ``sequence with 110 elements has Rest node in 101st position which itself contains the remaining 10 element nodes`` () =
    let w = watch {1..110} |> findChildByName "GetEnumerator"

    test <@ w.Children |> Seq.length = 101 @>
    test <@ (w.Children |> last).DefaultText = "Rest" @>
    test <@ (w.Children |> last).Children |> Seq.length = 10 @>

[<Fact>]
let ``infinate sequences are handled lazily without problem`` () =
    let w = watch (Seq.initInfinite id) |> findChildByName "GetEnumerator"

    test <@ w.Children |> Seq.length = 101 @>
    test <@ (((w.Children |> last).Children |> last).Children |> last).DefaultText = "Rest" @>