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
module internal Swensen.FsEye.FsiHelper

open System.Reflection

//The following method for extracting FSI session variables using reflection was 
//adapted from Tomas Petricek's (http://stackoverflow.com/users/33518/tomas-petricek) answer at
//http://stackoverflow.com/questions/4997028/f-interactive-how-to-see-all-the-variables-defined-in-current-session/4998232#4998232
let getRawFsiVariables =
    let fsiAssembly = 
        System.AppDomain.CurrentDomain.GetAssemblies() 
        |> Array.find (fun assm -> assm.GetName().Name = "FSI-ASSEMBLY")

    fun () ->
        [|
            let types = fsiAssembly.GetTypes()
            types |> Array.sortInPlaceBy (fun t -> t.Name)

            for t in types do
                if t.Name.StartsWith("FSI_") then 
                    let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
                    for pi in t.GetProperties(flags) do //i checked, nothing interesting in GetFields
                        if pi.GetIndexParameters().Length > 0 |> not then
                            yield pi.Name, lazy pi.GetValue(null, Array.empty), pi.PropertyType
        |]

///get all non-lambda variables, including the most recently updated "it"
let getWatchableFsiVariables() =
    [|
        let fsiVars = getRawFsiVariables()
        //last "it" should be most recent, show it first
        let it =
            seq { for i in (fsiVars.Length-1)..(-1)..0 do yield fsiVars.[i] }
            |> Seq.tryFind (fun (name,_,__) -> name = "it")

        match it with
        | Some(name, lval, ty) -> yield name, lval.Value, ty
        | _ -> ()

        //show the rest of the non-"@" vars
        for name, lval, ty in fsiVars do
            if name <> "it" && (name.Contains("@") |> not) then
                yield name, lval.Value, ty
    |]