namespace Swensen.Watch.Forms
open System.Windows.Forms
open System.Reflection
open Swensen.Watch.Model

type WatchTreeView() as this =
    inherit TreeView()
    let mutable archiveCounter = 0
    let createTreeNode (watchNode:IWatchNode) =
        let tn = new TreeNode()
        tn.Name <- watchNode.Name
        tn.Text <- watchNode.Text
        tn.Tag <- watchNode
        tn.Nodes.Add("dummy") |> ignore
        tn

    let afterExpand (node:TreeNode) =
        match node.Tag with
        | :? IWatchNode as watchNode when watchNode.Children.IsValueCreated |> not ->
            node.Nodes.Clear() //clear dummy node
            watchNode.Children.Value
            |> Seq.map createTreeNode
            |> Seq.toArray
            |> node.Nodes.AddRange
        | _ -> () //either an Archive node or IWatchNode children already expanded
    do
        this.AfterExpand.Add (fun args -> afterExpand args.Node)
    with
        member private this.UpdateWatch(tn:TreeNode, value, ty) =
            this.BeginUpdate()
            (
                let wn = Watch(tn.Name, value, ty)
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
                Watch(name, value, ty) 
                |> createTreeNode
                |> this.Nodes.Add
                |> ignore
            )
            this.EndUpdate()

        ///Add or update a watch with the given name.
        member this.Watch(name: string, value, ty) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(tn) when obj.ReferenceEquals((tn.Tag :?> Watch).Value, value) |> not -> this.UpdateWatch(tn, value, ty)
            | None -> this.AddWatch(name, value, ty)
            | _ -> ()

        ///Add or update all the elements in the sequence by name.
        member this.Watch(watchList:seq<string * obj * System.Type>) =
            watchList |> Seq.iter this.Watch

        //NOT WORKING RIGHT NOW
        ///take archival snap shot of all current watches
        member this.Archive(label: string) =
            this.BeginUpdate()
            (
                let nodesToArchiveBeforeClone =
                    this.Nodes 
                    |> Seq.cast<TreeNode> 
                    |> Seq.filter (fun tn -> tn.Tag |> function | :? Watch -> true | _ -> false)
                    |> Seq.toArray //need to convert to array or get lazy evaluation issues!

                let nodesToArchiveCloned =
                    nodesToArchiveBeforeClone
                    |> Seq.map (fun tn -> tn.Clone() :?> TreeNode) 
                    |> Seq.toArray
            
                nodesToArchiveBeforeClone
                |> Seq.iter (fun tn -> this.Nodes.Remove(tn))

                let archiveNode = TreeNode()
                archiveNode.Name <- (sprintf "%i@Archive" archiveCounter)
                archiveNode.Text <- label

                nodesToArchiveCloned 
                |> archiveNode.Nodes.AddRange
                
                archiveNode 
                |> this.Nodes.Add 
                |> ignore

                archiveCounter <- archiveCounter + 1
            )
            this.EndUpdate()

        ///take archival snap shot of all current watches with a default label
        member this.Archive() = this.Archive(sprintf "Archive (%i)" archiveCounter)