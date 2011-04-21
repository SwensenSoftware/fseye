[<AutoOpen>]
module Watch

#load "FsiExt.fs"
#load "WatchTreeView.fs"
#load "WatchForm.fs"
open Swensen.FsiWatch.Forms

//implement as a ref, since we want to go ahead and show it in the watch (perhaps make configurable),
//but we don't want to load all child nodes since intense and folks usually won't care about this object.
//update: doesn't seem to load in interactive vars when loaded from script
let watch = ref (new WatchForm())

fsi.AddPrintTransformer(
    //need to figure out a way to not call repeatedly for single evaluation
    fun (_:obj) ->
        try        
            if watch.contents.IsDisposed then
                watch.contents <- new WatchForm()

            if watch.contents.Visible |> not then
                watch.contents.Show()
                watch.contents.Activate()

            for KeyValue(key,value) in fsi.getNamedVariables() do
                watch.contents.AddOrUpdateWatch(key, value)

            null
        with e ->
            printfn "%A" (e.InnerException)
            null
)
