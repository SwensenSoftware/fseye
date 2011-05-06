#I __SOURCE_DIRECTORY__ //http://stackoverflow.com/questions/4860991/f-for-scripting-location-of-script-file

//should do dynamic action while building to toggle between this local path and the relative path
//#r "FsiWatch.dll"
#r "bin/Release/FsiWatch.dll"

open Swensen.Watch.Forms
open Swensen.Watch.Fsi
//initialize watch and attached fsi listener
let watchForm = new WatchForm()
let mutable listen = true

fsi.AddPrintTransformer <|
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

//only way can get at
type FsiWatch = 
    static member Watch(watches : seq<string * obj>) =
        watchForm.Watch(watches)

    static member Watch(name, value) =
        watchForm.Watch(name, value)

    static member AsyncBreak() =
        watchForm.AsyncBreak()

    static member ClearAll() =
        watchForm.ClearAll()
    
    static member ClearWatches() =
        watchForm.ClearWatches()
    
    static member ClearArchives() =
        watchForm.ClearArchives()
    
    static member Archive() =
        watchForm.Archive()

    static member Listen 
        with get() = listen
        and set(value) = listen <- value

    static member Show() =
        watchForm.Show()
        watchForm.Activate()

    static member Hide() =
        watchForm.Hide()