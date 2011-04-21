namespace Swensen.FsiWatch.Forms
open System.Windows.Forms
open System.Reflection

type ModelKind = 
    | Field
    | Property
//    | Enumerable
//    | Type

type ModelProtection =
    | Private
    | Public

type Model = { Name:string; LazyValue: Lazy<obj>; Kind: ModelKind; Protection: ModelProtection; Type: System.Type }

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

    let updateNodeChildren (tn:TreeNode) =
        tn.Nodes.Clear()
        if tn.Tag <> null then
            let propertyModels = seq {
                let props = 
                    tn.Tag.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                for p in props do
                    if p.GetIndexParameters() = Array.empty then //non-indexed property
                        let isPublic  = p.GetGetMethod(true).IsPublic
                        let propValue = lazy (
                            try
                                p.GetValue(tn.Tag, Array.empty)
                            with e ->
                                e :> obj
                        )
                        yield {Name = p.Name; LazyValue = propValue; Kind = Property; Protection = (if isPublic then Public else Private); Type = p.PropertyType}
            }

            let fieldModels = seq {
                let fields = 
                    tn.Tag.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                for f in fields do
                    let isPublic  = f.IsPublic
                    let fieldValue = lazy (
                        try 
                            f.GetValue(tn.Tag)
                        with e ->
                            e :> obj
                    )
                    yield {Name = f.Name; LazyValue = fieldValue; Kind = Field; Protection = (if isPublic then Public else Private); Type = f.FieldType}
            }

            let models = seq {yield! propertyModels; yield! fieldModels}
            let modelNodes =
                models
                |> Seq.map
                    (fun m ->
                        createNode 
                            m.Name  
                            m.LazyValue.Value
                            (sprintf "%s (%s %s %s): %s" 
                                (m.Name)
                                (m.Protection |> function | Public -> "public" | Private -> "private") 
                                (m.Kind |> function | Field -> "field" | Property -> "property")
                                (m.Type.Name)
                                (if obj.ReferenceEquals(m.LazyValue.Value, null) then "null" else m.LazyValue.Value.ToString())))
                |> Seq.sortBy (fun n -> n.Name.ToLower())
                |> Seq.toArray

            tn.Nodes.AddRange(modelNodes)
    do
        //when expanding a node, add all immediate children to each child
        this.AfterExpand.Add (fun args -> for node in args.Node.Nodes do updateNodeChildren node)
    with
        member this.UpdateWatch(tn:TreeNode, tag) =
            this.BeginUpdate()
            (
                updateNode tn tn.Name tag (getRootNodeText tn.Name tag)
                updateNodeChildren tn
            )
            this.EndUpdate()
            ()
        member this.AddWatch(name, tag) =
            this.BeginUpdate()
            (
                //create new node and add all it's immediate children
                let node = createNode name tag (getRootNodeText name tag)
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