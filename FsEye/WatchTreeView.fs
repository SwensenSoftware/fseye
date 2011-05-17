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

//Copy / Copy Value context Menu

//for thoughts on cancellation:
//http://stackoverflow.com/questions/5852317/how-to-cancel-individual-async-computation-being-run-in-parallel-with-others-fr

type WatchTreeView() as this =
    inherit TreeView()
    static let dummyText = "dummy"
    static let hasDummyChild (tn:TreeNode) = 
        tn.Nodes.Count = 1 && tn.Nodes.[0].Text = dummyText

    let rootWatchContextMenu = new ContextMenu()
    
    let mutable archiveCounter = 0

    let loadWatchAsync guiContext (tn:TreeNode) info addDummy =
        async {
            let original = System.Threading.SynchronizationContext.Current //always null - don't understand the point
            let text = info.Lazy.Value.Text
            do! Async.SwitchToContext guiContext
            Control.update this <| fun () ->
                tn.Text <- text
                if addDummy then tn.Nodes.Add(dummyText) |> ignore
            do! Async.SwitchToContext original
        }
    
    let createWatchTreeNode guiContext (watch:Watch) =
        let tn = new TreeNode(Text=watch.Text, Tag=watch)

        match watch with
        | Root info ->
            tn.Name <- info.Name
            tn.ContextMenu <- rootWatchContextMenu
            tn.Nodes.Add(dummyText) |> ignore
            tn, None
        | Custom _ -> 
            tn.Nodes.Add(dummyText) |> ignore
            tn, None
        | DataMember(info) -> //need to make this not clickable, Lazy is not thread safe
            tn, Some(loadWatchAsync guiContext tn info true)
        | CallMember(info) ->
            tn.Nodes.Add(dummyText) |> ignore
            tn, None

    let afterSelect (tn:TreeNode) =
        match tn.Tag with
        | :? Watch as watch when hasDummyChild tn ->
            match watch with
            | CallMember(info) ->
                Control.update this <| fun () ->
                    tn.Nodes.Clear() //so don't try click while still async loading
                    tn.Text <- info.Text + " = Loading..." //need to move to model

                let guiContext = System.Threading.SynchronizationContext.Current
                loadWatchAsync guiContext tn info true |> Async.Start
            | _ -> ()
        | _ -> ()

    //note:FSharpRefactor doesn't rename variables in when clause of a pattern match
    let afterExpand (node:TreeNode) =
        match node.Tag with
        | :? Watch as watch when hasDummyChild node -> //need to harden this check for loaded vs. not            
            let loadWatches context (node:TreeNode) (watch:Watch) =
                this.BeginUpdate()
                node.Nodes.Clear() //clear dummy node
                let createWatchTreeNode = createWatchTreeNode context
                let asyncNodes = 
                    [| for (tn, a) in watch.Children |> Seq.map createWatchTreeNode do
                        node.Nodes.Add(tn) |> ignore
                        match a with
                        | Some(a) -> yield a
                        | _ -> () |]
                this.EndUpdate()
                //N.B. deliberately excludeing Asyn.Start pipe-line from begin/end update 
                //so child nodes have chance to expand before parallel updates start kicking off
                asyncNodes
                |> Async.Parallel 
                |> Async.Ignore
                |> Async.Start

            let guiContext = System.Threading.SynchronizationContext.Current //gui thread
            match watch with
            | CallMember(info) ->
                node.Nodes.Clear()
                node.Text <- info.Text + " = Loading..." //need to move to model
                async {
                    let original = System.Threading.SynchronizationContext.Current
                    do! loadWatchAsync guiContext node info false
                    do! Async.SwitchToContext guiContext
                    do loadWatches guiContext node watch
                    do! Async.SwitchToContext original
                } |> Async.Start
            | _ -> 
                loadWatches guiContext node watch
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
            afterSelect args.Node

        this.AfterExpand.Add <| fun args -> 
            afterExpand args.Node
    with
        member private this.UpdateWatch(tn:TreeNode, value, ty) =
            Control.update this <| fun () ->
                let watch = createRootWatch tn.Name value ty
                tn.Text <- watch.Text
                tn.Tag <- watch
                tn.Nodes.Clear()
                tn.Nodes.Add(dummyText) |> ignore
                tn.Collapse()

        member private this.AddWatch(name, value, ty) =
            Control.update this <| fun () ->
                createRootWatch name value ty
                |> createWatchTreeNode null //don't like passing null here...
                |> fst
                |> this.Nodes.Add
                |> ignore

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

        ///Clear all nodes satisfying the given predicate.
        member this.ClearAll(predicate) =
            Control.update this <| fun () ->                
                //N.B. can't remove node while iterating Nodes, so need to make array first
                [| for tn in this.Nodes do if predicate tn then yield tn |]
                |> Seq.iter (fun tn -> this.Nodes.Remove(tn))

        ///Take archival snap shot of all current watches using the given label.
        member this.Archive(label: string) =
            Control.update this <| fun () ->
                let nodesToArchiveCloned = 
                    [| for tn in this.Nodes do
                        if tn.Tag :? Watch then yield tn.Clone() :?> TreeNode |]
            
                this.ClearAll(fun tn -> tn.Tag :? Watch)

                let archiveNode = TreeNode(Text = label)
                archiveNode.Nodes.AddRange(nodesToArchiveCloned)
                this.Nodes.Add(archiveNode) |> ignore
                archiveCounter <- archiveCounter + 1

        ///Take archival snap shot of all current watches using a default label based on an archive count.
        member this.Archive() = 
            this.Archive(sprintf "Archive (%i)" archiveCounter)

        ///Clear all archives and reset the archive count.
        member this.ClearArchives() =
            this.ClearAll(fun tn -> tn.Tag :? Watch |> not)
            archiveCounter <- 0

        ///Clear all watches (doesn't include archive nodes).
        member this.ClearWatches() =
            this.ClearAll(fun tn -> tn.Tag :? Watch)

        ///Clear all archives (reseting archive count) and watches.
        member this.ClearAll() =
            this.Nodes.Clear()
            archiveCounter <- 0