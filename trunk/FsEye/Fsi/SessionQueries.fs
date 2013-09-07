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
///Queries against the active FSI session
module internal Swensen.FsEye.Fsi.SessionQueries

open System.Reflection

//The following method for extracting FSI session variables using reflection was 
//adapted from Tomas Petricek's (http://stackoverflow.com/users/33518/tomas-petricek) answer at
//http://stackoverflow.com/questions/4997028/f-interactive-how-to-see-all-the-variables-defined-in-current-session/4998232#4998232
let getWatchableVariables =
    let fsiAssembly = 
        System.AppDomain.CurrentDomain.GetAssemblies() 
        |> Seq.find (fun assm -> assm.GetName().Name = "FSI-ASSEMBLY")

    fun () ->
        fsiAssembly.GetTypes()//FSI types have the name pattern FSI_####, where #### is the order in which they were created
        |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_"))
        |> Seq.sortBy (fun ty -> ty.Name.Split('_').[1] |> int)
        |> Seq.collect (fun ty ->
            let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
            ty.GetProperties(flags) 
            |> Seq.filter (fun pi -> pi.GetIndexParameters().Length > 0 |> not && pi.Name.Contains("@") |> not))
        //|> Seq.map (fun pi -> printfn "%A" (pi.Name, pi.GetValue(null, Array.empty), pi.PropertyType); pi)
        //the next sequence of pipes removes leading duplicates 
        |> Seq.mapi (fun i pi -> pi.Name, (i, pi)) //remember the order
        |> Map.ofSeq //remove leading duplicates (but now ordered by property name)
        |> Map.toSeq //reconstitue
        |> Seq.sortBy (fun (_,(i,_)) -> i) //order by original index
        |> Seq.map (fun (_,(_,pi)) -> pi.Name, pi.GetValue(null, Array.empty), pi.PropertyType) //discard ordering index, project usuable watch value
        //|> Seq.map (fun it -> printfn "%A" it; it)
