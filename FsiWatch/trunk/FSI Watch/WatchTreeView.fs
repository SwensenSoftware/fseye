namespace Swensen.Watch.Forms
open System.Windows.Forms
open System.Reflection
open Swensen.Watch.Model

type WatchTreeView() as this =
    inherit TreeView()
    let contextMenu = new ContextMenu()
    
    let mutable archiveCounter = 0
    
    let createWatchTreeNode isRoot (watchNode:WatchNode) =
        let tn = TreeNode(Name=watchNode.Name, Text=watchNode.Text, Tag=watchNode, ContextMenu=if isRoot then contextMenu else null)
        tn.Nodes.Add("dummy") |> ignore
        tn

    let createWatchChildTreeNode = createWatchTreeNode false
    let createWatchRootTreeNode = createWatchTreeNode true

    let afterExpand (node:TreeNode) =
        match node.Tag with
        | :? WatchNode as watchNode when watchNode.Children.IsValueCreated |> not ->
            node.Nodes.Clear() //clear dummy node
            watchNode.Children.Value
            |> Seq.map createWatchChildTreeNode
            |> Seq.toArray
            |> node.Nodes.AddRange
        | _ -> () //either an Archive node or IWatchNode children already expanded

    let refresh (node:TreeNode) =
        let watch = node.Tag :?> WatchNode
        this.UpdateWatch(node, watch.Value, if obj.ReferenceEquals(watch, null) then null else watch.GetType())

    do
        this.MouseClick.Add(fun args ->
            if args.Button = MouseButtons.Right then
                this.SelectedNode <- this.GetNodeAt(args.X, args.Y))

        (
            let mi = new MenuItem("Refresh")
            mi.Click.Add(fun args -> refresh this.SelectedNode)
            contextMenu.MenuItems.Add(mi) |> ignore
        )
        (
            let mi = new MenuItem("Remove")
            mi.Click.Add(fun args -> this.Nodes.Remove(this.SelectedNode))
            contextMenu.MenuItems.Add(mi) |> ignore
        )

        this.AfterExpand.Add (fun args -> 
            this.BeginUpdate()
            (
                afterExpand args.Node
            )
            this.EndUpdate()
        )
    with
        member private this.UpdateWatch(tn:TreeNode, value, ty) =
            this.BeginUpdate()
            (
                let wn = createWatchNode tn.Name value  ty
                tn.Text <- wn.Text
                tn.Tag <- wn
                tn.Nodes.Clear()
                tn.Nodes.Add("dummy") |> ignore
                tn.Collapse()
            )
            this.EndUpdate()
        member private this.AddWatch(name, value, ty) =
            this.BeginUpdate()
            (
                createWatchNode name value ty
                |> createWatchRootTreeNode
                |> this.Nodes.Add
                |> ignore
            )
            this.EndUpdate()

        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name: string, value, ty) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(tn) when obj.ReferenceEquals((tn.Tag :?> WatchNode).Value, value) |> not -> this.UpdateWatch(tn, value, ty)
            | None -> this.AddWatch(name, value, ty)
            | _ -> ()

        ///Add or update a watch with the given name and value, determine the type if not null.
        member this.Watch(name: string, value) =
            this.Watch(name, value, if obj.ReferenceEquals(value, null) then null else value.GetType())

        ///Add or update all the elements in the sequence by name and value, determine null type if not null.
        member this.Watch(watchList:seq<string * obj>) =
            watchList |> Seq.iter this.Watch

        ///Add or update all the elements in the sequence by name, value, and type.
        member this.Watch(watchList:seq<string * obj * System.Type>) =
            watchList |> Seq.iter this.Watch

        ///take archival snap shot of all current watches
        member this.Archive(label: string) =
            this.BeginUpdate()
            (
                let nodesToArchiveBeforeClone =
                    this.Nodes 
                    |> Seq.cast<TreeNode> 
                    |> Seq.filter (fun tn -> tn.Tag :? WatchNode)
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

        ///Take archival snap shot of all current watches with a default label
        member this.Archive() = this.Archive(sprintf "Archive (%i)" archiveCounter)

        ///Clear all watches (doesn't include archive nodes
        member this.ClearWatches() =
            this.Nodes 
            |> Seq.cast<TreeNode> 
            |> Seq.filter (fun tn -> tn.Tag :? WatchNode)
            |> Seq.toArray
            |> Array.iter (fun tn -> this.Nodes.Remove(tn))

        ///Clear all archives and reset archive count
        member this.ClearArchives() =
            this.Nodes 
            |> Seq.cast<TreeNode> 
            |> Seq.filter (fun tn -> tn.Tag :? WatchNode |> not)
            |> Seq.toArray
            |> Array.iter (fun tn -> this.Nodes.Remove(tn))
            
            archiveCounter <- 0