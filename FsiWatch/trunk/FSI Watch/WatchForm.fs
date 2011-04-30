﻿namespace Swensen.Watch.Forms
open System.Windows.Forms
open System.Reflection

type WatchForm() as this =
    inherit Form(
        Text="Stephen Swensen's FSI Watch", 
        Size = (
            let size = SystemInformation.PrimaryMonitorSize
            System.Drawing.Size((2 * size.Width) / 3, size.Height / 2)
        )
    )
    let treeView = new WatchTreeView(Dock=DockStyle.Fill)
    let continueButton = new Button(Text="Async Continue", AutoSize=true, Enabled=false)
    let asyncBreak = async {
        let! _ = Async.AwaitEvent continueButton.Click
        ()
    }

    do        
        ///prevent form from disposing when closing
        this.Closing.Add(fun args -> args.Cancel <- true ; this.Hide())
        //must tree view (with dockstyle fill) first in order for it to be flush with button panel
        //see: http://www.pcreview.co.uk/forums/setting-control-dock-fill-you-have-menustrip-t3240577.html
        this.Controls.Add(treeView)
        (
            let buttonPanel = new FlowLayoutPanel(Dock=DockStyle.Top, AutoSize=true)
            (
                let archiveButton = new Button(Text="Archive Watches", AutoSize=true)
                archiveButton.Click.Add(fun _ -> treeView.Archive()) 
                buttonPanel.Controls.Add(archiveButton)
            )
            (
                let clearButton = new Button(Text="Clear Archives", AutoSize=true)
                clearButton.Click.Add(fun _ -> treeView.ClearArchives() ) 
                buttonPanel.Controls.Add(clearButton)
            )
            (
                let clearButton = new Button(Text="Clear Watches", AutoSize=true)
                clearButton.Click.Add(fun _ -> treeView.ClearWatches()) 
                buttonPanel.Controls.Add(clearButton)
            )
            (
                let clearButton = new Button(Text="Clear All", AutoSize=true)
                clearButton.Click.Add(fun _ -> treeView.Nodes.Clear()) 
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

        ///Add or update a watch with the given name.
        member this.Watch(name, value, ty) =
            treeView.Watch(name, value, ty)

        ///Add or update a watch with the given name and value, determine the type if not null.
        member this.Watch(name: string, value) =
            treeView.Watch(name,value)

        ///Add or update all the elements in the sequence by name and value, determine null type if not null.
        member this.Watch(watchList:seq<string * obj>) =
            treeView.Watch(watchList)

        ///Add or update all the elements in the sequence by name, value, and type.
        member this.Watch(watchList:seq<string * obj * System.Type>) =
            treeView.Watch(watchList)

        ///take archival snap shot of all current watches
        member this.Archive(label: string) =
            treeView.Archive(label)

        ///take archival snap shot of all current watches with a default label
        member this.Archive() = 
            treeView.Archive()

        member this.ClearArchives() = 
            treeView.ClearArchives()

        member this.ClearWatches() = 
            treeView.ClearWatches()

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
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;do! watch.Break()</para>
        ///<para>} |> Async.StartImmediate</para>
        ///</summary>
        member this.AsyncBreak() =
            continueButton.Enabled <- true
            if this.Visible |> not then
                this.Show()

            this.Activate()

            asyncBreak

        member this.AsyncContinue() =
            //the Click event for continueButton.PerformClick() doesn't fire when form is closed
            //but it does fire using InvokeOnClick
            this.InvokeOnClick(continueButton, System.EventArgs.Empty)