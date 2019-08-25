module PluginManagerTests

open Swensen.Unquote
open Xunit

open Swensen.FsEye.WatchModel
open Swensen.FsEye.Forms
open Swensen.FsEye
open Swensen.FsEye.Plugins

open Swensen.Utils
open System.Windows.Forms

open System
open System.Reflection
//open ImpromptuInterface.FSharp

let mkPlugin name mkControl =
    {
        new IPlugin with
            member __.Name = name
            member __.IsWatchable(_,_) = true
            member __.CreateWatchViewer() =
                { 
                    new IWatchViewer with
                        member __.Watch(_,_,_) = ()
                        member __.Control = mkControl()
                }
    }

let mkControl = fun () -> new Control()

[<Fact>]
let ``SendTo plugin creates watch viewers with absolute count ids`` () =
    let plugin = mkPlugin "Plugin" mkControl
    let pm = new PluginManager(false)
    let mp = pm.RegisterPlugin(plugin)
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 1" @>
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 2" @>
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 3" @>
    pm.RemoveManagedWatchViewer("Plugin 3")
    test <@ pm.SendTo(mp, "", null, typeof<obj>).ID = "Plugin 4" @>

[<Fact>]
let ``removing a plugin removes its watch viewers`` () =
    let pluginA = mkPlugin "PluginA" mkControl
    let pluginB = mkPlugin "PluginB" mkControl

    let pm = new PluginManager(false)
    let mpA = pm.RegisterPlugin(pluginA)
    let mpB = pm.RegisterPlugin(pluginB)
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpB, "", null, typeof<obj>) |> ignore

    pm.RemoveManagedPlugin(mpB)

    test <@ pm.ManagedWatchViewers |> Seq.map (fun x -> x.ID) |> Seq.toList = ["PluginA 1"; "PluginA 2"]  @>

[<Fact>]
let ``registering a plugin of the same name removes the previous version of the plugin`` () =
    let pluginA = mkPlugin "PluginA" mkControl
    let pluginB = mkPlugin "PluginB" mkControl

    let pm = new PluginManager(false)
    let mpA = pm.RegisterPlugin(pluginA)
    let mpB = pm.RegisterPlugin(pluginB)
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpB, "", null, typeof<obj>) |> ignore

    let pluginB' = mkPlugin "PluginB" mkControl
    let mpB' = pm.RegisterPlugin(pluginB')

    ///i.e. all of the original PluginB watch viewers were removed when the new version of registered
    test <@ pm.ManagedWatchViewers |> Seq.map (fun x -> x.ID) |> Seq.toList = ["PluginA 1"; "PluginA 2"]  @>

    test <@ pm.ManagedPlugins |> Seq.map (fun x -> x.Plugin) |> Seq.toList = [pluginA; pluginB']  @>

[<Fact>]
let ``removing a managed watch viewer disposes of its control`` () =
    let control = new Control()
    let pluginA = mkPlugin "PluginA" (fun () -> control)

    let pm = new PluginManager(false)
    let mpA = pm.RegisterPlugin(pluginA)
    let mwv = pm.SendTo(mpA, "", null, typeof<obj>)
    
    test <@ not mwv.WatchViewer.Control.IsDisposed @>
    pm.RemoveManagedWatchViewer(mwv)
    test <@ mwv.WatchViewer.Control.IsDisposed @>

[<Fact>]
let ``ManagedPlugin ManagedWatchViewers filtered correctly`` () =
    let pluginA = mkPlugin "PluginA" mkControl
    let pluginB = mkPlugin "PluginB" mkControl

    let pm = new PluginManager(false)
    let mpA = pm.RegisterPlugin(pluginA)
    let mpB = pm.RegisterPlugin(pluginB)
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpA, "", null, typeof<obj>) |> ignore
    pm.SendTo(mpB, "", null, typeof<obj>) |> ignore

    test <@ mpA.ManagedWatchViewers |> Seq.map (fun x -> x.ID) |> Seq.toList = ["PluginA 1"; "PluginA 2"]  @>
    test <@ mpB.ManagedWatchViewers |> Seq.map (fun x -> x.ID) |> Seq.toList = ["PluginB 1";]  @>

let dumpAssemblyInfo asm kind =
    stdout.WriteLine("{0} assembly location: {1}", kind, asm)
    let fi = new System.IO.FileInfo(asm)
    let files = 
        fi.Directory.GetFiles() 
        //|> Seq.filter (fun x -> x.ToString().EndsWith(".dll")) 
        |> Seq.fold (fun acc x -> acc + " " + x.ToString()) ""
    stdout.WriteLine("{0} assembly files: {1}", kind, files)

[<Fact>]
let ``PluginManager finds plugins with scanForPlugins, the default, true`` () =
    let pm = new PluginManager()
    dumpAssemblyInfo (Assembly.GetExecutingAssembly().Location) "executing"
    dumpAssemblyInfo (Assembly.GetCallingAssembly().Location) "calling"

    //we assert that the plugins referenced in this project (the out-of-the-box plugins we provided) are available
    test <@ pm.ManagedPlugins |> Seq.length = 3 @>
    test <@ pm.ManagedPlugins |> Seq.exists (fun x -> x.Plugin.GetType() = typeof<DataGridViewPlugin>) @>
    test <@ pm.ManagedPlugins |> Seq.exists (fun x -> x.Plugin.GetType() = typeof<PropertyGridPlugin>) @>
    test <@ pm.ManagedPlugins |> Seq.exists (fun x -> x.Plugin.GetType() = typeof<TreeViewPlugin>) @>