namespace Swensen.FsEye.Plugins

open System
open System.Windows.Forms
open Swensen.FsEye
open LINQPad

type LinqPadWatchViewer () =
    let panel = new TableLayoutPanel ()
    let label = new Label ()
    let webBrowser = new WebBrowser ()

    do
        webBrowser.Dock <- DockStyle.Fill
        webBrowser.IsWebBrowserContextMenuEnabled <- false

        label |> panel.Controls.Add
        webBrowser |> panel.Controls.Add 

        label.Dock <- DockStyle.Fill
        label.Anchor <- AnchorStyles.Top ||| AnchorStyles.Left
        label.TextAlign <- System.Drawing.ContentAlignment.MiddleLeft

    interface IWatchViewer with
        member __.Watch (name,value,type') =
            label.Text <- sprintf "%s: %s" name type'.Name
            use writer = Util.CreateXhtmlWriter (enableExpansions=true)
            writer.Write(value)
            webBrowser.DocumentText <- writer.ToString ()
        member __.Control = 
            panel :> Control

type LinqPadPlugin() =
    interface IPlugin with
        member __.Name = 
            "LinqPad"
        member __.CreateWatchViewer() = 
            LinqPadWatchViewer() :> IWatchViewer
        member this.IsWatchable(_,_) =
            true
