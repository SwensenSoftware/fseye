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
namespace Swensen.FsEye.Forms
open System.Windows.Forms
open System.Reflection
open Swensen.FsEye
open System.Drawing

///A vertical split container which manages the interaction between the left WatchTreeView and the right PluginTabControl.
///When no plugin watch viewers are active, the right panel is hidden, otherwise it is shown, reacting to tabs as they 
///are added and remove. By default the size of each is 50% on each size, but it persists the percentage set by the
///user scaling on resize. This internal component is used by the EyePanel.
type internal EyeSplitContainer(pluginManager:PluginManager) as this =
    inherit SplitContainer(Orientation=Orientation.Vertical, SplitterWidth=6)

    let treeView = new WatchTreeView(Some(pluginManager), Dock=DockStyle.Fill)
    let tabControl = new PluginTabControl(pluginManager, Dock=DockStyle.Fill)

    let hidePanel2 () =
        this.Panel2Collapsed <- true
        this.Panel2.Hide()

    let showPanel2 () =
        this.Panel2Collapsed <- false
        this.Panel2.Show()

    do tabControl.TabAdded.Add(fun _ ->
        if tabControl.TabCount > 0 && this.Panel2Collapsed then
            showPanel2()
    )

    do tabControl.TabRemoved.Add(fun _ ->
        if tabControl.TabCount = 0 && not this.Panel2Collapsed then
            hidePanel2()
    )

    //Auto-update splitter distance to a percentage on resize
    let mutable splitterDistancePercentage = 0.5
    do     
        let inline ensureBounds lower upper num =
            if num > upper then upper
            elif num < lower then lower
            else num
        let updateSplitterDistancePercentage() = 
            if this.Width > 0 then
                splitterDistancePercentage <- 
                    let pct = (float this.SplitterDistance) / (float this.Width)
                    ensureBounds 0. 100. pct
        let updateSplitterDistance() = 
            if this.Width > 0 then
                this.SplitterDistance <- 
                    let dist = int ((float this.Width) * splitterDistancePercentage)
                    ensureBounds this.Panel1MinSize (this.Width - this.Panel2MinSize) dist
        updateSplitterDistance() //since SplitterMoved fires first, need to establish default splitter distance
        this.SplitterMoved.Add(fun _ -> updateSplitterDistancePercentage())
        this.SizeChanged.Add(fun _ -> updateSplitterDistance())

        hidePanel2()

    //build up the component tree
    do
        this.Panel1.Controls.Add(treeView)
        this.Panel2.Controls.Add(tabControl)

    //http://stackoverflow.com/a/10412371/236255 (try to emphasize splitter)
//    do
//        this.Paint.Add(fun e -> 
//            let s = this
//            let top = 5;
//            let bottom = s.Height - 5;
//            let left = s.SplitterDistance;
//            let right = left + s.SplitterWidth - 1;
//            e.Graphics.DrawLine(Pens.Silver, left, top, left, bottom);
//            e.Graphics.DrawLine(Pens.Silver, right, top, right, bottom);
//        )

    ///The left panel control
    member this.TreeView = treeView
    ///The right panel control
    member this.TabControl = tabControl

        
