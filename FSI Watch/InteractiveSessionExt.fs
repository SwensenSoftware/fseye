[<AutoOpen>]
module Swensen.Watch.Fsi.InteractiveSessionExt

open Microsoft.FSharp.Compiler.Interactive
open System.Reflection

///adapted from: http://stackoverflow.com/questions/4997028/f-interactive-how-to-see-all-the-variables-defined-in-current-session/4998232#4998232
let private getFsiVariables() =
    let types = Assembly.GetExecutingAssembly().GetTypes()
    seq{for t in types |> Seq.sortBy (fun t -> t.Name) do
            if t.Name.StartsWith("FSI_") then 
                let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
                for pi in t.GetProperties(flags) do 
                    if pi.GetIndexParameters().Length > 0 |> not then
                        yield pi.Name, lazy pi.GetValue(null, Array.empty), pi.PropertyType}

type InteractiveSession with
    member this.GetVariables() = 
        getFsiVariables() 
        |> Seq.map (fun (name, lval, ty) -> name, lval.Value, ty)

    member this.GetNamedVariables() = 
        getFsiVariables() 
        |> Seq.filter (fun (name,_,_) -> (name = "it"|| name.Contains("@")) |> not)
        |> Seq.map (fun (name, lval, ty) -> name, lval.Value, ty)