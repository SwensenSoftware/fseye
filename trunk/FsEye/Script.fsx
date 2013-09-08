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

//we use Debug.FsEye since it has the plugins.
#r "../Debug.FsEye/bin/Release/FsEye.dll"
#load @"..\Debug.FsEye\bin\Release\FsEye.fsx"


//----NEW FEATURE IDEAS
//add "Refresh Watches" button to top left
//file menu?
//drag and drop root nodes to archives
//add Archive Watches... which will prompt for archive name
//right click context menu on archive node allows you to change archive name
//data visualization: simple graph, or plug into existing visualization tools
//multi-select nodes (for drag-drop, archiving, etc.)

//Simple example of how we can "break" during evaluation!
async {
    for i in 1..40 do
        eye.Watch("i", i)
        eye.Watch("i*2", i*2)
        eye.Archive()
        if i % 10 = 0 then
            do! eye.AsyncBreak()
} |> Async.StartImmediate

eye.AsyncContinue()
eye.Show()


let work loops = 
    for i in 1I..(loops*1000000I) do () 
    loops
    
type SlowType() =
    member this.AMethod() = work 2I ; new SlowType()
    member this.One = work 1I
    member this.Two = work 2I
    member this.Three = work 1I |> ignore ; failwith "Some exception occurred" ; 3
    member this.Four = work 1I
    member this.Four3 = work 1I
    member this.Four2 = work 2I
    member this.Four1 = work 4I
    member this.Fou5 = work 3I
    member private this.Fou23 = work 5I
    member this.Fou234 = work 6I
    member this.Fousd = work 1I
    member private this.Fous =work 3I
    member this.Foug = work 2I

eye.Watch("sl", SlowType())
eye.Show()


//also try to avoid display immediately seq's which are lazy (e.g. known concrete type, or perhaps having Item.[int] property lookup)

//features
//Monitors FSI for watch additions and updates
//Asycronous, parallel, lazy loading of child nodes
//Asyncronouse Break and Continue debugging
//View large or infinite sequences in 100 element lazy loaded chunks
//View Public and Non-public value members
//Programatic access to Gui commands and watch addition and updates through FSI
//Pretty F# name printing via Unquote