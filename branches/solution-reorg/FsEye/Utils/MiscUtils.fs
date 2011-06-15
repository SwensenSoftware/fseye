[<AutoOpen>]
module Swensen.Utils.MiscUtils

///Reference comparison
let inline (=&) a b = obj.ReferenceEquals(a,b)

///Not reference equals
let inline (<>&) a b = a =& b |> not
