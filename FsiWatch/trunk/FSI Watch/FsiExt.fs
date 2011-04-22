[<AutoOpen>]
module Swensen.Watch.FsiExt

open Microsoft.FSharp.Compiler.Interactive
open System.Reflection

///adapted from: http://stackoverflow.com/questions/4997028/f-interactive-how-to-see-all-the-variables-defined-in-current-session/4998232#4998232
let private getFsiVariables() =
    let types = Assembly.GetExecutingAssembly().GetTypes()
    seq{for t in types |> Seq.sortBy (fun t -> t.Name) do
            if t.Name.StartsWith("FSI_") then 
                let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
                for m in t.GetProperties(flags) do 
                    if not (m.Name = "it"|| m.Name.Contains("@")) then
                        yield m.Name, lazy m.GetValue(null, Array.empty)}

type InteractiveSession with
    member this.getVariables() = 
        getFsiVariables() 
        |> Seq.map (fun (name, lval) -> name, lval.Value)
        |> dict

    member this.getNamedVariables() = 
        getFsiVariables() 
        |> Seq.filter (fun (name, _) -> not (name = "it"|| name.Contains("@")))
        |> Seq.map (fun (name, lval) -> name, lval.Value)
        |> dict