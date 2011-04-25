module Swensen.Watch.Model
open System
open System.Reflection

type IWatchNode =
    abstract Name : string
    abstract Text : string
    abstract Children : Lazy<seq<IWatchNode>>

[<AbstractClass>]
type AbstractWatchNode(value:obj, ty:Type) =
    let children = lazy(seq {
        yield! DataType.Yield(ty)
        yield! SeqElement.Yield(value)
        yield! DataMember.Yield(value) 
    } |> Seq.cache)

    abstract Name : string
    abstract Text : string
    abstract Children : Lazy<seq<IWatchNode>>
    abstract Value : obj
    abstract Type : System.Type
    
    default this.Name = String.Empty
    default this.Children = children
    default this.Type = ty
    default this.Value = value

    interface IWatchNode with
        member this.Name = this.Name
        member this.Text = this.Text
        member this.Children = this.Children

///ty must not be null
and DataType(ty:Type) =
    inherit AbstractWatchNode(ty, ty.GetType())
    let text = sprintf "Type: %s" ty.Name
    override __.Text = text

    static member Yield(ty) =
        seq {
            match ty with
            | null -> ()
            | _ -> yield DataType(ty) :> IWatchNode
        }

///Represents a field or property member of a Value. Member Type is not null.
and DataMember(value:obj, ty:Type, isPublic:bool, text:string) =
    inherit AbstractWatchNode(value, ty)

    override __.Text = text
    member __.IsPublic = isPublic
        
    ///Get all data members for the given owner value
    static member Yield(ownerValue:obj) =
        if obj.ReferenceEquals(ownerValue, null) then Seq.empty
        else
            let props = 
                seq {
                    let propInfos = 
                        ownerValue.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                    for pi in propInfos do
                        if pi.GetIndexParameters() = Array.empty then //non-indexed property
                            let name = pi.Name
                            let value =
                                try
                                    pi.GetValue(ownerValue, Array.empty)
                                with e ->
                                    e :> obj

                            let text = sprintf "%s (P): %A" name value
                            yield DataMember(value, pi.PropertyType, pi.GetGetMethod(true).IsPublic, text) 
                }
            
            let fields = 
                seq {
                    let fieldInfos = 
                        ownerValue.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                    for fi in fieldInfos do
                        let name = fi.Name
                        let value =
                            try 
                                fi.GetValue(ownerValue)
                            with e ->
                                e :> obj

                        let text = sprintf "%s (F): %A" name value
                        yield DataMember(value, fi.FieldType, fi.IsPublic, text)
                }

            let publicDataMembers, nonPublicDataMembers = 
                Seq.append props fields
                |> Seq.sortBy (fun dm -> dm.Text.ToLower())
                |> Seq.toArray
                |> Array.partition (fun dm -> dm.IsPublic)

            seq {
                if nonPublicDataMembers.Length > 0 then
                    let children = lazy(nonPublicDataMembers |> Seq.cast<IWatchNode>)
                        
                    yield { new IWatchNode with
                        member __.Text = "Non-public"
                        member __.Children = children 
                        member __.Name = String.Empty }

                yield! publicDataMembers |> Seq.cast<IWatchNode>
            }

and SeqElement(index:int, value:obj, ty:System.Type) =
    inherit AbstractWatchNode(value, ty)
    let text = sprintf "[%i]: %A" index value
    override __.Text = text

    ///return a seq which yields the Seq element root, or empty if None
    static member Yield(value:obj) =
        seq {
            match value with
            | :? System.Collections.IEnumerable as value -> 
                //todo: chunck so take first 100 nodes or so, and then keep expanding "Rest" last node until exhausted
                let results =
                    lazy(value 
                    |> Seq.cast<obj>
                    |> Seq.truncate 100
                    |> Seq.mapi (fun i x -> SeqElement(i, x, if obj.ReferenceEquals(x,null) then null else x.GetType()) :> IWatchNode)
                    |> Seq.cache)
                
                yield { new IWatchNode with
                    member this.Name = String.Empty
                    member this.Text = "Results"
                    member this.Children = results
                }
            | _ -> ()
        }

and Watch(name:string, value:obj, ty:Type) = 
    inherit AbstractWatchNode(value, ty)
    let text = sprintf "%s: %A" name value
    override __.Text = text
    override __.Name = name //needed