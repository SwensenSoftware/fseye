namespace Swensen.FsEye.Plugins

open Swensen.FsEye
open Swensen.FsEye.Forms
open System.Windows.Forms

type TreeViewWatchViewer() =
    let watchTreeView = new WatchTreeView()
    interface IWatchViewer with
        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value, ty) =
            watchTreeView.Watch(name, value, ty)
            
        ///Get the underlying Control of this watch view
        member this.Control = watchTreeView :> Control

type TreeViewPlugin() =
    interface IPlugin with
        member this.Version = "1.0"
        member this.Name = "Tree View"
        member this.CreateWatchViewer() = new TreeViewWatchViewer() :> IWatchViewer