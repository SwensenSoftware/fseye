namespace Swensen.Watch.Forms
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
                createNode model.Name model.Value model.Text

            //might want to move this to model area
            let nonPublicModels, publicModels = 
                MemberModel.GetFieldsAndProperties(tn.Tag)
                |> Seq.sortBy (fun x -> x.Name)
                |> Seq.toArray
                |> Array.partition (fun x -> x.Protection = MemberModelProtection.NonPublic)

            if nonPublicModels.Length > 0 then
                let nonPublicRootNode = createNode (tn.Name + "@Non-public") null "Non-public"
                
                nonPublicModels 
                |> Array.map createNodeFromModel 
                |> nonPublicRootNode.Nodes.AddRange
                
                nonPublicRootNode 
                |> tn.Nodes.Add 
                |> ignore

            publicModels 
            |> Array.map createNodeFromModel 
            |> tn.Nodes.AddRange

            //call "Results" because that's what VS Watch window does
            match tn.Tag with
            | :? System.Collections.IEnumerable as results -> //todo: chunck so take first 100 nodes or so, and then keep expanding "Rest" last node until exhausted
                let resultsRootNode = createNode (tn.Name + "@Results") null "Results"
                results
                |> Seq.cast<obj>
                |> Seq.truncate 100
                |> Seq.mapi (fun i x -> createNode (sprintf "%s@Results[%i]" tn.Name i) x (sprintf "[%i]" i))
                |> Seq.toArray
                |> resultsRootNode.Nodes.AddRange

                
                resultsRootNode
                |> tn.Nodes.Add
                |> ignore
            | _ -> ()
    do
        //when expanding a node, add all immediate children to each child if not already populated
        this.AfterExpand.Add (fun args -> for node in args.Node.Nodes do if node.Nodes.Count = 0 then updateNodeChildren node)
    with
        member private this.UpdateWatch(tn:TreeNode, tag) =
            this.BeginUpdate()
            (
                updateNode tn tn.Name tag (getRootNodeText tn.Name tag)
                updateNodeChildren tn
                tn.Collapse()
            )
            this.EndUpdate()
        member private this.AddWatch(name, tag) =
            this.BeginUpdate()
            (
                //create new node and add all it's immediate children
                let node = createNode name tag (getRootNodeText name tag)
                updateNodeChildren node
                this.Nodes.Add(node) |> ignore
            )
            this.EndUpdate()
        member this.AddOrUpdateWatch(name: string, tag:obj) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(tn) when obj.ReferenceEquals(tn.Tag, tag) |> not -> this.UpdateWatch(tn, tag)
            | None -> this.AddWatch(name, tag)
            | _ -> ()