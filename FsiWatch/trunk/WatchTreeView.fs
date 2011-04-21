namespace Swensen.FsiWatch.Forms
open System.Windows.Forms
open System.Reflection

type WatchTreeView() as this =
    inherit TreeView()

    let updateNode (tn:TreeNode) name tag =
        tn.Tag <- tag
        tn.Name <- name
        tn.Text <- sprintf "%s: %s" name (if obj.ReferenceEquals(tag, null)  then "null" else tag.ToString())

    let createNode name tag = 
        let tn = new TreeNode()
        updateNode tn name tag
        tn

    let updateNodeChildren (tn:TreeNode) =
        tn.Nodes.Clear()
        if tn.Tag <> null then
            let props = 
                tn.Tag.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)
                |> Seq.sortBy (fun p -> p.Name.ToLower())

            for p in props do
                //printfn "%A updating child with %A" tn.Tag p.Name
                
                //this is a hack, this.AccessiblityObject.Parent hangs, probably due to a lock
                //need to implement timeout, perhaps configurable
                //no longer needed once we moved watch tree view out
                //let skip = (obj.ReferenceEquals(tn.Tag, this.AccessibilityObject)) && p.Name = "Parent"
                // && (not skip)
                if p.GetIndexParameters() = Array.empty then //non-indexed property
                    try
                        //printfn "before getvalue"
                        let propValue = p.GetValue(tn.Tag, Array.empty)
                        //printfn "after getvalue"
                        let isPublic  = p.GetGetMethod(true).IsPublic
                        ignore <| tn.Nodes.Add(createNode (sprintf "%s (%s property)" p.Name (if isPublic then "public" else "private")) propValue)
                    with e ->
                        //printfn "%A" e
                        ignore <| tn.Nodes.Add(createNode p.Name e)
    do
        //when expanding a node, add all immediate children to each child
        this.AfterExpand.Add (fun args -> for node in args.Node.Nodes do updateNodeChildren node)
    with
        member this.UpdateWatch(tn:TreeNode, tag) =
            this.BeginUpdate()
            (
                updateNode tn tn.Name tag
                updateNodeChildren tn
            )
            this.EndUpdate()
            ()
        member this.AddWatch(name, tag) =
            this.BeginUpdate()
            (
                //create new node and add all it's immediate children
                let node = createNode name tag
                updateNodeChildren node
                ignore <| this.Nodes.Add(node)
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