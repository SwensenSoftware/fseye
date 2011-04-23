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
        member this.Watch(name: string, tag:obj) =
            treeView.Watch(name, tag)

        ///Add or update all the elements in the sequence by name.
        member this.Watch(watchList: seq<string * obj>) =
            treeView.Watch watchList