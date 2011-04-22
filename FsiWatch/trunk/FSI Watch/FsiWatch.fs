//dont auto open since my want to use other things.
module Swensen.Watch.Fsi.FsiWatch
//open Swensen.Watch
open Swensen.Watch.Forms

//throw exception if not interactive? and auto open?

//implement as a ref, since we want to go ahead and show it in the watch (perhaps make configurable),
//but we don't want to load all child nodes since intense and folks usually won't care about this object.
//update: doesn't seem to load in interactive vars when loaded from script
let mutable watch = new WatchForm()

fsi.AddPrintTransformer(
    //need to figure out a way to not call repeatedly for single evaluation
    fun (_:obj) ->
        try        
            if watch.IsDisposed then
                watch <- new WatchForm()

            if watch.Visible |> not then
                watch.Show()
                watch.Activate()

            for KeyValue(key,value) in fsi.GetNamedVariables() do
                watch.AddOrUpdateWatch(key, value)

            null
        with e ->
            printfn "%A" (e.InnerException)
            null
)