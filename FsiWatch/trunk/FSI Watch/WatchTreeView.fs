namespace Swensen.Watch.Forms
open System.Windows.Forms
open System.Reflection
open Swensen.Watch.Model

type WatchTreeView() as this =
    inherit TreeView()

    let mutable archiveCounter = 0

    //should also include type here
    let getRootNodeText name tag = 
        sprintf "%s: %A" name tag

    let updateNode (tn:TreeNode) name tag text =
        tn.Tag <- tag
        tn.Name <- name
        tn.Text <- text

    let createNode name tag text = 
        let tn = new TreeNode()
        updateNode tn name tag text
        tn

    let getTypeNodeName ownerName = 
        ownerName + "@Type"

    let getTypeNodeText (ownerType:System.Type) =
        sprintf "Type: %s" (ownerType.ToString())

    ///if tag is null, do nothing, this is a special node, or otherwise has no meaningful children
    //acutally N.B. Tag should encode more info than just the value of the watch type, in particular
    //capture whether a load attempt has already been made on a type, or the type of a null value
    //if we have it.
    let rec updateNodeChildren (tn:TreeNode) =
        if tn.Tag <> null then
            tn.Nodes.Clear()

            //create type node
            //don't create Type node for type Node, or will get infinite recursion
            if tn.Name.EndsWith("@Type") |> not then //want to be stronger typed :(
                let typeNode = createNode (getTypeNodeName tn.Name) (tn.Tag.GetType()) (getTypeNodeText (tn.Tag.GetType()))
                updateNodeChildren typeNode

                typeNode
                |> tn.Nodes.Add
                |> ignore

            //add Results node if the given Tag value is of IEnumerable
            //call it "Results" because that's what VS Watch window does
            match tn.Tag with
            | :? System.Collections.IEnumerable as results -> //todo: chunck so take first 100 nodes or so, and then keep expanding "Rest" last node until exhausted
                let results = ResultModel.GetResults(tn.Name, results) //results are truncated
                let resultsRootNode = 
                    createNode 
                        (tn.Name + "@Results") 
                        null 
                        (sprintf "Results: %A" (results |> Seq.map (fun x -> x.Value)))

                results
                |> Seq.map (fun x -> createNode x.Name x.Value x.Text)
                |> Seq.toArray
                |> resultsRootNode.Nodes.AddRange

                resultsRootNode
                |> tn.Nodes.Add
                |> ignore
            | _ -> ()

            let createNodeFromModel model = 
                createNode model.Name model.Value model.Text

            //might want to move this to model area
            let nonPublicModels, publicModels = 
                MemberModel.GetMembers(tn.Tag)

            //add non public node if there are any non public members
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

        ///Add or update a watch with the given name.
        member this.Watch(name: string, tag) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(tn) when obj.ReferenceEquals(tn.Tag, tag) |> not -> this.UpdateWatch(tn, tag)
            | None -> this.AddWatch(name, tag)
            | _ -> ()

        ///Add or update all the elements in the sequence by name.
        member this.Watch(watchList:seq<string*obj>) =
            watchList |> Seq.iter this.Watch

        //NOT WORKING RIGHT NOW
        ///take archival snap shot of all current watches
        member this.Archive(label: string) =
            this.BeginUpdate()
            (
                let nodesToArchiveBeforeClone =
                    this.Nodes 
                    |> Seq.cast<TreeNode> 
                    |> Seq.filter (fun tn -> tn.Name.EndsWith("@Archive") |> not)
                    |> Seq.toArray //need to convert to array or get lazy evaluation issues!

                let nodesToArchiveCloned =
                    nodesToArchiveBeforeClone
                    |> Seq.map (fun tn -> tn.Clone() :?> TreeNode) 
                    |> Seq.toArray
            
                nodesToArchiveBeforeClone
                |> Seq.iter (fun tn -> this.Nodes.Remove(tn))

                let archiveNode = createNode (sprintf "%i@Archive" archiveCounter) null label
                nodesToArchiveCloned |> archiveNode.Nodes.AddRange
                archiveNode |> this.Nodes.Add |> ignore

                archiveCounter <- archiveCounter + 1
            )
            this.EndUpdate()

        ///take archival snap shot of all current watches with a default label
        member this.Archive() = this.Archive(sprintf "Archive (%i)" archiveCounter)