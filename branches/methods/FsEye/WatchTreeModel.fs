(*
Copyright 2011 Stephen Swensen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*)
module Swensen.FsEye.Model
open System
open System.Reflection
open Swensen.Unquote
open Microsoft.FSharp.Reflection
open Swensen.Utils

//how to add icons to tree view: http://msdn.microsoft.com/en-us/library/aa983725(v=vs.71).aspx

let private sprintValue (value:obj) (ty:Type) =
    let cleanString (str:string) = str.Replace("\n","").Replace("\r","").Replace("\t","")

    match value with
    | null when ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<option<_>> -> "None"
    | null -> "null"
    | _ -> 
        if typeof<System.Type>.IsAssignableFrom(ty) then
            sprintf "typeof<%s>" (value :?> Type).FSharpName
        else
            sprintf "%A" value |> cleanString

type RootInfo = { Text: string ; Children:seq<Watch> ; Value:obj ; Name: String }
and MemberInfo = { LoadingText:string ; AsyncInfo: Lazy<string * seq<Watch>>}
and CustomInfo = { Text: string ; Children:seq<Watch>}
and Watch =
    | Root of RootInfo
    | Member of  MemberInfo
    | Custom of CustomInfo
    with 
        member this.RootInfo =
            match this with
            | Root(info) -> info
            | _ -> failwith "Invalid Root match, Watch is actually: %A" this
        member this.DefaultText =
            match this with
            | Root(info) -> info.Text
            | Member(info) -> info.LoadingText
            | Custom(info) -> info.Text
        member this.Children =
            match this with
            | Root(info) -> info.Children
            | Member(info) -> info.AsyncInfo.Value |> snd
            | Custom(info) -> info.Children

///Create lazy seq of children s for a typical valued 
let rec createChildren (value:obj) (ty:Type) =
    seq {
        yield! createType ty
        yield! createResults value
        yield! createMembers value
    } //maybe |> Seq.cache
///Type , if type info exists
and createType ty = 
    seq {
        match ty with
        | null -> ()
        | _ -> 
            let tyty = ty.GetType()
            let text = sprintf "GetType() : %s = typeof<%s>" tyty.FSharpName ty.FSharpName
            let children = createChildren ty (ty.GetType())
            yield Custom({Text=text ; Children=children})
    }
///Results , if value is IEnumerable
and createResults value =
    seq {
        match value with
        | :? System.Collections.IEnumerable as value -> 
            let createChild index value =
                //Would like to be able to always get the type
                //but if is non-Custom IEnumerable, then can't
                let ty = if value =& null then null else value.GetType()
                let text = sprintf "[%i] : %s = %s" index ty.FSharpName (sprintValue value ty)
                let children = createChildren value ty
                Custom({Text=text ; Children=children})
            
            //yield 100  chunks
            let rec calcRest pos (ie:System.Collections.IEnumerator) = seq {
                if ie.MoveNext() then
                    let nextResult = createChild pos ie.Current
                    if pos % 100 = 0 && pos <> 0 then
                        let rest = seq { yield nextResult; yield! calcRest (pos+1) ie }
                        yield Custom({Text="Rest" ; Children=rest})
                    else
                        yield nextResult;
                        yield! calcRest (pos+1) ie
            }
            
            let children = seq {
                yield! calcRest 0 (value.GetEnumerator()) //should use "use" when getting enumerator?
            } // |> Seq.cache
                
            yield Custom({Text= sprintf "GetEnumerator() : IEnumerator" ; Children = children})
        | _ -> ()
    }
//Create a s for fields and properites, sorted by name and sub-organized by access
and createMembers ownerValue =
    if ownerValue =& null then Seq.empty
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

                            pretext (sprintValue value valueTy), createChildren value valueTy
                        )
                        yield pi.Name, Member({LoadingText=(pretext "Loading...") ; AsyncInfo=delayed})
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

                        pretext (sprintValue value valueTy), createChildren value valueTy
                    )

                    yield fi.Name, Member({LoadingText=(pretext "Loading...") ; AsyncInfo=delayed})
            }

        let getMembers flags =
            let propCount, propSeq = props flags
            let fieldCount, fieldSeq = fields flags

            let sortedMembers =
                Seq.append propSeq fieldSeq
                |> Seq.sortBy (fun (name, _) -> name.ToLower())

            (propCount + fieldCount), sortedMembers

        let _, publicMembers = getMembers publicFlags
        let nonPublicMembersCount, nonPublicMembers =  getMembers nonPublicFlags

        seq {
            //optimization: check count instead of doing Seq.isEmpty |> not which forces
            //full evaluation due to Seq.sortBy
            if nonPublicMembersCount > 0 then 
                let children = nonPublicMembers |> Seq.map snd
                yield Custom({Text="Non-public" ; Children=children})
            yield! publicMembers |> Seq.map snd
        }

///Create a watch root. If value is not null, then value.GetType() is used as the watch Type instead of
///ty. Else if ty is not null ty is used. Else typeof<obj> is used.
let createRootWatch (name:string) (value:obj) (ty:Type) = 
    let ty =
        if value <> null then value.GetType()
        elif ty <> null then ty
        else typeof<obj>

    let text = sprintf "%s : %s = %s" name ty.FSharpName (sprintValue value ty)
    let children = createChildren value ty
    Root({Text=text ; Children=children ; Value=value ; Name=name})