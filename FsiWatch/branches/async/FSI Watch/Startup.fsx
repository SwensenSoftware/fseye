#r @"C:\Users\Stephen\Documents\Visual Studio 2010\Projects\Unquote\builds\Unquote-1.3.0\Unquote.dll"
open Swensen.Unquote
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
    for i in 1..40 do
        watch.Watch("i", i)
        watch.Watch("i*2", i*2)
        watch.Archive()
        if i % 10 = 0 then
            System.Threading.Thread.Sleep(50)
            do! watch.AsyncBreak()
} |> Async.StartImmediate

watch.AsyncContinue()
watch.Show()

type SlowType() =
    member this.One = System.Threading.Thread.Sleep(1000) ; 1
    member this.Two = System.Threading.Thread.Sleep(2000) ; 2
    member this.Three = System.Threading.Thread.Sleep(3000) ; failwith "Some exception occurred" ; 3
    member this.Four = System.Threading.Thread.Sleep(4000) ; 4
    member this.Four3 = System.Threading.Thread.Sleep(6000) ; 4
    member this.Four2 = System.Threading.Thread.Sleep(7000) ; 4
    member this.Four1 = System.Threading.Thread.Sleep(8000) ; 4
    member this.Fou5 = System.Threading.Thread.Sleep(9000) ; 4
    member private this.Fou23 = System.Threading.Thread.Sleep(1000) ; 4
    member this.Fou234 = System.Threading.Thread.Sleep(12000) ; 4
    member this.Fousd = System.Threading.Thread.Sleep(32000) ; 4
    member private this.Fous = System.Threading.Thread.Sleep(4000) ; 4
    member this.Foug = System.Threading.Thread.Sleep(4000) ; 4

watch.Watch("test!", 23)