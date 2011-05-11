[<AutoOpen>]
module Swensen.MiscUtils

///Reference comparison
let inline (=&) a b = obj.ReferenceEquals(a,b)

///Not reference equals
let inline (<>&) a b = a =& b |> not


            
let xl = [None, 1; Some 1, 2; Some 0, 2;]

xl
|> distinctByResolve
    snd
    (fun x y -> compare (fst x) (fst y))