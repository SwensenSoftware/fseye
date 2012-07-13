﻿(*
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

//todo: rename IEye?
///Specifies a watch viewer interface, an instance which can add or update one or more watches with 
///a custom watch viewer control
type IWatchViewer =
    ///Add or update a watch with the given label, value, and type. Note: you can choose to 
    ///disregard the label and type if desired, but will almost certainly need the value.
    abstract Watch : string * 'a * System.Type -> unit
    ///The underlying watch viewer control. Exists as a property of IWatchViewer 
    ///since you may or may not own the control (i.e. you cannot directly implement IWatchViewer on the control).
    abstract Control : Control

///Specificies a watch view plugin, capable of creating watch viewer instances
type IPlugin =    
    //The name of the plugin
    abstract Name : string
    ///Create an instance of this plugin's watch viewer
    abstract CreateWatchViewer : unit -> IWatchViewer
    ///Returns true or false depending on whether an instance of the given type is watchable: if false,
    ///then FsEye will not allow creating a watch for a value of the given type.
    abstract IsWatchable : Type -> bool

///Represents a plugin watch viewer being managed by the PluginManager
type ManagedWatchViewer = {
    ///The unique ID of the watch viewer instance 
    ID:string
    ///The watch viewer instance which is being managed
    WatchViewer:IWatchViewer
    ///The owning ManagedPlugin
    ManagedPlugin:ManagedPlugin
}

//todo: refactor so we don't expose mutable properties and methods that could result in invalid state.
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
and PluginManager(tabControl:TabControl) as this =
    let watchAdded = new Event<ManagedWatchViewer>()
    let watchUpdated = new Event<ManagedWatchViewer>()
    let watchRemoved = new Event<ManagedWatchViewer>()

    let showErrorDialog (owner:IWin32Window) (text:string) (caption:string) =
        MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
    
    let managedPlugins = 
        try
            let pluginDir = sprintf "%s%cplugins" (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) Path.DirectorySeparatorChar
            if Directory.Exists(pluginDir) then
                Directory.GetFiles(pluginDir)
                |> Seq.map (fun assemblyFile -> Assembly.LoadFile(assemblyFile))
                |> Seq.collect (fun assembly -> assembly.GetTypes())
                |> Seq.filter (fun ty -> typeof<IPlugin>.IsAssignableFrom(ty))
                |> Seq.map (fun pluginTy -> Activator.CreateInstance(pluginTy) :?> IPlugin)
                |> Seq.map (fun plugin -> {Plugin=plugin; PluginManager=this})
                |> Seq.toList
            else
                []
        with x ->
            showErrorDialog tabControl.TopLevelControl x.Message "FsEye Plugin Loading Error"
            []
                                        
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
    
    member __.ManagedPlugins = managedPlugins
    member __.ManagedWatchViewers = managedWatchViewers |> Seq.readonly

    ///Create a new watch viewer for the given managed plugin, sending the given label, value and type
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
        let managedWatchViewer = {ID=id;WatchViewer=watchViewer;ManagedPlugin=managedPlugin}
        managedWatchViewers.Add(managedWatchViewer)
            
        //display the watch viewer
        let tabPage = new TabPage(id, Name=id)
        let watchViewerControl = managedWatchViewer.WatchViewer.Control
        watchViewerControl.Dock <- DockStyle.Fill
        tabPage.Controls.Add(watchViewerControl)
        tabControl.TabPages.Add(tabPage)
        tabControl.SelectTab(tabPage)

        watchAdded.Trigger(managedWatchViewer)

    ///Send the given label, value and type to the given, existing managed watch viewer.
    member this.SendTo(managedWatchViewer:ManagedWatchViewer, label: string, value: obj, valueTy:System.Type) =
        managedWatchViewer.WatchViewer.Watch(label, value, valueTy)
        tabControl.SelectTab(managedWatchViewer.ID)
        watchUpdated.Trigger(managedWatchViewer)

    ///Remove the managed watch viewer by id
    member this.RemoveManagedWatchViewer(id:string) =
        let mwv = managedWatchViewers |> Seq.find(fun x -> x.ID = id)
        managedWatchViewers.Remove(mwv) |> ignore
        let tab = tabControl.TabPages.[id]
        tab.Dispose() //http://stackoverflow.com/a/1970158/236255
        tabControl.TabPages.Remove(tab)
        watchRemoved.Trigger(mwv)