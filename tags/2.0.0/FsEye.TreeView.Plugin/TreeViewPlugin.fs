namespace Swensen.FsEye.Plugins

open System
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
        ///"Tree View"
        member this.Name = "Tree View"
        ///Creates and returns a new instance of a TreeViewWatchViewer
        member this.CreateWatchViewer() = new TreeViewWatchViewer() :> IWatchViewer
        ///Always returns true.
        member this.IsWatchable(value:obj, ty:Type) = true