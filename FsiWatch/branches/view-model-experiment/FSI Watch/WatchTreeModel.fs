module Swensen.Watch.Model
open System
open System.Reflection

type IWatchNode =
    abstract Name : string
    abstract Text : string
    abstract Children : Lazy<seq<IWatchNode>>
    abstract Value : option<obj>

///Create lazy seq of children nodes
let rec createChildren (value:obj) (ty:Type) =
    lazy(seq {
        yield! createTypeNode ty
        yield! createResultsNode value
        yield! createDataMemberNodes value
    } |> Seq.cache)
///Type node, if type info exists
and createTypeNode ty = 
    seq {
        match ty with
        | null -> ()
        | _ -> 
            let text = sprintf "Type: %s" ty.Name
            let children = createChildren ty (ty.GetType())
            yield { new IWatchNode with
                    member __.Text = text
                    member __.Name = String.Empty
                    member __.Children = children 
                    member __.Value = Some(ty :> obj)}
    }
///Results node, if value is IEnumerable
and createResultsNode value =
    seq {
        match value with
        | :? System.Collections.IEnumerable as value -> 
            let createChild index value =
                let text = sprintf "[%i]: %A" index value
                let ty = if obj.ReferenceEquals(value, null) then null else value.GetType()
                let children = createChildren value ty

                { new IWatchNode with
                  member __.Name = String.Empty
                  member __.Text = text
                  member __.Children = children 
                  member __.Value = Some(value)}
            
            //todo: chunck so take first 100 nodes or so, and then keep expanding "Rest" last node until exhausted
            let children =
                lazy(value 
                |> Seq.cast<obj>
                |> Seq.truncate 100
                |> Seq.mapi createChild
                |> Seq.cache)
                
            yield { new IWatchNode with
                    member __.Name = String.Empty
                    member __.Text = "Results"
                    member __.Children = children 
                    member __.Value = None }
        | _ -> ()
    }
and createDataMemberNodes ownerValue =
    if obj.ReferenceEquals(ownerValue, null) then Seq.empty
    else
        let publicFlags = BindingFlags.Instance ||| BindingFlags.Public
        let nonPublicFlags =BindingFlags.Instance ||| BindingFlags.NonPublic

        let props flags = 
            seq {
                let propInfos = 
                    ownerValue.GetType().GetProperties(flags)

                for pi in propInfos do
                    if pi.GetIndexParameters() = Array.empty then //non-indexed property
                        let name = pi.Name
                        let value =
                            try
                                pi.GetValue(ownerValue, Array.empty)
                            with e ->
                                e :> obj

                        let text = sprintf "%s (P): %A" name value
                        let children = createChildren value pi.PropertyType
                        yield { new IWatchNode with
                                member __.Name = String.Empty
                                member __.Text = text
                                member __.Children = children 
                                member __.Value = Some(value) }
            }
            
        let fields flags = 
            seq {
                let fieldInfos = 
                    ownerValue.GetType().GetFields(flags)

                for fi in fieldInfos do
                    let name = fi.Name
                    let value =
                        try 
                            fi.GetValue(ownerValue)
                        with e ->
                            e :> obj

                    let text = sprintf "%s (F): %A" name value
                    let children = createChildren value fi.FieldType
                    yield { new IWatchNode with
                            member __.Name = String.Empty
                            member __.Text = text
                            member __.Children = children 
                            member __.Value = Some(value) }
            }

        let getDataMembers flags = 
            Seq.append (props flags) (fields flags)
            |> Seq.sortBy (fun node -> node.Text.ToLower())

        let publicDataMembers = getDataMembers publicFlags
        let nonPublicDataMembers =  getDataMembers nonPublicFlags

        seq {
            if nonPublicDataMembers |> Seq.isEmpty |> not then
                let children = lazy(nonPublicDataMembers)
                        
                yield { new IWatchNode with
                        member __.Text = "Non-public"
                        member __.Children = children 
                        member __.Name = String.Empty 
                        member __.Value = None }

            yield! publicDataMembers
        }


let createWatchNode (name:string) (value:obj) (ty:Type) = 
    let text = sprintf "%s: %A" name value
    let children = createChildren value ty
    { new IWatchNode with
      member __.Text = text
      member __.Name = name
      member __.Children = children 
      member __.Value = Some(value) }