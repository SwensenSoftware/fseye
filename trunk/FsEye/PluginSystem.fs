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
namespace Swensen.FsEye
open System.Windows.Forms
open System.Reflection
open System.IO
open System
open Microsoft.FSharp.Collections

///Specifies a watch viewer interface, an instance which can add or update one or more watches with 
///a custom watch viewer control
type IWatchViewer =
    ///Add or update a watch with the given label, value, and type. Note: you can choose to 
    ///disregard the label and type if desired, but will almost certainly need the value.
    abstract Watch : string * obj * System.Type -> unit
    ///The underlying watch viewer control. Exists as a property of IWatchViewer 
    ///since you may or may not own the control (i.e. you cannot directly implement IWatchViewer on the control).
    abstract Control : Control

///Specificies a watch view plugin, capable of creating watch viewer instances
type IPlugin =    
    //The name of the plugin
    abstract Name : string
    ///Create an instance of this plugin's watch viewer
    abstract CreateWatchViewer : unit -> IWatchViewer
    ///Returns true or false depending on whether the given instance and its type (which we may need if 
    ///the instance is null) are watchable: if false, then FsEye will not allow creating a watch for a value 
    ///of the given type. Plugin authors should be mindful of the performance impact this method may have.
    abstract IsWatchable : obj * Type -> bool

///Represents a plugin watch viewer being managed by the PluginManager
type ManagedWatchViewer = {
    ///The unique ID of the watch viewer instance 
    ID:string
    ///The watch viewer instance which is being managed
    WatchViewer:IWatchViewer
    ///The owning ManagedPlugin
    ManagedPlugin:ManagedPlugin
}

///Represents a plugin being managed by the PluginManager
and ManagedPlugin = {
    ///The plugin being managed
    Plugin:IPlugin
    ///The owning plugin manager
    PluginManager:PluginManager
}
    with
        ///The list of active watch viewers (i.e. watch viewers may be added and removed by the plugin manager)
        member this.ManagedWatchViewers = this.PluginManager.ManagedWatchViewers |> Seq.filter (fun (x:ManagedWatchViewer) -> x.ManagedPlugin = this)

