namespace Swensen.FsEye.Plugins

open Swensen.FsEye
open System.Windows.Forms
open System.Drawing

///A PropertyGrid-based watch viewer
type PropertyGridWatchViewer() =
    let panel = new Panel()
    
    let propGrid = new PropertyGrid(Dock=DockStyle.Fill)
    do
        panel.Controls.Add(propGrid)
    
    let labelPanel = new FlowLayoutPanel(Dock=DockStyle.Top, AutoSize=true, Padding=Padding(0,3,3,5))
    let labelLabel = new Label(Text="Source Expression:", AutoSize=true)
    let expressionLabel = new Label(AutoSize=true)
    do
        labelLabel.Font <- new Font(labelLabel.Font, FontStyle.Bold)
        labelPanel.Controls.Add(labelLabel)

        labelPanel.Controls.Add(expressionLabel)

        panel.Controls.Add(labelPanel)
   
    interface IWatchViewer with 
        ///Set or refresh the PropertyGride SelectedObject with the given value (the label and type are not currently used).
        member this.Watch(label, value, _) =
            expressionLabel.Text <- label
            propGrid.SelectedObject <- value
        ///Get the underlying Control of this watch view
        member this.Control = panel :> Control

///A Plugin that creates PropertyGridWatchViewers
type PropertyGridPlugin() =
    interface IPlugin with
        member __.Name = "Property Grid"
        member __.Version = "1.0"
        ///Create a new instance of a PropertyGridWatchViewer
        member __.CreateWatchViewer() = new PropertyGridWatchViewer() :> IWatchViewer