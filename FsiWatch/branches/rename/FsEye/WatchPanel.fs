﻿namespace Swensen.FsEye.Forms
open System.Windows.Forms
open System.Reflection

type WatchPanel() as this =
    inherit Panel()
    let treeView = new WatchTreeView(Dock=DockStyle.Fill)
    let continueButton = new Button(Text="Async Continue", AutoSize=true, Enabled=false)
    let asyncBreak = async {
        let! _ = Async.AwaitEvent continueButton.Click
        ()
    }

    do        
        //must tree view (with dockstyle fill) first in order for it to be flush with button panel
        //see: http://www.pcreview.co.uk/forums/setting-control-dock-fill-you-have-menustrip-t3240577.html
        this.Controls.Add(treeView)
        (
            let buttonPanel = new FlowLayoutPanel(Dock=DockStyle.Top, AutoSize=true)
            (
                let archiveButton = new Button(Text="Archive Watches", AutoSize=true)
                archiveButton.Click.Add(fun _ -> this.Archive()) 
                buttonPanel.Controls.Add(archiveButton)
            )
            (
                let clearButton = new Button(Text="Clear Archives", AutoSize=true)
                clearButton.Click.Add(fun _ -> this.ClearArchives() ) 
                buttonPanel.Controls.Add(clearButton)
            )
            (
                let clearButton = new Button(Text="Clear Watches", AutoSize=true)
                clearButton.Click.Add(fun _ -> this.ClearWatches()) 
                buttonPanel.Controls.Add(clearButton)
            )
            (
                let clearButton = new Button(Text="Clear All", AutoSize=true)
                clearButton.Click.Add(fun _ -> this.ClearAll()) 
                buttonPanel.Controls.Add(clearButton)
            )
            (
                continueButton.Click.Add(fun _ -> continueButton.Enabled <- false)
                buttonPanel.Controls.Add(continueButton)
            )
            this.Controls.Add(buttonPanel)
        )
    with
        //a lot of delegation to treeView below -- not sure how to do this better

        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value, ty) =
            treeView.Watch(name, value, ty)

        ///Add or update a watch with the given name and value.
        member this.Watch(name, value) =
            treeView.Watch(name,value)

        ///Take archival snap shot of all current watches using the given label.
        member this.Archive(label: string) =
            treeView.Archive(label)

        ///Take archival snap shot of all current watches using a default label based on an archive count.
        member this.Archive() = 
            treeView.Archive()

        ///Clear all archives and reset the archive count.        
        member this.ClearArchives() = 
            treeView.ClearArchives()

        ///Clear all watches (doesn't include archive nodes).
        member this.ClearWatches() = 
            treeView.ClearWatches()

        ///Clear all archives and watches.
        member this.ClearAll() = 
            treeView.Nodes.Clear()

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

        member this.AsyncContinue() =
            //the Click event for continueButton.PerformClick() doesn't fire when form is closed
            //but it does fire using InvokeOnClick
            this.InvokeOnClick(continueButton, System.EventArgs.Empty)