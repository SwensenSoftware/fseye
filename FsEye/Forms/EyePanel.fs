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

type EyePanel() as this =
    inherit Panel()    
    let continueButton = new Button(Text="Async Continue", AutoSize=true, Enabled=false)
    let asyncBreak = async {
        let! _ = Async.AwaitEvent continueButton.Click
        ()
    }

    let pluginManager = new PluginManager() //todo: only the Eye should own this resource
    let splitContainer = new EyeSplitContainer(pluginManager, Dock=DockStyle.Fill)

    //must add splitContainer (with dockstyle fill) first in order for it to be flush with button panel
    //see: http://www.pcreview.co.uk/forums/setting-control-dock-fill-you-have-menustrip-t3240577.html
    do this.Controls.Add(splitContainer)

    do //build button panel and add it to this panel
        let buttonPanel = new FlowLayoutPanel(Dock=DockStyle.Top, AutoSize=true)
    
        let archiveButton = new Button(Text="Archive Watches", AutoSize=true)
        archiveButton.Click.Add(fun _ -> this.Archive()) 
        buttonPanel.Controls.Add(archiveButton)
    
        let clearButton = new Button(Text="Clear Archives", AutoSize=true)
        clearButton.Click.Add(fun _ -> this.ClearArchives() ) 
        buttonPanel.Controls.Add(clearButton)
    
        let clearButton = new Button(Text="Clear Watches", AutoSize=true)
        clearButton.Click.Add(fun _ -> this.ClearWatches()) 
        buttonPanel.Controls.Add(clearButton)
    
        let clearButton = new Button(Text="Clear All", AutoSize=true)
        clearButton.Click.Add(fun _ -> this.ClearAll()) 
        buttonPanel.Controls.Add(clearButton)
    
        continueButton.Click.Add(fun _ -> continueButton.Enabled <- false)
        buttonPanel.Controls.Add(continueButton)
    
        this.Controls.Add(buttonPanel)
    with
        //a lot of delegation to treeView below -- not sure how to do this better

        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value:obj, ty) =
            splitContainer.TreeView.Watch(name, value, ty)

        ///Add or update a watch with the given name and value (where the type is derived from the type paramater of the value).
        member this.Watch(name, value) =
            splitContainer.TreeView.Watch(name,value)

        ///Take archival snap shot of all current watches using the given label.
        member this.Archive(label: string) =
            splitContainer.TreeView.Archive(label)

        ///Take archival snap shot of all current watches using a default label based on an archive count.
        member this.Archive() = 
            splitContainer.TreeView.Archive()

        ///Clear all archives and reset the archive count.        
        member this.ClearArchives() = 
            splitContainer.TreeView.ClearArchives()

        ///Clear all watches (doesn't include archive nodes).
        member this.ClearWatches() = 
            splitContainer.TreeView.ClearWatches()

        ///Clear all archives (reseting archive count) and watches.
        member this.ClearAll() = 
            splitContainer.TreeView.ClearAll()

        //note: would like to use [<DebuggerBrowsable(DebuggerBrowsableState.Never)>]
        //on the following two Async methods but the attribute is not valid on methods
        //maybe we should introduce our own "MethodDebuggerAttribute"

        ///<summary>
        ///Use this in a sync block with do!, e.g.
        ///<para></para>
        ///<para>async { </para>
        ///<para>&#160;&#160;&#160;&#160;for i in 1..100 do</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;watch.Watch("i", i, typeof&lt;int&gt;)</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;watch.Archive()</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;if i = 50 then</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;do! watch.AsyncBreak()</para>
        ///<para>} |> Async.StartImmediate</para>
        ///</summary>
        member this.AsyncBreak() =
            continueButton.Enabled <- true
            asyncBreak

        ///Continue from an AsyncBreak()
        member this.AsyncContinue() =
            //the Click event for continueButton.PerformClick() doesn't fire when form is closed
            //but it does fire using InvokeOnClick
            this.InvokeOnClick(continueButton, System.EventArgs.Empty)