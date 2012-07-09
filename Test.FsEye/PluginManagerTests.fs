module PluginManagerTests

open Swensen.Unquote
open Xunit

open Swensen.FsEye.WatchModel
open Swensen.FsEye.Forms
open Swensen.FsEye

open Swensen.Utils
open System.Windows.Forms

open System
open System.Reflection
open ImpromptuInterface.FSharp

//type MockWatchViewer() =
//    let mutable result = "", new obj(), typeof<MockWatchViewer>
//    interface IWatchViewer with
//        member __.Watch(x,y,z) =
//            result <- (x,y :> obj,z)
//        member __.Control = null
//    member __.Result = result
//
//let sendToResult (tn:TreeNode) =
//    let pm = tn.TreeView?pluginManager?Value
//    pm
//
//[<Fact(Skip="not done yet: trying to figure best way create mock plugin")>]
//let ``root watch SendTo label value`` () =
//    //arrange
//    let tc = new TabControl()
//    tc.TabPages.Add("mock","mock")
//    let pm = new PluginManager(tc)
//    let mp = {Plugin=Unchecked.defaultof<IPlugin>;PluginManager=pm}
//    let wv = new MockWatchViewer()
//    let mwv = {ID="mock"; ManagedPlugin=mp; WatchViewer=wv}
//
//    let tree = new WatchTreeView(Some(pm))
//    tree.Watch("watch", [1;2;3;4;5])
//    let tn = tree.Nodes.[0]
//
//    //act
//    pm.SendTo(
//
//    test <@ sendToLabel tn <> null @>

[<Fact>]
let ``root watch SendTo label value`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", [1;2;3;4;5])
    let tn = tree.Nodes.[0]

    test <@ tree?calcNodeLabel(tn) = "watch" @>