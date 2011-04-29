module Swensen.Watch.Model
open System
open System.Reflection
open Swensen.Unquote

//how to add icons to tree view: http://msdn.microsoft.com/en-us/library/aa983725(v=vs.71).aspx

let private cleanString (str:string) = str.Replace("\n","").Replace("\r","").Replace("\t","")

type WatchNode(text, children, ?value, ?name) =
    let name =
        match name with
        | Some(name) -> name
        | None -> String.Empty

    let value = 
        match value with
        | Some(value) -> value
        | None -> null

    member __.Name = name
    member __.Text = text
    member __.Children = children
    member __.Value = value


///Create lazy seq of children nodes for a typical valued node
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
            let text = sprintf "Type"// ty.Name
            let children = createChildren ty (ty.GetType())
            yield WatchNode(text, children)
    }
///Results node, if value is IEnumerable
and createResultsNode value =
    seq {
        
        match value with
        | :? System.Collections.IEnumerable as value -> 
            let createChild index value =
                let ty = if obj.ReferenceEquals(value, null) then null else value.GetType()
                let text = sprintf "[%i] : %s = %A" index ty.FSharpName value |> cleanString
                let children = createChildren value ty
                WatchNode(text, children)
            
            //yield 100 node chunks
            let rec calcRest pos (ie:System.Collections.IEnumerator) = seq {
                if ie.MoveNext() then
                    let nextResult = createChild pos ie.Current
                    if pos % 100 = 0 && pos <> 0 then
                        let rest = seq { yield nextResult; yield! calcRest (pos+1) ie }
                        yield WatchNode("Rest", lazy(rest))
                    else
                        yield nextResult;
                        yield! calcRest (pos+1) ie
            }
            
            let children = lazy(seq {
                yield! calcRest 0 (value.GetEnumerator())
            } |> Seq.cache)
                
            yield WatchNode("Results", children)
        | _ -> ()
    }
//Create a nodes for fields and properites, sorted by name and sub-organized by access
and createDataMemberNodes ownerValue =
    if obj.ReferenceEquals(ownerValue, null) then Seq.empty
    else
        let publicFlags = BindingFlags.Instance ||| BindingFlags.Public
        let nonPublicFlags =BindingFlags.Instance ||| BindingFlags.NonPublic

        //returns count * WatchNode
        let props flags = 
            let propInfos = ownerValue.GetType().GetProperties(flags)
            propInfos.Length, seq {
                for pi in propInfos do
                    if pi.GetIndexParameters() = Array.empty then //non-indexed property
                        let name = pi.Name
                        let value =
                            try
                                pi.GetValue(ownerValue, Array.empty)
                            with e ->
                                e :> obj

                        let text = sprintf "(P) %s : %s = %A" name pi.PropertyType.FSharpName value |> cleanString
                        let children = createChildren value pi.PropertyType
                        yield WatchNode(text, children)
            }
          
        //returns count * WatchNode  
        let fields flags = 
            let fieldInfos = ownerValue.GetType().GetFields(flags)
            fieldInfos.Length, seq {
                for fi in fieldInfos do
                    let name = fi.Name
                    let value =
                        try 
                            fi.GetValue(ownerValue)
                        with e ->
                            e :> obj

                    let text = sprintf "(F) %s : %s = %A" name fi.FieldType.FSharpName value |> cleanString
                    let children = createChildren value fi.FieldType
                    yield WatchNode(text, children)
            }

        let getDataMembers flags =
            let propCount, propSeq = props flags
            let fieldCount, fieldSeq = fields flags

            let sortedDataMembers =
                Seq.append propSeq fieldSeq
                |> Seq.sortBy (fun node -> node.Text.ToLower())

            (propCount + fieldCount), sortedDataMembers

        let _, publicDataMembers = getDataMembers publicFlags
        let nonPublicDataMembersCount, nonPublicDataMembers =  getDataMembers nonPublicFlags

        seq {
            //optimization: check count instead of doing Seq.isEmpty |> not which forces
            //full evaluation due to Seq.sortBy
            if nonPublicDataMembersCount > 0 then 
                let children = lazy(nonPublicDataMembers)
                yield WatchNode("Non-public", children)
            yield! publicDataMembers
        }

///Create a watch root node
let createWatchNode (name:string) (value:obj) (ty:Type) = 
    let text = sprintf "%s : %s = %A" name ty.FSharpName value |> cleanString
    let children = createChildren value ty
    WatchNode(text, children, value=value, name=name)