module Swensen.Watch.Model
open System
open System.Reflection

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

type dataMemberInfo = 
    | Field of FieldInfo
    | Property of PropertyInfo

///Represents a field or property member of a Value. Member Type is not null.
type DataMember(ownerValue: obj, dmi: dataMemberInfo) as this =
    inherit AbstractNode()
    let name, value, text, ty, isPublic =
        match dmi with
        | Field(fi) -> 
            let name = fi.Name
            let value =
                try 
                    fi.GetValue(ownerValue)
                with e ->
                    e :> obj
            let text = sprintf "%s (F): %A" name value
            name, value, text, fi.FieldType, fi.IsPublic
        | Property(pi) ->
            let name = pi.Name
            let value =
                try
                    pi.GetValue(ownerValue, Array.empty)
                with e ->
                    e :> obj
            let text = sprintf "%s (P): %A" name value
            name, value, text, pi.PropertyType, pi.GetGetMethod(true).IsPublic

    let children = lazy(seq {
        yield! SeqElement.YieldSeqElementsRootOrEmptyIfNone(name, value)
        yield! DataMember.GetDataMembers(value) } |> Seq.cache)

    override __.Text = text
    override __.Name = name
    override __.Children = children
    member __.Value = value
    member __.Type = ty
    member __.IsPublic = isPublic
        
    ///Get all data members for the given owner value
    static member GetDataMembers(ownerValue:obj) =
        if obj.ReferenceEquals(ownerValue, null) then Seq.empty
        else
            let props = seq {
                let propInfos = 
                    ownerValue.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                for pi in propInfos do
                    if pi.GetIndexParameters() = Array.empty then //non-indexed property
                        yield DataMember(ownerValue,Property(pi)) }
            
            let fields = seq {
                let fieldInfos = 
                    ownerValue.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                for fi in fieldInfos do
                    yield DataMember(ownerValue,Field(fi)) }

            let publicDataMembers, nonPublicDataMembers = 
                Seq.append props fields
                |> Seq.sortBy (fun dm -> dm.Text.ToLower())
                |> Seq.toArray
                |> Array.partition (fun dm -> dm.IsPublic)

            seq {
                if nonPublicDataMembers.Length > 0 then
                    let children = lazy(nonPublicDataMembers |> Seq.cast<INode>)
                        
                    yield { new INode with
                        member __.Text = "Non-public"
                        member __.Name = "Non-public" //does it really even need to be unique?, I'm strating to think most of the time not (except for root Watch node)
                        member __.Children = children }

                    yield! publicDataMembers |> Seq.cast<INode>
            }

and SeqElement(ownerName:string, index:int, value:obj) = 
    inherit AbstractNode()
    let name = sprintf "%s@Results[%i]" ownerName index
    let text = sprintf "[%i]: %A" index value
    let children = lazy(seq {
        yield! SeqElement.YieldSeqElementsRootOrEmptyIfNone(name, value)
        yield! DataMember.GetDataMembers(value) } |> Seq.cache)

    member __.Index = index
    member __.Value = value
    override __.Name = name
    override __.Text = text
    override __.Children = children

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

and Watch(name:string, value:obj) = 
    inherit AbstractNode()
    let text = sprintf "%s: %A" name value
    let children = lazy(seq {
        yield! SeqElement.YieldSeqElementsRootOrEmptyIfNone(name, value)
        yield! DataMember.GetDataMembers(value) } |> Seq.cache)

    override __.Text = text
    override __.Name = name
    override __.Children = children
    member __.Value = value

//type Archive(count:int, watches: Watch[]) =
//    inherit AbstractNode()
//    let name = sprintf "%i@Archive" count
//    let text = sprintf "Archive (%i)" count
//    let children = lazy(watches |> Seq.cast<INode>) //give me co/contravariant generics!
//    
//    override __.Text = text
//    override __.Name = name
//    override __.Children = children

//type nodeData = {Name:string, Text, }

//type watch =
//    | Archive of (int, watch list)
//    | Watch of (string, obj)
//    | Result of (string, 