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

type IWatchView =
    ///Add or update a watch with the given name, value, and type.
    abstract Watch : string * 'a * System.Type -> unit
    ///Add or update a watch with the given name, and value.
    abstract Watch : string * 'a -> unit
    ///Get the underlying Control of this watch view
    abstract AsControl : unit -> Control

type IWatchViewPlugin =
    ///display text * object value -> watch view control
    abstract CreateWatchView: unit -> IWatchView
    abstract Name : string

type WatchPropertyGrid() =
    inherit PropertyGrid()
    interface IWatchView with
        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value, ty) =
            this.SelectedObject <- value

        ///Add or update a watch with the given name and value.
        member this.Watch(name: string, value: 'a) =
            this.SelectedObject <- value
            
        ///Get the underlying Control of this watch view
        member this.AsControl() = this :> Control

type PropertyGridPlugin() =
    interface IWatchViewPlugin with
        member this.Name = "Property Grid"
        member this.CreateWatchView() = new WatchPropertyGrid() :> IWatchView

type PluginTracking = { Plugin: IWatchViewPlugin; WatchViewInstances : Map<string,IWatchView>}

type SendToTarget =
    | New
    | Existing of string

type PluginManager() =
    //todo: load dynamically from settings (possibly load all, even those that are disabled,
    //supposing they are enabled / disabled during the life of an FsEye session)
    let plugins = [System.Activator.CreateInstance(System.Type.GetType("Swensen.FsEye.Forms.TreeViewPlugin")) :?> IWatchViewPlugin; PropertyGridPlugin() :> IWatchViewPlugin]

    let mutable pluginTracking =
        plugins
        |> List.map (fun x -> (x.Name, {Plugin=x; WatchViewInstances=Map.empty})) 
        |> Map.ofList
    
    //rmember to set x.Dock<-DockStyle.Fill on controls when we create them
    
    member this.Plugins = plugins
    member this.PluginTracking = pluginTracking
    member this.SendTo(pluginName:string, target: SendToTarget, displayText: string, value: obj) =
        let pt = pluginTracking |> Map.find pluginName
        match target with
        | New ->
            //todo: we may not want to reuse count numbers when e.g. a plugin view is close, in which case we need to keep that count on the PluginTracking object
            //todo: we need this on pt itself for showing in the context menue
            let title = sprintf "%s %i" pt.Plugin.Name (pt.WatchViewInstances.Count + 1) 
            let container = 
                new Form(Text=title, Size = (let size = SystemInformation.PrimaryMonitorSize
                                             System.Drawing.Size((2 * size.Width) / 3, size.Height / 2)))
            container.Closing.Add <| fun _ -> 
                //the price we pay for keep an immutable interface.
                pluginTracking <- 
                    pluginTracking 
                    //|> Map.remove pluginName
                    |> Map.add pluginName { pt with WatchViewInstances=pt.WatchViewInstances |> Map.remove container.Text }
            
            let watchView = pt.Plugin.CreateWatchView()
            pluginTracking <- 
                pluginTracking 
                //|> Map.remove pluginName
                |> Map.add pluginName { pt with WatchViewInstances=pt.WatchViewInstances |> Map.add container.Text watchView}

            watchView.Watch(displayText, value)
            let watchViewControl = watchView.AsControl()
            watchViewControl.Dock <- DockStyle.Fill
            container.Controls.Add(watchView.AsControl())
            //todo: temp hack
            let mainForm = Application.OpenForms |> Seq.cast<Form> |> Seq.find (fun x -> x.Text = "FsEye v1.0.1 by Stephen Swensen")
            container.Show(mainForm)
            ()
        | Existing(targetName) ->
            let t = pt.WatchViewInstances |> Map.find targetName
            t.Watch(displayText, value)
            t.AsControl().Focus() |> ignore //experimental