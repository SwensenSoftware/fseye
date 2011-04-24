module ExperimentalModels
open System

[<AbstractClass>]
type AbstractNode() =
    abstract Name : string
    abstract Text : string
    //abstract Children : seq<IWatchNode>

type ResultsElement(ownerName:string, index:int, value:obj) = 
    inherit AbstractNode()
    let name = sprintf "%s@Results[%i]" ownerName index
    let text = sprintf "[%i]: %A" index value

    member __.Index = index
    member __.Value = value
    override __.Name = name
    override __.Text = text

//should i flip this on it's head and make Results have a list of Option<Lazy<list<ResultsElement>>> ?
type Results(ownerName:string, elements:Lazy<list<ResultsElement>>) =
    inherit AbstractNode()
    let name = ownerName + "@Results"

    override __.Name = ownerName
    override __.Text = "Results"
    member __.Elements = elements

    static member TryCreate(ownerName:string, value:obj) =
        match value with
        | :? System.Collections.IEnumerable as value -> 
            //todo: chunck so take first 100 nodes or so, and then keep expanding "Rest" last node until exhausted
            Some(lazy(value 
            |> Seq.cast<obj>
            |> Seq.truncate 100
            |> Seq.mapi (fun i x -> ResultsElement(ownerName, i, x))
            |> Seq.toList))
        | _ -> 
            None

type Watch(name:string, value:obj) = 
    inherit AbstractNode()
    let text = sprintf "%s: %A" name value
    let results = Results.TryCreate(name, value)

    override __.Text = text
    override __.Name = name
    member __.Results = results

type Archive(count:int, watches:Watch list) =
    inherit AbstractNode()
    let name = sprintf "%i@Archive" count
    let text = sprintf "Archive (%i)" count
    
    override __.Text = text
    override __.Name = name

//type nodeData = {Name:string, Text, }

//type watch =
//    | Archive of (int, watch list)
//    | Watch of (string, obj)
//    | Result of (string, 