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

///Parses the path to find a tree node in the Tree, expanding nodes as needed. Path has form:
///root/child1/child2/child3 where the root and each child is a text starts with match
let findTreeNode (tree:TreeView) (path:string) =
    let parts = path.Split('/') |> Seq.toList

    let find (nodes:TreeNodeCollection) p = nodes |> Seq.cast<TreeNode> |> Seq.find (fun x -> x.Text.StartsWith(p))

    let rec loop tn parts =
        match parts with
        | [] -> tn
        | p::parts' when tn = null ->
            let tn' = find tree.Nodes p
            loop tn' parts'
        | p::parts' ->
            tree?OnAfterExpand(new TreeViewEventArgs(tn))    
            let tn' = find tn.Nodes p
            loop tn' parts'
            
    loop null parts
        

[<Fact>]
let ``non virtual method SendTo label value`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/ToArray"
    test <@ tree?calcNodeLabel(tn) = "watch.ToArray()" @>