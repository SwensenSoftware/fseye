namespace Swensen.Watch.Fsi
open Swensen.Watch.Forms

type FsiWatch(watchForm:WatchForm) = 
    //value indicates whether or not FSI session listening is turned on
    let mutable listen = true

    //the listener event handler
    let listener =
        //need to figure out a way to not call repeatedly for single evaluation
        fun (_:obj) ->
            if listen then
                //printfn "listen is true"
                try
                    if watchForm.Visible |> not then
                        watchForm.Show()
                        watchForm.Activate()

                    FsiHelper.getWatchableFsiVariables()
                    |> watchForm.Watch
                    //System.Threading.Timer(new TimerCallback(fun _ -> listen <- true), 0, 1000)
                    null
                with e ->
                    printfn "%A" (e.InnerException)
                    null
            else
                //printfn "listen is false"
                null

    member __.Watch(watches : seq<string * obj>) =
        watchForm.Watch(watches)

    member __.Watch(name, value) =
        watchForm.Watch(name, value)

    member __.AsyncBreak() =
        watchForm.AsyncBreak()

    member __.ClearAll() =
        watchForm.ClearAll()
    
    member __.ClearWatches() =
        watchForm.ClearWatches()
    
    member __.ClearArchives() =
        watchForm.ClearArchives()
    
    member __.Archive() =
        watchForm.Archive()

    ///Indicates whether or not FSI session listening is turned on
    member __.Listen 
        with get() = listen
        and set(value) = listen <- value

    member __.Listener = 
        listener

    member __.Show() =
        watchForm.Show()
        watchForm.Activate()

    member __.Hide() =
        watchForm.Hide()

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FsiWatch =
    let watch = new FsiWatch(new WatchForm())