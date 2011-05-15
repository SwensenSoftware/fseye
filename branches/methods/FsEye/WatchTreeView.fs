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
namespace Swensen.FsEye.Forms
open System.Windows.Forms
open System.Reflection
open Swensen.FsEye.Model
open Swensen.Utils

//features to add: Methods with lazy loading values
//type info about IEnumerable
//Copy / Copy Value context Menu

type WatchTreeView() as this =
    inherit TreeView()
    let rootWatchContextMenu = new ContextMenu()
    
    let mutable archiveCounter = 0
    
    let createWatchTreeNode guiContext (watch:Watch) =
        let tn = new TreeNode(Text=watch.Text, Tag=watch)

        match watch with
        | Root info ->
            tn.Name <- info.Name
            tn.ContextMenu <- rootWatchContextMenu
            tn.Nodes.Add("dummy") |> ignore
            tn, None
        | Custom _ -> 
            tn.Nodes.Add("dummy") |> ignore
            tn, None
        | DataMember(info) -> //need to make this not clickable, Lazy is not thread safe
            tn, Some(async {
                //let original = System.Threading.SynchronizationContext.Current //always null - don't understand the point
                let text = info.Lazy.Value.Text
                do! Async.SwitchToContext guiContext
                tn.Text <- text
                tn.Nodes.Add("dummy") |> ignore
                //do! Async.SwitchToContext original
            })
        | CallMember(info) ->
            tn.Nodes.Add("dummy") |> ignore
            tn, None

    let afterSelect (node:TreeNode) =
        match node.Tag with
        | :? Watch as watch ->
            match watch with
            | CallMember(info) when info.Lazy.IsValueCreated |> not ->
                node.Text <- info.Lazy.Value.Text
            | _ -> ()
        | _ -> ()

    let afterExpand (node:TreeNode) =
        match node.Tag with
        | :? Watch as watch when node.Nodes.Count = 1 && node.Nodes.[0].Text = "dummy" -> //need to harden this check for loaded vs. not
            node.Nodes.Clear() //clear dummy node
            match watch with
            | CallMember(info) when info.Lazy.IsValueCreated |> not ->
                node.Text <- info.Lazy.Value.Text
            | _ -> ()

            let context = System.Threading.SynchronizationContext.Current //gui thread
            let createWatchTreeNode = createWatchTreeNode context
            let asyncNodes = 
                [| for (tn, a) in watch.Children |> Seq.map createWatchTreeNode do
                        node.Nodes.Add(tn) |> ignore
                        match a with
                        | Some(a) -> yield a
                        | _ -> () |]

            asyncNodes
            |> Async.Parallel 
            |> Async.Ignore
            |> Async.Start
        | _ -> () //either an Archive node or IWatchNode children already expanded

    let refresh (node:TreeNode) =
        let watch = node.Tag :?> Watch
        let info = watch.RootMatch
        this.UpdateWatch(node, info.Value , if info.Value =& null then null else info.Value.GetType())

    do
        //set the selected node on mouse click so can use with right-click context menu
        this.MouseClick.Add <| fun args ->
            if args.Button = MouseButtons.Right then
                this.SelectedNode <- this.GetNodeAt(args.X, args.Y)

        (
            let mi = new MenuItem("Refresh")
            mi.Click.Add(fun args -> refresh this.SelectedNode)
            rootWatchContextMenu.MenuItems.Add(mi) |> ignore
        )
        (   
            let mi = new MenuItem("Remove")
            mi.Click.Add(fun args -> this.Nodes.Remove(this.SelectedNode))
            rootWatchContextMenu.MenuItems.Add(mi) |> ignore
        )

        this.AfterSelect.Add <| fun args ->
            this.BeginUpdate()
            (
                afterSelect args.Node
            )
            this.EndUpdate()

        this.AfterExpand.Add <| fun args -> 
            this.BeginUpdate()
            (
                afterExpand args.Node
            )
            this.EndUpdate()
    with
        member private this.UpdateWatch(tn:TreeNode, value, ty) =
            this.BeginUpdate()
            (
                let watch = createRootWatch tn.Name value ty
                tn.Text <- watch.Text
                tn.Tag <- watch
                tn.Nodes.Clear()
                tn.Nodes.Add("dummy") |> ignore
                tn.Collapse()
            )
            this.EndUpdate()
        member private this.AddWatch(name, value, ty) =
            this.BeginUpdate()
            (
                createRootWatch name value ty
                |> createWatchTreeNode null //don't like passing null here...
                |> fst
                |> this.Nodes.Add
                |> ignore
            )
            this.EndUpdate()

        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value, ty) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(tn) when (tn.Tag :?> Watch).RootMatch.Value <>& value -> 
                this.UpdateWatch(tn, value, ty)
            | None -> this.AddWatch(name, value, ty)
            | _ -> ()

        ///Add or update a watch with the given name and value.
        member this.Watch(name: string, value: 'a) =
            this.Watch(name, value, typeof<'a>)

        ///Take archival snap shot of all current watches using the given label.
        member this.Archive(label: string) =
            this.BeginUpdate()
            (
                let nodesToArchiveBeforeClone =
                    this.Nodes 
                    |> Seq.cast<TreeNode> 
                    |> Seq.filter (fun tn -> tn.Tag :? Watch)
                    |> Seq.toArray //need to convert to array or get lazy evaluation issues!

                let nodesToArchiveCloned =
                    nodesToArchiveBeforeClone
                    |> Seq.map (fun tn -> tn.Clone() :?> TreeNode) 
                    |> Seq.toArray
            
                nodesToArchiveBeforeClone
                |> Seq.iter (fun tn -> this.Nodes.Remove(tn))

                let archiveNode = TreeNode(Text = label)

                nodesToArchiveCloned 
                |> archiveNode.Nodes.AddRange
                
                archiveNode 
                |> this.Nodes.Add 
                |> ignore

                archiveCounter <- archiveCounter + 1
            )
            this.EndUpdate()

        ///Take archival snap shot of all current watches using a default label based on an archive count.
        member this.Archive() = this.Archive(sprintf "Archive (%i)" archiveCounter)

        ///Clear all archives and reset the archive count.
        member this.ClearArchives() =
            this.Nodes 
            |> Seq.cast<TreeNode> 
            |> Seq.filter (fun tn -> tn.Tag :? Watch |> not)
            |> Seq.toArray
            |> Array.iter (fun tn -> this.Nodes.Remove(tn))
            
            archiveCounter <- 0

        ///Clear all watches (doesn't include archive nodes).
        member this.ClearWatches() =
            this.Nodes 
            |> Seq.cast<TreeNode> 
            |> Seq.filter (fun tn -> tn.Tag :? Watch)
            |> Seq.toArray
            |> Array.iter (fun tn -> this.Nodes.Remove(tn))

        ///Clear all archives (reseting archive count) and watches.
        member this.ClearAll() = 
            this.Nodes.Clear()
            archiveCounter <- 0