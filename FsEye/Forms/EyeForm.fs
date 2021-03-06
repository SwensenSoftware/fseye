﻿(*
Copyright 2011 Stephen Swensen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*)
namespace Swensen.FsEye.Forms
open Swensen.FsEye
open System.Windows.Forms
open System

type EyeForm(pluginManager:PluginManager) as this =
    inherit Form(
        Name = "FsEye",
        Icon = IconResource.FsEye.Icon,
        Text = (
            let version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            sprintf "FsEye v%i.%i.%i by Stephen Swensen" version.Major version.Minor version.Build
        ),
        Size = (
            let size = SystemInformation.PrimaryMonitorSize
            System.Drawing.Size((2 * size.Width) / 3, size.Height / 2)
        )
    )

    let eyePanel = new EyePanel(pluginManager, Dock=DockStyle.Fill)

    do
        this.Controls.Add(eyePanel)
        Application.AddMessageFilter this

    /// Implementing message filter here allows to propagate MouseWheel messages
    /// to the control under mouse pointer. This is needed because otherwise - only control
    /// which currently has focus will receive these events. 
    interface IMessageFilter with
        member __.PreFilterMessage message =
            if message.Msg <> Win32.WM_MOUSEWHEEL
            then 
                false
            else
                let hWnd = Win32.WindowFromPoint Cursor.Position 

                if hWnd <> IntPtr.Zero && hWnd <> message.HWnd && (Control.FromHandle hWnd <> null) then 
                    Win32.SendMessage (hWnd,message.Msg,message.WParam,message.LParam) |> ignore
                    true
                else
                    false
    with
        //a lot of delegation to treeView below -- not sure how to do this better

        ///Add or update a watch with the given name, value, and type.
        member this.Watch(name, value:obj, ty) =
            eyePanel.Watch(name, value, ty)

        ///Add or update a watch with the given name and value (where the type is derived from the type paramater of the value).
        member this.Watch(name, value) =
            eyePanel.Watch(name,value)

        ///Take archival snap shot of all current watches using the given label.
        member this.Archive(label: string) =
            eyePanel.Archive(label)

        ///Take archival snap shot of all current watches using a default label based on an archive count.
        member this.Archive() = 
            eyePanel.Archive()

        ///Clear all archives and reset the archive count.
        member this.ClearArchives() = 
            eyePanel.ClearArchives()

        ///Clear all watches (doesn't include archive nodes).
        member this.ClearWatches() = 
            eyePanel.ClearWatches()

        ///Clear all archives (reseting archive count) and watches.
        member this.ClearAll() = 
            eyePanel.ClearAll()

        ///<summary>
        ///Use this in an async block with do! to pause execution and activate the watch viewer, e.g.
        ///<para></para>
        ///<para>async { </para>
        ///<para>&#160;&#160;&#160;&#160;for i in 1..100 do</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;watch.Watch("i", i, typeof&lt;int&gt;)</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;watch.Archive()</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;if i = 50 then</para>
        ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;do! watch.AsyncBreak()</para>
        ///<para>} |> Async.StartImmediate</para>
        ///</summary>
        member this.AsyncBreak() =
            let asyncBreak = eyePanel.AsyncBreak()
            if this.Visible |> not then
                this.Show()

            this.Activate()

            asyncBreak

        ///Continue from an AsyncBreak()
        member this.AsyncContinue() =
            eyePanel.AsyncContinue()