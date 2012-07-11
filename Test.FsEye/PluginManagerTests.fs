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

///Parses the path to find a tree node in the Tree, expanding nodes as needed. Path has form:
///root/child1/child2/child3 where t%he root and each child is a text starts with match
let findTreeNode (tree:TreeView) (path:string) =
    let parts = path.Split('/') |> Seq.toList

    let tryFind (nodes:TreeNodeCollection) p = nodes |> Seq.cast<TreeNode> |> Seq.tryFind (fun x -> x.Text.StartsWith(p))

    let rec loop tn parts =
        match parts with
        | [] -> tn
        | p::parts' when tn = null -> //the root
            match tryFind tree.Nodes p with
            | Some(tn') -> loop tn' parts'
            | None -> failwith "unexpected condition"
        | p::parts' ->
            //expand tn nodes in another thread so thread sleeping on this thread doesn't block loading work
            async {
                do! Async.SwitchToNewThread()
                tree?OnAfterExpand(new TreeViewEventArgs(tn))
            } |> Async.RunSynchronously

            let rec waitLoop = function
                | None ->
                    System.Threading.Thread.Sleep(120)
                    waitLoop (tryFind tn.Nodes p)
                | Some(tn') -> tn'
                    
            let tn' = waitLoop None
            loop tn' parts'
            
    loop null parts

[<Fact>]
let ``watch label: root watch`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", [1;2;3;4;5])
    let tn = tree.Nodes.[0]
    test <@ tree?calcNodeLabel(tn) = "watch" @>

[<Fact>]
let ``watch label: non virtual method`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/ToArray()"
    test <@ tree?calcNodeLabel(tn) = "watch.ToArray()" @>

[<Fact>]
let ``watch label: base class virtual method`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/obj.ToString()"
    test <@ tree?calcNodeLabel(tn) = "watch.ToString()" @>

[<Fact>]
let ``watch label: non virtual property`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/Count"
    test <@ tree?calcNodeLabel(tn) = "watch.Count" @>

[<Fact>]
let ``watch label: interface virtual property`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/IList.IsFixedSize"
    test <@ tree?calcNodeLabel(tn) = "(watch :> IList).IsFixedSize" @>

[<Fact>]
let ``watch label: non virtual method of interface virtual property`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/IList.IsFixedSize/GetHashCode()"
    test <@ tree?calcNodeLabel(tn) = "(watch :> IList).IsFixedSize.GetHashCode()" @>

[<Fact>]
let ``watch label: enumerator element`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    let tn = findTreeNode tree "watch/Non-public/obj.MemberwiseClone()"
    test <@ tree?calcNodeLabel(tn) = "watch?MemberwiseClone()" @>

[<Fact>]
let ``watch label: enumerator rest element`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..110}))
    let tn = findTreeNode tree "watch/GetEnumerator()/Rest/[103]"
    test <@ tree?calcNodeLabel(tn) = "watch.GetEnumerator().[103]" @>

[<Fact>]
let ``watch label: private method`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..110}))
    let tn = findTreeNode tree "watch/GetEnumerator()/Rest/[103]"
    test <@ tree?calcNodeLabel(tn) = "watch.GetEnumerator().[103]" @>

[<Fact>]
let ``watch label: archive`` () =
    let tree = new WatchTreeView()
    tree.Watch("watch", ResizeArray({1..5}))
    tree.Archive()
    let tn = findTreeNode tree "Archive (0)/watch"
    test <@ tree?calcNodeLabel(tn) = "[Archive (0)] watch" @>