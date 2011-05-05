module Swensen.Watch.Model
open System
open System.Reflection
open Swensen.Unquote

//how to add icons to tree view: http://msdn.microsoft.com/en-us/library/aa983725(v=vs.71).aspx

let private cleanString (str:string) = str.Replace("\n","").Replace("\r","").Replace("\t","")
//let valuePrinter (value:obj) =
//    match obj with
//    | 

type RootInfo = { Text: string ; Children:seq<Watch> ; Value:obj ; Name: String }
and DataMemberInfo = { LoadingText:string ; AsyncInfo: Lazy<string * seq<Watch>>}
and GenericInfo = { Text: string ; Children:seq<Watch>}
and Watch =
    | Root of RootInfo
    | DataMember of  DataMemberInfo
    | Generic of GenericInfo
    with 
        member this.RootInfo =
            match this with
            | Root(info) -> info
            | _ -> failwith "Invalid Root match, Watch is actually: %A" this
        member this.DefaultText =
            match this with
            | Root(info) -> info.Text
            | DataMember(info) -> info.LoadingText
            | Generic(info) -> info.Text
        member this.Children =
            match this with
            | Root(info) -> info.Children
            | DataMember(info) -> info.AsyncInfo.Value |> snd
            | Generic(info) -> info.Children

///Create lazy seq of children s for a typical valued 
let rec createChildren (value:obj) (ty:Type) =
    seq {
        yield! createType ty
        yield! createResults value
        yield! createDataMembers value
    } //maybe |> Seq.cache
///Type , if type info exists
and createType ty = 
    seq {
        match ty with
        | null -> ()
        | _ -> 
            let tyty = ty.GetType()
            let text = sprintf "Type : %s = typeof<%s>" tyty.FSharpName ty.FSharpName
            let children = createChildren ty (ty.GetType())
            yield Generic({Text=text ; Children=children})
    }
///Results , if value is IEnumerable
and createResults value =
    seq {
        match value with
        | :? System.Collections.IEnumerable as value -> 
            let createChild index value =
                //Would like to be able to always get the type
                //but if is non-generic IEnumerable, then can't
                let ty = if obj.ReferenceEquals(value, null) then null else value.GetType()
                let text = sprintf "[%i] : %s = %A" index ty.FSharpName value |> cleanString
                let children = createChildren value ty
                Generic({Text=text ; Children=children})
            
            //yield 100  chunks
            let rec calcRest pos (ie:System.Collections.IEnumerator) = seq {
                if ie.MoveNext() then
                    let nextResult = createChild pos ie.Current
                    if pos % 100 = 0 && pos <> 0 then
                        let rest = seq { yield nextResult; yield! calcRest (pos+1) ie }
                        yield Generic({Text="Rest" ; Children=rest})
                    else
                        yield nextResult;
                        yield! calcRest (pos+1) ie
            }
            
            let children = seq {
                yield! calcRest 0 (value.GetEnumerator()) //should use "use" when getting enumerator?
            } // |> Seq.cache
                
            yield Generic({Text="IEnumerable" ; Children = children})
        | _ -> ()
    }
//Create a s for fields and properites, sorted by name and sub-organized by access
and createDataMembers ownerValue =
    if obj.ReferenceEquals(ownerValue, null) then Seq.empty
    else
        let publicFlags = BindingFlags.Instance ||| BindingFlags.Public
        let nonPublicFlags =BindingFlags.Instance ||| BindingFlags.NonPublic

        //returns count * Watch
        let props flags = 
            let propInfos = ownerValue.GetType().GetProperties(flags)
            propInfos.Length, 
            seq {
                for pi in propInfos do
                    if pi.GetIndexParameters() = Array.empty then //non-indexed property
                        let pretext = sprintf "(P) %s : %s = %s" pi.Name pi.PropertyType.FSharpName

                        let delayed = lazy(
                            let value, valueTy =
                                try
                                    pi.GetValue(ownerValue, Array.empty), pi.PropertyType
                                with e ->
                                    box e, e.GetType()

                            pretext (value |> string |> cleanString), createChildren value valueTy
                        )
                        yield pi.Name, DataMember({LoadingText=(pretext "Loading...") ; AsyncInfo=delayed})
            }
          
        //returns count * Watch  
        let fields flags = 
            let fieldInfos = ownerValue.GetType().GetFields(flags)
            fieldInfos.Length, 
            seq {
                for fi in fieldInfos do
                    let pretext = sprintf "(F) %s : %s = %s" fi.Name fi.FieldType.FSharpName

                    let delayed = lazy(
                        let value, valueTy = 
                            try 
                                fi.GetValue(ownerValue), fi.FieldType
                            with e ->
                                box e, e.GetType()

                        pretext (value |> string |> cleanString), createChildren value valueTy
                    )

                    yield fi.Name, DataMember({LoadingText=(pretext "Loading...") ; AsyncInfo=delayed})
            }

        let getDataMembers flags =
            let propCount, propSeq = props flags
            let fieldCount, fieldSeq = fields flags

            let sortedDataMembers =
                Seq.append propSeq fieldSeq
                |> Seq.sortBy (fun (name, _) -> name.ToLower())

            (propCount + fieldCount), sortedDataMembers

        let _, publicDataMembers = getDataMembers publicFlags
        let nonPublicDataMembersCount, nonPublicDataMembers =  getDataMembers nonPublicFlags

        seq {
            //optimization: check count instead of doing Seq.isEmpty |> not which forces
            //full evaluation due to Seq.sortBy
            if nonPublicDataMembersCount > 0 then 
                let children = nonPublicDataMembers |> Seq.map snd
                yield Generic({Text="Non-public" ; Children=children})
            yield! publicDataMembers |> Seq.map snd
        }

///Create a watch root 
let createRootWatch (name:string) (value:obj) (ty:Type) = 
    let text = sprintf "%s : %s = %A" name ty.FSharpName value |> cleanString
    let children = createChildren value ty
    Root({Text=text ; Children=children ; Value=value ; Name=name})