module ExperimentalModels
open System

type INode =
    abstract Name : string
    abstract Text : string
    abstract Children : Lazy<seq<INode>>

[<AbstractClass>]
type AbstractNode() =
    abstract Name : string
    abstract Text : string
    abstract Children : Lazy<seq<INode>>
    interface INode with
        member this.Name = this.Name
        member this.Text = this.Text
        member this.Children = this.Children


type SeqElement(ownerName:string, index:int, value:obj) = 
    inherit AbstractNode()
    let name = sprintf "%s@Results[%i]" ownerName index
    let text = sprintf "[%i]: %A" index value

    member __.Index = index
    member __.Value = value
    override __.Name = name
    override __.Text = text
    override __.Children = lazy(Seq.empty)

    ///if value is IEnumerable, then retrn Some INode node with SeqElement Children
    static member TryGetSeqElementsRoot(ownerName:string, value:obj) =
        match value with
        | :? System.Collections.IEnumerable as value -> 
            //todo: chunck so take first 100 nodes or so, and then keep expanding "Rest" last node until exhausted
            let results =
                lazy(value 
                |> Seq.cast<obj>
                |> Seq.truncate 100
                |> Seq.mapi (fun i x -> SeqElement(ownerName, i, x) :> INode)
                |> Seq.cache)

            let name = ownerName + "@Results"
            Some({ new INode with
                member this.Name = name
                member this.Text = "Results"
                member this.Children = results
            })
        | _ -> 
            None

    ///return a seq which yields the Seq element root, or empty if None
    static member YieldSeqElementsRootOrEmptyIfNone(ownerName:string, value:obj) =
        seq {
            match SeqElement.TryGetSeqElementsRoot(ownerName, value) with
                | Some(elementsNode) -> yield elementsNode
                | _ -> ()
        }

type Watch(name:string, value:obj) = 
    inherit AbstractNode()
    let text = sprintf "%s: %A" name value
    override __.Text = text
    override __.Name = name
    override __.Children =
        lazy(seq {
            //try create Results node
            yield! SeqElement.YieldSeqElementsRootOrEmptyIfNone(name, value)

        } |> Seq.cache)

type Archive(count:int, watches:Watch list) =
    inherit AbstractNode()
    let name = sprintf "%i@Archive" count
    let text = sprintf "Archive (%i)" count
    
    override __.Text = text
    override __.Name = name
    override __.Children = lazy(watches |> Seq.cast<INode>) //give me co/contravariant generics!

//type nodeData = {Name:string, Text, }

//type watch =
//    | Archive of (int, watch list)
//    | Watch of (string, obj)
//    | Result of (string, 