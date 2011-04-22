namespace Swensen.FsiWatch.Forms
open System.Windows.Forms
open System.Reflection
open Swensen.Watch.Model

type WatchTreeView() as this =
    inherit TreeView()

    //should also include type here
    let getRootNodeText name tag = sprintf "%s: %s" name (if obj.ReferenceEquals(tag, null)  then "null" else tag.ToString())

    let updateNode (tn:TreeNode) name tag text =
        tn.Tag <- tag
        tn.Name <- name
        tn.Text <- text

    let createNode name tag text = 
        let tn = new TreeNode()
        updateNode tn name tag text
        tn

    ///if tag is null, do nothing, this is a special node
    let updateNodeChildren (tn:TreeNode) =
        if tn.Tag <> null then
            tn.Nodes.Clear()
            let createNodeFromModel model = 
                createNode model.Name model.LazyValue.Value model.Text

            let nonPublicModels, publicModels = 
                WatchModel.GetFieldsAndProperties(tn.Tag)
                |> Seq.toArray
                |> Seq.sortBy (fun x -> x.Name)
                |> Seq.toArray
                |> Array.partition (fun x -> x.Protection = WatchProtection.NonPublic)

            if nonPublicModels.Length > 0 then
                let nonPublicRootNode = createNode (tn.Name + "@Non-public") null "Non-public"
                nonPublicRootNode.Nodes.AddRange(nonPublicModels |> Array.map createNodeFromModel)
                tn.Nodes.Add(nonPublicRootNode) |> ignore

            tn.Nodes.AddRange(publicModels |> Array.map createNodeFromModel)
    do
        //when expanding a node, add all immediate children to each child if not already populated
        this.AfterExpand.Add (fun args -> for node in args.Node.Nodes do if node.Nodes.Count = 0 then updateNodeChildren node)
    with
        member this.UpdateWatch(tn:TreeNode, tag) =
            this.BeginUpdate()
            (
                updateNode tn tn.Name tag (getRootNodeText tn.Name tag)
                updateNodeChildren tn
                tn.Collapse()
            )
            this.EndUpdate()
            ()
        member this.AddWatch(name, tag) =
            this.BeginUpdate()
            (
                //create new node and add all it's immediate children
                let node = createNode name tag (getRootNodeText name tag)
                updateNodeChildren node
                this.Nodes.Add(node) |> ignore
            )
            this.EndUpdate()
            ()
        member this.AddOrUpdateWatch(name: string, tag:obj) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(tn) when obj.ReferenceEquals(tn.Tag, tag) |> not -> this.UpdateWatch(tn, tag)
            | None -> this.AddWatch(name, tag)
            | _ -> ()