namespace Swensen.Watch.Forms
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
    do
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
                let clearButton = new Button(Text="Clear Watches", AutoSize=true)
                clearButton.Click.Add(fun _ -> treeView.ClearWatches()) 
                buttonPanel.Controls.Add(clearButton)
            )
            (
                let clearButton = new Button(Text="Clear All", AutoSize=true)
                clearButton.Click.Add(fun _ -> treeView.Nodes.Clear()) 
                buttonPanel.Controls.Add(clearButton)
            )
            this.Controls.Add(buttonPanel)
        )
    with
        ///Add or update a watch with the given name.
        member this.Watch(name, value, ty) =
            treeView.Watch(name, value, ty)

        ///Add or update all the elements in the sequence by name.
        member this.Watch(watchList) =
            treeView.Watch watchList

        ///take archival snap shot of all current watches
        member this.Archive(label: string) =
            treeView.Archive(label)

        ///take archival snap shot of all current watches with a default label
        member this.Archive() = 
            treeView.Archive()

            
