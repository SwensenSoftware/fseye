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

///Specifies a watch viewer interface, an instance which can add or update one or more watches with 
///a custom watch viewer control
type IWatchViewer =
    ///Add or update a watch with the given name, value, and type.
    abstract Watch : string * 'a * System.Type -> unit
    ///The underlying watch viewer control. Exists as a property of IWatchViewer 
    ///since you may or may not own the control (i.e. you cannot directly implement IWatchViewer on the control).
    abstract Control : Control

///Specificies a watch view plugin, capable of creating watch viewer instances
type IPlugin =    
    //The name of the plugin
    abstract Name : string
    //The version of the plugin
    abstract Version : string
    ///Create an instance of this plugin's watch viewer
    abstract CreateWatchViewer : unit -> IWatchViewer

///Represents a plugin watchviewer being managed by the PluginManager
type ManagedWatchViewer(id:string, watchViewer:IWatchViewer, containerControl:Control) =
    ///The unique ID of the watch viewer instance 
    //todo: (this may be redundant if we just want to use the TabControl.Text, but that may be an implementation detail, and indeed we could implement it as such)
    member __.ID = containerControl.Text
    ///The watch viewer instance which is being managed
    member __.WatchViewer = watchViewer
    ///The tab control containing the watch viewer control
    //todo: consider naming it "Container" so that the implementation detail is not so coupled. indeed, we could make this return Control and cast to known type if we want to support multiple watch viewer container modes)
    member __.ContainerControl = containerControl

///Represents a plugin being managed by the PluginManager
type ManagedPlugin(plugin: IPlugin) =
    ///The absolute number of watch viewer instances which have been created by the plugin manager (i.e. we want to keep incrementing to make unique managed watch viewer ID's even when managed watch viewers may have been removed).
    let mutable curIncrement = 0

    let managedWatchViewers = ResizeArray<ManagedWatchViewer>()
    
    ///The plugin being managed
    member __.Plugin = plugin

    ///The list of active watch viewers (i.e. watch viewers may be added and removed by the plugin manager)
    //todo: would be best if we could avoid having this be an array (i.e. avoid mutability)
    member __.ManagedWatchViewers = managedWatchViewers

    ///To the given target, where None is a new instance and Some(instanceId) indicates an existing instance with the given id,
    ///send the given valueText, value, and valueType.
    member this.SendTo(target: option<string>, valueText: string, value: obj, valueTy:System.Type) =
        match target with
        | None -> //"new"
            //create the new watch viewer
            let watchViewer = plugin.CreateWatchViewer()
            watchViewer.Watch(valueText, value, valueTy)

            //create the container control
            let title = sprintf "%s %i" plugin.Name (curIncrement <- curIncrement + 1 ; curIncrement)
            let containerControl = 
                new Form(Text=title, Size = (let size = SystemInformation.PrimaryMonitorSize in System.Drawing.Size((2 * size.Width) / 3, size.Height / 2)))
            
            //create the managed watch viewer and add it to this managed plugin's collection
            let managedWatchViewer = ManagedWatchViewer(title, watchViewer, containerControl)
            this.ManagedWatchViewers.Add(managedWatchViewer)
            
            //when the managed watch viewer's container control is closed, remove it from this plugin's collection
            containerControl.Closing.Add(fun _ -> this.ManagedWatchViewers.Remove(managedWatchViewer) |> ignore)
            
            //display the watch viewer
            let watchViewerControl = managedWatchViewer.WatchViewer.Control
            watchViewerControl.Dock <- DockStyle.Fill
            containerControl.Controls.Add(watchViewerControl)
            //todo: temp hack
            let mainForm = Application.OpenForms |> Seq.cast<Form> |> Seq.find (fun x -> x.Text = "FsEye v1.0.1 by Stephen Swensen")
            containerControl.Show(mainForm)
            ()
        | Some(targetName) ->
            let targetManagedWatchViewer = this.ManagedWatchViewers |> Seq.find (fun x -> x.ID = targetName)
            targetManagedWatchViewer.WatchViewer.Watch(valueText, value, valueTy)
            targetManagedWatchViewer.WatchViewer.Control.Focus() |> ignore 

///Manages FsEye watch viewer plugins
type PluginManager() =
//    List<Assembly> allAssemblies = new List<Assembly>();
//    string path = Assembly.GetExecutingAssembly().Location;
//
//    foreach (string dll in Directory.GetFiles(path, "*.dll"))
//        allAssemblies.Add(Assembly.LoadFile(dll));
    let managedPlugins = 
        let pluginDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof<PluginManager>).Location) + "\\" + "plugins"
        Directory.GetFiles(pluginDir)
        |> Seq.map (fun assemblyFile -> Assembly.LoadFile(assemblyFile))
        |> Seq.collect (fun assembly -> assembly.GetTypes())
        |> Seq.filter (fun ty -> ty.GetInterface("Swensen.FsEye.Plugins.IPlugin") <> null)
        |> Seq.map (fun pluginTy -> Activator.CreateInstance(pluginTy) :?> IPlugin)
        |> Seq.map (fun plugin -> ManagedPlugin(plugin))
        |> Seq.toList
                                        
    member this.ManagedPlugins = managedPlugins