///Manages FsEye watch viewer plugins
and PluginManager(?scanForPlugins:bool) as this =
    let scanForPlugins = defaultArg scanForPlugins true
    let watchAdded = new Event<ManagedWatchViewer>()
    let watchUpdated = new Event<ManagedWatchViewer>()
    let watchRemoved = new Event<ManagedWatchViewer>()

    let showErrorDialog (text:string) (caption:string) =
        MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore

    let showPluginErrorDialog text = showErrorDialog text "FsEye Plugin Loading Error"
    
    let managedPlugins = 
        if scanForPlugins then
            try
                let executingAsm = Assembly.GetExecutingAssembly()
                let executingDir = Path.GetDirectoryName(executingAsm.Location)
                let pluginDir = sprintf "%s%cplugins" executingDir Path.DirectorySeparatorChar
                let assemblyExclude = ["FsEye.dll"] //maybe exclude all of executingAsm.GetReferencedAssemblies() as well.

                let pluginSeq =
                    [pluginDir; executingDir]
                    |> Seq.filter Directory.Exists
                    |> Seq.collect (fun pluginDir ->
                        Directory.GetFiles(pluginDir)
                        |> Seq.filter(fun assemblyFile -> 
                            assemblyFile.EndsWith(".dll") && assemblyExclude |> List.exists (fun exclude -> assemblyFile.EndsWith(exclude)) |> not)
                        //Issue 36: need to use Assembly.UnsafeLoadFrom to avoid plugin loading errors
                        |> Seq.map (fun assemblyFile -> Assembly.UnsafeLoadFrom(assemblyFile))
                        |> Seq.collect (fun assembly -> 
                            try
                                assembly.GetTypes()    
                            with
                            | :? ReflectionTypeLoadException as x ->
                                for le in x.LoaderExceptions do
                                    let msg = sprintf "Error loading types in assembly '%s'.\n\n%s" assembly.FullName le.Message
                                    showPluginErrorDialog msg
                                [||]
                        )
                        |> Seq.filter (fun ty -> typeof<IPlugin>.IsAssignableFrom(ty))
                        |> Seq.map (fun pluginTy -> 
                            let plugin = Activator.CreateInstance(pluginTy) :?> IPlugin
                            {Plugin=plugin; PluginManager=this}))
                ResizeArray(pluginSeq)
            with
            | x ->
                showPluginErrorDialog x.Message
                ResizeArray()
        else
            ResizeArray()
                                        
    let managedWatchViewers = ResizeArray<ManagedWatchViewer>()

    ///absolute number of watch viewer instances created for the given plugin
    let absoluteCounts = 
        let dic = new System.Collections.Generic.Dictionary<ManagedPlugin, int>()
        for plugin in managedPlugins do
            dic.[plugin] <- 0

        dic

    [<CLIEvent>]
    member __.WatchAdded = watchAdded.Publish
    [<CLIEvent>]
    member __.WatchUpdated = watchUpdated.Publish
    [<CLIEvent>]
    member __.WatchRemoved = watchRemoved.Publish
    
    member __.ManagedPlugins = managedPlugins |> Seq.readonly
    member __.ManagedWatchViewers = managedWatchViewers |> Seq.readonly

    ///Create a new watch viewer for the given managed plugin, sending the given label, value and type.
    ///Returns the ManagedWatchViewer which wraps the created watch viewer.
    member this.SendTo(managedPlugin:ManagedPlugin, label: string, value: obj, valueTy:System.Type) =
        //create the new watch viewer
        let watchViewer = managedPlugin.Plugin.CreateWatchViewer()
        watchViewer.Watch(label, value, valueTy)

        //create the container control
        let id = 
            let next = absoluteCounts.[managedPlugin] + 1
            absoluteCounts.[managedPlugin] <- next
            sprintf "%s %i" managedPlugin.Plugin.Name next
            
        
        //create the managed watch viewer and add it to this managed plugin's collection
        let mwv = {ID=id;WatchViewer=watchViewer;ManagedPlugin=managedPlugin}
        managedWatchViewers.Add(mwv)
            
        watchAdded.Trigger(mwv)
        mwv

    ///Send the given label, value and type to the given, existing managed watch viewer.
    member this.SendTo(mwv:ManagedWatchViewer, label: string, value: obj, valueTy:System.Type) =
        mwv.WatchViewer.Watch(label, value, valueTy)
        
        watchUpdated.Trigger(mwv)

    ///Remove the given managed watch viewer, disposing the watch viewer's Control.
    member this.RemoveManagedWatchViewer(mwv) =
        mwv.WatchViewer.Control.Dispose()
        managedWatchViewers.Remove(mwv) |> ignore
        watchRemoved.Trigger(mwv)

    ///Remove the managed watch viewer by id, disposing the watch viewer's Control.
    member this.RemoveManagedWatchViewer(id:string) =
        let mwv = managedWatchViewers |> Seq.find(fun x -> x.ID = id)
        this.RemoveManagedWatchViewer(mwv)

    ///Remove the given managed plugin (and all of its managed watch viewers).
    member this.RemoveManagedPlugin(mp) =
        managedWatchViewers 
        |> Seq.filter (fun x -> x.ManagedPlugin = mp) 
        |> Seq.toList
        |> Seq.iter (fun x -> this.RemoveManagedWatchViewer(x))

        absoluteCounts.Remove(mp) |> ignore
        managedPlugins.Remove(mp) |> ignore

    ///Remove the managed plugin (and all of its managed watch viewers) by name.
    member this.RemoveManagedPlugin(name:string) =
        let mp = managedPlugins |> Seq.find(fun x -> x.Plugin.Name = name)
        this.RemoveManagedPlugin(mp)

    ///Register the given plugin and return the managed plugin wrapping it. If a managed plugin wrapping a plugin of the same name exists, 
    ///removes it (and all of its associated managed watch viewers).
    member this.RegisterPlugin(plugin:IPlugin) =
        match managedPlugins |> Seq.tryFind (fun x -> x.Plugin.Name = plugin.Name) with
        | Some(old_mp) -> this.RemoveManagedPlugin(old_mp)
        | None -> ()
             
        let mp = {Plugin=plugin; PluginManager=this}
        managedPlugins.Add(mp)
        absoluteCounts.[mp] <- 0
        mp