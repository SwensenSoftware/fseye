namespace Swensen.Watch.Forms
open System.Windows.Forms
open System.Reflection

type WatchForm() as this =
    inherit Form()
    let treeView = new WatchTreeView()
    do
        let title = "FSI Watch"
        this.Name <- title
        this.Text <- title
        let size = SystemInformation.PrimaryMonitorSize
        this.Size <- System.Drawing.Size((2 * size.Width) / 3, size.Height / 2)

        treeView.Dock <- DockStyle.Fill
        this.Controls.Add treeView
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