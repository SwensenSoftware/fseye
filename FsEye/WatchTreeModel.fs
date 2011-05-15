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
type Root = { Text: string ; Children:seq<Watch> ; Value:obj ; Name: String }
and Custom = { Text: string ; Children:seq<Watch>}
and Member = { Text:string ; Lazy: Lazy<Custom>}
and Watch =
    | Root of Root
    | DataMember of  Member
    | CallMember of  Member
    | Custom of Custom
    with 
        member this.RootMatch =
            match this with
            | Root(info) -> info
            | _ -> failwith "Invalid Root match, Watch is actually: %A" this
        member this.Text =
            match this with
            | Root {Text=text} | DataMember {Text=text}
            | CallMember {Text=text} | Custom {Text=text} -> text
        member this.Children =
            match this with
            | Root {Children=children} | Custom {Children=children} -> children
            | CallMember {Lazy=l} | DataMember {Lazy=l} -> l.Value.Children

let private sprintValue (value:obj) (ty:Type) =
    if ty =& null then
        nullArg "ty cannot be null"

    let cleanString (str:string) = str.Replace("\n","").Replace("\r","").Replace("\t","")

    match value with
    | null when ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<option<_>> -> "None"
    | null -> "null"
    | _ -> 
        if typeof<System.Type>.IsAssignableFrom(ty) then
            sprintf "typeof<%s>" (value :?> Type).FSharpName
        else
            sprintf "%A" value |> cleanString

///Create lazy seq of children s for a typical valued 
let rec createChildren ownerValue (ownerTy:Type) =
    if ownerValue =& null then Seq.empty
    else
        let getMembers bindingFlags =
            let allMembers = seq {
                ///yield all ownerTy members
                yield! ownerTy.GetMembers(bindingFlags)
            
                let mapMembers tys = 
                    tys 
                    |> Seq.map (fun (ty:Type) -> ty.GetMembers(bindingFlags))
                    |> Seq.concat
                //yield all ownerTy interface members
                yield! ownerTy.GetInterfaces() |> mapMembers
                //yield all ownerTy base type (recursive) members
                yield! 
                    ownerTy 
                    |> Seq.unfold(fun ty -> 
                        let bty = ty.BaseType
                        if bty = null then None else Some(bty, bty))
                    |> mapMembers }

            let validMemberTypes =
                allMembers
                |> Seq.filter (fun mi ->
                    match mi with
                    | :? PropertyInfo as pi -> pi.GetIndexParameters() = Array.empty
                    | :? MethodInfo as meth -> 
                        meth.GetParameters() = Array.empty && 
                        meth.ReturnType <> typeof<System.Void> && 
                        meth.ReturnType <> typeof<unit> &&
                        meth.ContainsGenericParameters |> not &&
                        meth.Name.StartsWith("get_") |> not
                    | :? FieldInfo -> true
                    | _ -> false)

            let nonRedundantMembers =
                validMemberTypes
                |> Seq.distinctByResolve
                    (fun mi -> mi.Name.ToLower())
                    (fun mi1 mi2 -> 
                        let ty1, ty2 = mi1.DeclaringType, mi2.DeclaringType
                        if ty1.IsAssignableFrom(ty2) then -1
                        elif ty2.IsAssignableFrom(ty1) then 1
                        else 0)

            let sortedMembers =
                nonRedundantMembers
                |> Seq.sortBy (fun mi -> mi.Name.ToLower())

            sortedMembers

        let createResultWatches (value:System.Collections.IEnumerator) = 
            let createChild index value =
                //Would like to be able to always get the type
                //but if is non-Custom IEnumerable, then can't
                let ty = if value =& null then typeof<obj> else value.GetType()
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
                        yield! calcRest (pos+1) ie }
            
            seq { yield! calcRest 0 value } //should use "use" when getting enumerator?

        let getMemberName (mi:Reflection.MemberInfo) =
            if mi.ReflectedType <> ownerTy then
                mi.ReflectedType.FSharpName + "." + mi.Name
            else
                mi.Name

        let getPropertyWatch (pi:PropertyInfo) =
            let pretext = sprintf "(P) %s : %s = %s" (getMemberName pi) pi.PropertyType.FSharpName
            let delayed = lazy(
                let value, valueTy =
                    try
                        pi.GetValue(ownerValue, Array.empty), pi.PropertyType
                    with e ->
                        box e, e.GetType()
                if typeof<System.Collections.IEnumerator>.IsAssignableFrom(valueTy) then
                    { Custom.Text=pretext "seq [..]"; Children=(createResultWatches (value :?> System.Collections.IEnumerator)) }
                else
                    { Text=pretext (sprintValue value valueTy); Children=(createChildren value valueTy) })
            DataMember({Text=(pretext "Loading...") ; Lazy=delayed })

        let getFieldWatch (fi:FieldInfo) =
            let pretext = sprintf "(F) %s : %s = %s" (getMemberName fi) fi.FieldType.FSharpName
            let delayed = lazy(
                let value, valueTy = 
                    try 
                        fi.GetValue(ownerValue), fi.FieldType
                    with e ->
                        box e, e.GetType()
                if typeof<System.Collections.IEnumerator>.IsAssignableFrom(valueTy) then
                    { Custom.Text=pretext "seq [..]"; Children=(createResultWatches (value :?> System.Collections.IEnumerator)) }
                else
                    { Text=pretext (sprintValue value valueTy); Children=(createChildren value valueTy) })
            DataMember({Text=(pretext "Loading...") ; Lazy=delayed })

        let getMethodWatch (mi:MethodInfo) =
            let pretext = sprintf "(M) %s() : %s = %s" (getMemberName mi) mi.ReturnType.FSharpName
            let delayed = lazy(
                let value, valueTy =
                    try
                        mi.Invoke(ownerValue, Array.empty), mi.ReturnType
                    with e ->
                        box e, e.GetType()
                if typeof<System.Collections.IEnumerator>.IsAssignableFrom(valueTy) then
                    { Custom.Text=pretext "seq [..]"; Children=(createResultWatches (value :?> System.Collections.IEnumerator)) }
                else
                    { Text=pretext (sprintValue value valueTy); Children=(createChildren value valueTy) })
            CallMember({Text=(pretext "Loading...") ; Lazy=delayed })

        let getMemberWatches bindingFlags = seq {
            let members = getMembers bindingFlags
            for m in members do
                match m with
                | :? PropertyInfo as x -> yield getPropertyWatch x
                | :? FieldInfo as x -> yield getFieldWatch x
                | :? MethodInfo as x -> yield getMethodWatch x 
                | _ -> failwith "unexpected MemberInfo type: %A" m }

        let publicBindingFlags = BindingFlags.Instance ||| BindingFlags.Public
        let nonPublicBindingFlags = BindingFlags.Instance ||| BindingFlags.NonPublic

        seq {
            let nonPublicMemberWatches = getMemberWatches nonPublicBindingFlags
            yield Custom({Text="Non-public" ; Children=nonPublicMemberWatches})
            yield! getMemberWatches publicBindingFlags
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