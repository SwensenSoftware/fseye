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
open Swensen.FsEye

///The tab-based visual surface of the PluginManager. Exposes TabAdded and TabRemoved events
///which are fired when plugin watch view tabs are added and removed (do not directly add / remove
///tab pages, this is initiated via tab right-click context menu calls which do add / remove ops 
///on the tab plugin itself. This decoupling always direct, programatic calls to add / remove
///plugin manager watch views to propagate events that are handled by this and higher up visual components.
type internal PluginTabControl(pluginManager:PluginManager) as this =
    inherit TabControl()

    let tabAdded = new Event<TabPage>()
    let tabRemoved = new Event<TabPage>()

    //wire up tab closing, coordinating with the plugin manager.
    let closeTab (tab:TabPage) = 
        pluginManager.RemoveManagedWatchViewer(tab.Name)

    let closeOtherTabs (tab:TabPage) =
        this.TabPages
        |> Seq.cast<TabPage>
        |> Seq.filter (fun x -> x.Name <> tab.Name)
        |> Seq.map (fun x -> x.Name)
        |> Seq.toList
        |> Seq.iter (fun id -> pluginManager.RemoveManagedWatchViewer(id))

    let closeAllTabs () =
        this.TabPages
        |> Seq.cast<TabPage>
        |> Seq.map (fun x -> x.Name)
        |> Seq.toList
        |> Seq.iter (fun id -> pluginManager.RemoveManagedWatchViewer(id))

    //we may want to have WatchUpdating event and trigger select at that point rather than after
    do pluginManager.WatchUpdated.Add (fun mwv -> 
        this.SelectTab(mwv.ID)
    )

    do pluginManager.WatchAdded.Add (fun mwv -> 
        //display the watch viewer
        let tab = new TabPage(mwv.ID, Name=mwv.ID)
        let wvControl = mwv.WatchViewer.Control
        wvControl.Dock <- DockStyle.Fill
        tab.Controls.Add(wvControl)
        this.TabPages.Add(tab)

        this.SelectTab(tab)
        
        tabAdded.Trigger(tab)
    )
        
    do pluginManager.WatchRemoved.Add (fun mwv ->
        let tab = this.TabPages.[mwv.ID]
        tab.Dispose() //http://stackoverflow.com/a/1970158/236255
        this.TabPages.Remove(tab)
        
        tabRemoved.Trigger(tab)
    )
        
    let createTabContextMenu (tab:TabPage) =
        new ContextMenu [|
            let mi = new MenuItem("Close Tab") 
            mi.Click.Add(fun _ -> closeTab tab) 
            yield mi

            let mi = new MenuItem("Close Other Tabs", Enabled=(this.TabCount>1))
            mi.Click.Add(fun _ -> closeOtherTabs tab)
            yield mi

            let mi = new MenuItem("Close All Tabs") 
            mi.Click.Add(fun _ -> closeAllTabs ()) 
            yield mi
        |]
        
    //show the context menu on right-click
    do this.MouseClick.Add (fun e -> 
        if e.Button = MouseButtons.Right then                                                 
            let clickedTab = 
                this.TabPages 
                |> Seq.cast<TabPage> 
                |> Seq.mapi (fun i tab -> (i,tab)) 
                |> Seq.find (fun (i,tab) -> this.GetTabRect(i).Contains(e.Location))
                |> snd
            (createTabContextMenu clickedTab).Show(this, e.Location)
    )

    [<CLIEvent>]
    ///Fires when a plugin tab has been added (but not if you manually add a tab)
    member __.TabAdded = tabAdded.Publish
    [<CLIEvent>]
    ///Fires when a plugin tab has been removed (but not if you manually add a tab)
    member __.TabRemoved = tabRemoved.Publish