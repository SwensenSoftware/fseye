namespace Swensen.FsEye.Plugins

open System
open Swensen.FsEye
open System.Windows.Forms
open System.Drawing

//todo:share common code between this plugin and the PropertyGrid plugin

module private Helpers =
    //see http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.datasource.aspx
    let ValidDataSourceTypes = [
        typeof<System.Collections.IList>
        typeof<System.ComponentModel.IListSource>
        typeof<System.ComponentModel.IBindingList>
        typeof<System.ComponentModel.IBindingListView>]

///A DataGridView-based watch viewer
type DataGridViewWatchViewer() =
    let panel = new Panel()
    
    let dgv = new DataGridView(Dock=DockStyle.Fill)
    do
        panel.Controls.Add(dgv)
    
    let labelPanel = new FlowLayoutPanel(Dock=DockStyle.Top, AutoSize=true, Padding=Padding(0,3,3,5))
    let labelLabel = new Label(Text="Source Expression:", AutoSize=true)
    let expressionLabel = new Label(AutoSize=true)
    do
        labelLabel.Font <- new Font(labelLabel.Font, FontStyle.Bold)
        labelPanel.Controls.Add(labelLabel)

        labelPanel.Controls.Add(expressionLabel)

        panel.Controls.Add(labelPanel)
   
    interface IWatchViewer with 
        ///Set or refresh the DataGridView DataSource with the given value.
        member this.Watch(label, value, ty) =
            expressionLabel.Text <- label
//            let value =
//                if not (Helpers.ValidDataSourceTypes |> List.exists (fun validTy -> validTy.IsAssignableFrom(ty))) then //must be non-generic IEnumerable
//                    value :?> System.Collections.IEnumerable |> Seq.cast<obj> |> Seq.toArray :> obj //arrays implement IList
//                else    
//                    value
            dgv.DataSource <- value
        ///Get the underlying Control of this watch view
        member this.Control = panel :> Control

///A Plugin that creates DataGridViewWatchViewers
type DataGridViewPlugin() =
    interface IPlugin with
        ///"Property Grid"
        member __.Name = "Data Grid"
        ///Create a new instance of a DataGridViewWatchViewer
        member __.CreateWatchViewer() = new DataGridViewWatchViewer() :> IWatchViewer
        ///Returns true if and only if the given value is not null and the given type implements one of the interfaces supported by DataGridView.DataSource.
        member this.IsWatchable(value:obj, ty:Type) =
            //we convert non-generic IEnumerables to ILists for convenience
            //let validTys = typeof<System.Collections.IEnumerable> :: Helpers.ValidDataSourceTypes
            let validTys = Helpers.ValidDataSourceTypes
            value <> null && (validTys |> List.exists (fun validTy -> validTy.IsAssignableFrom(ty)))