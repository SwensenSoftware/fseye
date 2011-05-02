[<AutoOpen>]
module Swensen.Watch.Fsi.InteractiveSessionExt

open Microsoft.FSharp.Compiler.Interactive
open System.Reflection
open Swensen.Watch.Forms

module internal FsiHelper =
    ///adapted from: http://stackoverflow.com/questions/4997028/f-interactive-how-to-see-all-the-variables-defined-in-current-session/4998232#4998232
    let private getRawFsiVariables =
        let fsiAssembly = 
            System.AppDomain.CurrentDomain.GetAssemblies() 
            |> Array.find (fun assm -> assm.GetName().Name = "FSI-ASSEMBLY")

        fun () ->
            let types = fsiAssembly.GetTypes()
            [|
                for t in types |> Seq.sortBy (fun t -> t.Name) do
                    if t.Name.StartsWith("FSI_") then 
                        let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
                        for pi in t.GetProperties(flags) do //i checked, nothing interesting in GetFields
                            if pi.GetIndexParameters().Length > 0 |> not then
                                yield pi.Name, lazy pi.GetValue(null, Array.empty), pi.PropertyType
            |]

    ///get all non-lambda variables, including the most recently updated "it"
    let private getWatchableFsiVariables() =
        [|
            let fsiVars = getRawFsiVariables()
            //last "it" should be most recent, show it first
            let it =
                seq { for i in (fsiVars.Length-1)..(-1)..0 do yield fsiVars.[i] }
                |> Seq.tryFind (fun (name,_,__) -> name = "it")

            match it with
            | Some(name, lval, ty) -> yield name, lval.Value, ty
            | _ -> ()

            //show the rest of the non-"@" vars
            for name, lval, ty in fsiVars do
                if name <> "it" && (name.Contains("@") |> not) then
                    yield name, lval.Value, ty
        |]

    let private watch = new WatchForm()
    let mutable private watchListenerAdded = false
       
    let getWatch (fsi : InteractiveSession) =
        if watchListenerAdded |> not then
            fsi.AddPrintTransformer <|
                //need to figure out a way to not call repeatedly for single evaluation
                fun (_:obj) ->
                    try
                        if watch.Visible |> not then
                            watch.Show()
                            watch.Activate()

                        getWatchableFsiVariables()
                        |> watch.Watch

                        null
                    with e ->
                        printfn "%A" (e.InnerException)
                        null
            watchListenerAdded <- true
        watch

type InteractiveSession with
    member this.watch = FsiHelper.getWatch(this)