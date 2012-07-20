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

let mkPlugin name mkControl =
    {
        new IPlugin with
            member __.Name = name
            member __.IsWatchable(_,_) = true
            member __.CreateWatchViewer() =
                { 
                    new IWatchViewer with
                        member __.Watch(_,_,_) = ()
                        member __.Control = mkControl()
                }
    }

let mkControl = fun () -> new Control()

[<Fact>]
let ``SendTo plugin creates watch viewers with absolute count ids`` () =
    let plugin = mkPlugin "Plugin" mkControl
    let pm = new PluginManager()
    let mp = pm.RegisterPlugin(plugin)
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 1" @>
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 2" @>
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 3" @>
    pm.RemoveManagedWatchViewer("Plugin 3")
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 4" @>

[<Fact>]
let ``removing a plugin removes its watch viewers`` () =
    let pluginA = mkPlugin "PluginA" mkControl
    let pluginB = mkPlugin "PluginB" mkControl

    let pm = new PluginManager()
    let mpA = pm.RegisterPlugin(pluginA)
    let mpB = pm.RegisterPlugin(pluginB)
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpB, "", null, typeof<obj>) |> ignore

    pm.RemoveManagedPlugin(mpB)

    test <@ pm.ManagedWatchViewers |> Seq.map (fun x -> x.ID) |> Seq.toList = ["PluginA 1"; "PluginA 2"]  @>

[<Fact>]
let ``registering a plugin of the same name removes the previous version of the plugin`` () =
    let pluginA = mkPlugin "PluginA" mkControl
    let pluginB = mkPlugin "PluginB" mkControl

    let pm = new PluginManager()
    let mpA = pm.RegisterPlugin(pluginA)
    let mpB = pm.RegisterPlugin(pluginB)
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpB, "", null, typeof<obj>) |> ignore

    let pluginB' = mkPlugin "PluginB" mkControl
    let mpB' = pm.RegisterPlugin(pluginB')

    ///i.e. all of the original PluginB watch viewers were removed when the new version of registered
    test <@ pm.ManagedWatchViewers |> Seq.map (fun x -> x.ID) |> Seq.toList = ["PluginA 1"; "PluginA 2"]  @>

    test <@ pm.ManagedPlugins |> Seq.map (fun x -> x.Plugin) |> Seq.toList = [pluginA; pluginB']  @>
