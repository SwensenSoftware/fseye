namespace Swensen.FsEye.Plugins

open Swensen.FsEye
open Swensen.FsEye.Forms
open System.Windows.Forms

type TreeViewWatchViewer() =
    let watchTreeView = new WatchTreeView()
    interface IWatchViewer with
        member this.Watch(label, value, ty) =
            watchTreeView.Watch(label, value, ty)
            
        member this.Control = watchTreeView :> Control

type TreeViewPlugin() =
    interface IPlugin with
        member this.Version = "1.0"
        member this.Name = "Tree View"
        member this.CreateWatchViewer() = new TreeViewWatchViewer() :> IWatchViewer