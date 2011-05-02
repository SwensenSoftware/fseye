[<AutoOpen>]
module Swensen.Watch.Fsi.InteractiveSessionExt

open Microsoft.FSharp.Compiler.Interactive
open System.Reflection

///adapted from: http://stackoverflow.com/questions/4997028/f-interactive-how-to-see-all-the-variables-defined-in-current-session/4998232#4998232
let getFsiVariables() =
    let types = Assembly.GetExecutingAssembly().GetTypes()
    [|
        for t in types |> Seq.sortBy (fun t -> t.Name) do
            if t.Name.StartsWith("FSI_") then 
                let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
                for pi in t.GetProperties(flags) do //i checked, nothing interesting in GetFields
                    if pi.GetIndexParameters().Length > 0 |> not then
                        yield pi.Name, lazy pi.GetValue(null, Array.empty), pi.PropertyType
    |]

type InteractiveSession with
    ///get all non-lambda variables, including the most recently updated "it"
    member this.GetVariables() = 
        [|
            let fsiVars = getFsiVariables()
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