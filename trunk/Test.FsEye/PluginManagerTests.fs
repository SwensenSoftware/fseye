module PluginManagerTests

open Swensen.Unquote
open Xunit

open Swensen.FsEye.WatchModel
open Swensen.FsEye.Forms
open Swensen.FsEye

open Swensen.Utils
open System.Windows.Forms

open System
open System.Reflection
open ImpromptuInterface.FSharp

let sendToLabel(tn:TreeNode) =
    let pm = tn.TreeView?pluginManager?Value
    pm

[<Fact(Skip="not done yet: trying to figure best way create mock plugin")>]
let ``root watch SendTo label value`` () =
    let pm = new PluginManager(new TabControl())
    let plugin = {Plugin=Unchecked.defaultof<IPlugin>;PluginManager=pm}
    pm?managedPlugins

    let tree = new WatchTreeView(Some(pm))
    tree.Watch("watch", [1;2;3;4;5])
    let tn = tree.Nodes.[0]


    test <@ sendToLabel tn <> null @>
