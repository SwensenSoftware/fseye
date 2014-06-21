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
module DataGridViewTests

open Swensen.Unquote
open Xunit

open Swensen.FsEye
open Swensen.FsEye.Plugins

open Swensen.Utils
open System.Windows.Forms

[<Fact>]
let ``seq is synthetic watchable type`` () =
    let p = new DataGridViewPlugin() :> IPlugin
    let xs = seq [1;2;3]
    test <@ p.IsWatchable(xs, xs.GetType()) @>

    let wv = p.CreateWatchViewer()
    wv.Watch("", xs, xs.GetType())
    let dgv = wv.Control.Controls |> Seq.cast<Control> |> Seq.find (fun c -> c :? DataGridView) :?> DataGridView
    let xsWrapper = dgv.DataSource
    test <@ xsWrapper :? System.Collections.Generic.List<obj> @>
    let xsWrapper = xsWrapper :?> System.Collections.Generic.List<obj>
    test <@ xsWrapper.Count = 3 @>
    test <@ xsWrapper.[0] :?> int = 1 @>
    test <@ xsWrapper.[1] :?> int = 2 @>
    test <@ xsWrapper.[2] :?> int = 3 @>

[<Fact>]
let ``ResizeArray is a natively watchable type`` () =
    let p = new DataGridViewPlugin() :> IPlugin
    let xl = new System.Collections.Generic.List<int>([1;2;3])
    test <@ p.IsWatchable(xl, xl.GetType()) @>

    let wv = p.CreateWatchViewer()
    wv.Watch("", xl, xl.GetType())
    let dgv = wv.Control.Controls |> Seq.cast<Control> |> Seq.find (fun c -> c :? DataGridView) :?> DataGridView
    test <@ obj.ReferenceEquals(xl, dgv.DataSource) @>