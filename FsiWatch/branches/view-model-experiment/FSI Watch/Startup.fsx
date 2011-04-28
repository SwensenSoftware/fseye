#load "InteractiveSessionExt.fs"
#load "WatchTreeModel.fs"
#load "WatchTreeView.fs"
open Swensen.Watch.Forms
#load "WatchForm.fs"
open Swensen.Watch.Forms
open Swensen.Watch.Fsi
#load "FsiWatch.fs"
open Swensen.Watch.Fsi

//----testing todo, need to refactor out to model view
//lazy load children
//loads private and public instance properties and fields
//loads fields in alphebetic order, then properties in alphebetic order; case ignored
//loads enumerables
//does not load static properties


    

//Simple example of how we can "break" during evaluation!
async {
    for i in 1..100 do
        watch.Watch("i", i, typeof<int>)
        watch.Watch("i*2", i*2, typeof<int>)
        watch.Archive()
        if i % 10 = 0 then
            do! watch.Break()
} |> Async.StartImmediate