namespace Swensen.Utils

module Control =
    let inline update< ^a when ^a :(member BeginUpdate: unit -> unit) and ^a : (member EndUpdate: unit -> unit)> (this: ^a) f =
        (^a : (member BeginUpdate: unit -> unit) (this))
        f()
        (^a : (member EndUpdate: unit -> unit) (this))