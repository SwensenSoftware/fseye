namespace Swensen.FsEye.Plugins

open Swensen.FsEye
open System.Windows.Forms

///A PropertyGrid-based watch viewer
type PropertyGridWatchViewer() =
    inherit PropertyGrid()
    interface IWatchViewer with
        ///Set or refresh the selected object with the given value (the name and tpe are not used).
        member this.Watch(_, value, _) =
            this.SelectedObject <- value
        ///Get the underlying Control of this watch view
        member this.Control = this :> Control

///A Plugin that creates PropertyGridWatchViewers
type PropertyGridPlugin() =
    interface IPlugin with
        member __.Name = "Property Grid"
        member __.Version = "1.0"
        ///Create a new instance of a PropertyGridWatchViewer
        member __.CreateWatchViewer() = new PropertyGridWatchViewer() :> IWatchViewer