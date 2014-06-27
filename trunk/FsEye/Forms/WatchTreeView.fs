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

open Swensen.Utils
open Swensen.FsEye
open Swensen.FsEye.WatchModel


//Copy / Copy Value context Menu

//for thoughts on cancellation:
//http://stackoverflow.com/questions/5852317/how-to-cancel-individual-async-computation-being-run-in-parallel-with-others-fr

///A TreeView which binds to and manipulates a Watch model.
///The PluginManager argument is only used for the primary FsEye WatchTreeView,
///it should be None for WatchTreeViews hosted as plugins.
type WatchTreeView(pluginManager: PluginManager option) as this =
    inherit TreeView()
    
    static let requiresUIThread (ty:System.Type) =
        [typeof<System.Windows.Forms.Control>
         System.Type.GetType("System.Windows.UIElement", false)] //mono does not support WPF
        |> Seq.filter ((<>&) null)
        |> Seq.exists ty.IsAssignableFrom

    static let (|Archive|Watch|) (tn:TreeNode) =
        match tn.Tag with
        | :? Watch as w -> Watch(w)
        | _ -> Archive

    static let isWatch = function
        | Watch _ -> true
        | _ -> false

    static let isArchive = function
        | Archive -> true
        | _ -> false

    ///The text used for constructing "dummy" TreeNodes used as a place-holder until childnodes are 
    ///loaded lazily.
    static let dummyText = "dummy"

    ///Checks whether the given TreeNode contains the default "dummy" TreeNode as it's only child.
    static let hasDummyChild (tn:TreeNode) = 
        tn.Nodes.Count = 1 && tn.Nodes.[0].Text = dummyText

    ///The action to perform on the given TreeNode after the "Refresh" menu item has
    ///been clicked via the right-click context menu (which can only be performed on root TreeNode's
    ///for Root Watches).
    let refresh (node:TreeNode) =
        match node with
        | Watch(Root({ValueInfo=vi})) ->
            this.UpdateWatch(node, vi.Value, vi.Type)
        | _ -> failwith "TreeNode was not a Root Watch"

    ///Calculate the "label" for the given node. i.e., the expression, starting from the root, from which the current node value is obtained. Used by the SendTo plugin.
    let calcNodeLabel (tn:TreeNode) =
        //todo: should not fail hard in unexpected node cases, instead return something like "!error!" and write detailed message to a local log file 
        //use Trace API for now to help keep 3rd party libs out for now.
        let rec loop (cur:TreeNode) = 
            match cur with
            | null -> ""
            | Archive -> sprintf "[%s] " cur.Text
            | Watch (Root {ExpressionInfo=ei}) -> sprintf "%s%s" (loop cur.Parent) ei.Expression
            | Watch w ->
                match w.ExpressionInfo with
                | None -> loop cur.Parent
                | Some(ei) ->
                    let separator =
                        if ei.IsPublic then "."
                        else "?"

                    match ei.ExplicitInterfaceName with
                    | Some(iface) ->
                        sprintf "(%s :> %s)%s%s" (loop cur.Parent) iface separator ei.Expression
                    | None ->
                        sprintf "%s%s%s" (loop cur.Parent) separator ei.Expression
        loop tn

    let createNodeContextMenu (tn:TreeNode) = 
        new ContextMenu [|
            match tn with
            | Watch(Root(_)) ->
                let mi = new MenuItem("Refresh") 
                mi.Click.Add(fun args -> refresh tn) 
                yield mi
            | _ -> ()

            match tn with
            | Watch(Root(_)) | Archive ->
                let mi = new MenuItem("Remove")
                mi.Click.Add(fun args -> this.Nodes.Remove(tn))
                yield mi
            | _ -> ()

            match tn with
            | Watch(Organizer _ ) -> ()
            | Watch(w) ->
                match w with
                | Root _ ->
                    yield new MenuItem("-") //n.b. for root watches, ValueInfo is always Some
                | _ -> ()

                let mi = new MenuItem("Copy Value")
                match w.ValueInfo with
                | Some({Text=vtext}) -> mi.Click.Add(fun _ -> Clipboard.SetText(vtext))
                | None -> mi.Enabled <- false
                yield mi 

                match pluginManager with
                | Some(pluginManager) ->
                    //issues 25 and 26 (plugin architecture and view property grid)
                    let miSendTo = new MenuItem("Send To")
                    match w.ValueInfo with
                    | Some(vi) when pluginManager.ManagedPlugins |> Seq.length > 0 ->
                        let label = lazy(calcNodeLabel tn) //lazy since we need it 0 to x times depending on Plugin.IsWatchable
                        miSendTo.MenuItems.AddRange [|
                            let managedPlugins = 
                                pluginManager.ManagedPlugins 
                                |> Seq.map (fun mp -> mp.Plugin.IsWatchable(vi.Value, vi.Type), mp)

                            //send to a new watch                                
                            for (enabled, managedPlugin) in managedPlugins do
                                let miWatchViewer = new MenuItem(managedPlugin.Plugin.Name + " (new)")
                                miWatchViewer.Enabled <- enabled
                                miWatchViewer.Click.Add(fun _ -> pluginManager.SendTo(managedPlugin, label.Force(), vi.Value, vi.Type) |> ignore)
                                yield miWatchViewer
                            
                            //send to an existing watch                                
                            let managedWatchViewers = [|
                                for (enabled, managedPlugin) in managedPlugins do
                                    if managedPlugin.ManagedWatchViewers |> Seq.length > 0 then
                                        for managedWatchViewer in managedPlugin.ManagedWatchViewers do
                                            let miWatchViewer = new MenuItem(managedWatchViewer.ID)
                                            miWatchViewer.Enabled <- enabled
                                            miWatchViewer.Click.Add(fun _ -> pluginManager.SendTo(managedWatchViewer, label.Force(), vi.Value, vi.Type))
                                            yield miWatchViewer
                            |]
                            if managedWatchViewers |> Seq.length > 0 then
                                yield new MenuItem("-")
                                yield! managedWatchViewers
                        |]
                    | _ -> 
                        miSendTo.Enabled <- false
                    yield miSendTo
                | None -> ()
            | _ -> () |]
    
    let mutable archiveCounter = 0

    ///Constructs an async expression used for updating the given TreeNode with values from the given Lazy<Custom>.
    let loadWatchAsync guiContext (tn:TreeNode) (lz:Lazy<MemberValue>) addDummy =
        async {
            let original = System.Threading.SynchronizationContext.Current //always null - don't understand the point
            let text = lz.Value.LoadedText
            do! Async.SwitchToContext guiContext
            Control.update this <| fun () ->
                tn.Text <- text
                if addDummy then tn.Nodes.Add(dummyText) |> ignore
            do! Async.SwitchToContext original
        }
    
    ///Create a TreeNode from the given Watch model. If the Watch is a DataMember, then
    ///the guiContext must be provided and is used to construct an async expression returned
    ///as the second element of the tuple. Otherwise guiContext may be null and the second 
    ///element of the tuple is None.
    let createWatchTreeNode guiContext (watch:Watch) =
        let tn = new TreeNode(Text=watch.DefaultText, Tag=watch, ImageKey=watch.Image.Name, SelectedImageKey=watch.Image.Name)

        match watch with
        | Root info ->
            tn.Name <- info.Name
            tn.Nodes.Add(dummyText) |> ignore
            tn, None
        | DataMember(info) -> //need to make this not clickable, Lazy is not thread safe
            if info.MemberInfo.DeclaringType |> requiresUIThread then //issue 20
                tn.Text <- info.LazyMemberValue.Value.LoadedText
                tn.Nodes.Add(dummyText) |> ignore
                tn, None
            else
                tn, Some(loadWatchAsync guiContext tn info.LazyMemberValue true)
        | _ ->
            tn.Nodes.Add(dummyText) |> ignore
            tn, None

    ///The action to perform on the given TreeNode after it has been selected:
    ///if the Tag of the given TreeNode is a CallMember Watch which hasn't yet been loaded,
    ///Asyncrounously execute the method and show it's return value (but do not load it's children).
    let afterSelect (tn:TreeNode) =
        match tn.Tag with
        | :? Watch as watch when hasDummyChild tn ->
            match watch with
            | CallMember(info) when info.LazyMemberValue.IsValueCreated |> not ->
                if info.MemberInfo.DeclaringType |> requiresUIThread then //issue 20
                    Control.update this <| fun () ->
                        tn.Text <- info.LazyMemberValue.Value.LoadedText
                        //note that the dummy node is already added
                else
                    Control.update this <| fun () ->
                        tn.Nodes.Clear() //so don't try click while still async loading
                        tn.Text <- info.LoadingText

                    let guiContext = System.Threading.SynchronizationContext.Current
                    loadWatchAsync guiContext tn info.LazyMemberValue true |> Async.Start
            | _ -> ()
        | _ -> ()

    //note: FSharpRefactor doesn't rename variables in when clause of a pattern match
    ///The action to perform on the given TreeNode after it has been expanded:
    ///Load all the child watches of the TreeNode's Watch; load all DataMember child watches
    ///asyncronously in parallel. If the TreeNode's Watch is a CallMember and it's value has not
    ///yet been loaded and displayed (via previous selection), asycronously load and display it's value,
    ///and then load all of it's children as normal.
    let afterExpand (node:TreeNode) =
        match node with
        | Watch(watch) when hasDummyChild node -> //need to harden this check for loaded vs. not            
            let loadWatches context (node:TreeNode) (watch:Watch) =
                this.BeginUpdate()
                node.Nodes.Clear() //clear dummy node
                let createWatchTreeNode = createWatchTreeNode context
                let asyncNodes = [| 
                    for (tn, a) in watch.Children |> Seq.map createWatchTreeNode do
                        node.Nodes.Add(tn) |> ignore
                        match a with
                        | Some(a) -> yield a
                        | _ -> () |]
                this.EndUpdate()
                //N.B. deliberately excluding Asyn.Start pipe-line from begin/end update 
                //so child nodes have chance to expand before parallel updates start kicking off
                asyncNodes
                |> Async.Parallel 
                |> Async.Ignore
                |> Async.Start

            let guiContext = System.Threading.SynchronizationContext.Current //gui thread
            match watch with
            | CallMember(info) when info.LazyMemberValue.IsValueCreated |> not (* not i.e. already loaded via after select event *) ->
                node.Nodes.Clear()
                node.Text <- info.LoadingText
                async {
                    let original = System.Threading.SynchronizationContext.Current
                    do! loadWatchAsync guiContext node info.LazyMemberValue false
                    do! Async.SwitchToContext guiContext
                    do loadWatches guiContext node watch
                    do! Async.SwitchToContext original
                } |> Async.Start
            | _ -> 
                loadWatches guiContext node watch
        | _ -> () //either an Archive node or IWatchNode children already expanded

    do
        this.NodeMouseClick.Add <| fun args ->
            if args.Button = MouseButtons.Right then
                this.SelectedNode <- args.Node //right click causing node to become selected is std. windows behavior
                let nodeContextMenu = createNodeContextMenu args.Node
                if nodeContextMenu.MenuItems.Count > 0 then
                    nodeContextMenu.Show(this, args.Location)
            else ()

        this.AfterSelect.Add (fun args -> afterSelect args.Node)
        this.AfterExpand.Add (fun args -> afterExpand args.Node)
        
        this.ImageList <- 
            let il = new ImageList()
            il.TransparentColor <- System.Drawing.Color.Magenta

            for ir in ImageResource.WatchImages do 
                il.Images.Add(ir.Name, ir.Image)

            il
    with
        ///Initialize an instance of a WatchTreeView without a PluginManager (e.g. when a WatchTreeView is used as the basis for a plugin!).
        new() = new WatchTreeView(None)

        member private this.UpdateWatch(tn:TreeNode, value, ty) =
            Control.update this <| fun () ->
                let watch = createRootWatch tn.Name value ty
                tn.Text <- watch.DefaultText
                tn.Tag <- watch
                tn.Nodes.Clear()
                tn.Nodes.Add(dummyText) |> ignore
                tn.Collapse()

        member private this.AddWatch(name, value:obj, ty) =
            Control.update this <| fun () ->
                createRootWatch name value ty
                |> createWatchTreeNode null //don't like passing null here...
                |> fst
                |> this.Nodes.Add
                |> ignore

        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value:obj, ty) =
            let objNode =
                this.Nodes
                |> Seq.cast<TreeNode>
                |> Seq.tryFind (fun tn -> tn.Name = name)

            match objNode with
            | Some(Watch(Root({ValueInfo=vi})) as tn) when vi.Value <>& value -> 
                this.UpdateWatch(tn, value, ty)
            | None -> this.AddWatch(name, value, ty)
            | _ -> ()

        ///Add or update a watch with the given name and value (where the type is derived from the type paramater of the value).
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
                let nodesToArchiveCloned = [| 
                    for tn in this.Nodes do
                        if tn |> isWatch then
                            yield tn.Clone() :?> TreeNode |]
            
                this.ClearAll isWatch

                let archiveNode = TreeNode(Text=label)
                archiveNode.Nodes.AddRange(nodesToArchiveCloned)
                this.Nodes.Add(archiveNode) |> ignore
                archiveCounter <- archiveCounter + 1

        ///Take archival snap shot of all current watches using a default label based on an archive count.
        member this.Archive() = 
            this.Archive(sprintf "Archive (%i)" archiveCounter)

        ///Clear all archives and reset the archive count.
        member this.ClearArchives() =
            this.ClearAll isArchive
            archiveCounter <- 0

        ///Clear all watches (doesn't include archive nodes).
        member this.ClearWatches() =
            this.ClearAll isWatch

        ///Clear all archives (reseting archive count) and watches.
        member this.ClearAll() =
            this.Nodes.Clear()
            archiveCounter <- 0