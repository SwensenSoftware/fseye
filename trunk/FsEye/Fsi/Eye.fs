(*
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
namespace Swensen.FsEye.Fsi
open Swensen.FsEye.Forms

///Manages a EyeForm in the context of an FSI session listening for watch additions and updates and reflecting those in the EyeForm.
type Eye() as this = 
    let mutable eyeForm = new EyeForm()
    do
        ///prevent form from disposing when closing
        eyeForm.Closing.Add(fun args -> args.Cancel <- true ; this.Hide())

     ///Indicates whether or not FSI session listening is turned on
    let mutable listen = true   

    let mutable listenerCts = new System.Threading.CancellationTokenSource()

    ///The listener event handler. Takes care to throttle fast repeated calls (discards those leading up to the last in <100ms succession).
    let listener (_:obj) =
        if not listen then
            null
        else    
            listenerCts.Cancel()
            listenerCts <- new System.Threading.CancellationTokenSource()
            let gui = System.Threading.SynchronizationContext.Current
            let computation = async {
                let original = System.Threading.SynchronizationContext.Current

                do! Async.Sleep(100)
                do! Async.Sleep(0) //nop to force cancellation check

                let watchVars = SessionQueries.getWatchableVariables() 
                
                do! Async.SwitchToContext gui
                
                this.Show()
                watchVars |> Seq.iter eyeForm.Watch

                do! Async.SwitchToContext original
            }
            Async.Start(computation, listenerCts.Token)
            null

    ///Add or update a watch with the given name, value, and type.
    member __.Watch(name, value:obj, ty) =
        eyeForm.Watch(name, value, ty)

    ///Add or update a watch with the given name and value (where the type is derived from the type paramater of the value).
    member __.Watch(name, value) =
        eyeForm.Watch(name, value)

    ///Take archival snap shot of all current watches using the given label.
    member this.Archive(label) =
        eyeForm.Archive(label)

    ///Take archival snap shot of all current watches using a default label based on an archive count.
    member __.Archive() =
        eyeForm.Archive()
    
    ///Clear all watches (doesn't include archive nodes).
    member __.ClearArchives() =
        eyeForm.ClearArchives()

    ///Clear all watches (doesn't include archive nodes).
    member __.ClearWatches() =
        eyeForm.ClearWatches()

    ///Clear all archives (reseting archive count) and watches.
    member __.ClearAll() =
        eyeForm.ClearAll()

    ///<summary>
    ///Use this in a sync block with do!, e.g.
    ///<para></para>
    ///<para>async { </para>
    ///<para>&#160;&#160;&#160;&#160;for i in 1..100 do</para>
    ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;watch.Watch("i", i, typeof&lt;int&gt;)</para>
    ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;watch.Archive()</para>
    ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;if i = 50 then</para>
    ///<para>&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;do! watch.AsyncBreak()</para>
    ///<para>} |> Async.StartImmediate</para>
    ///</summary>
    member __.AsyncBreak() =
        eyeForm.AsyncBreak()

    ///Continue from an AsyncBreak()
    member __.AsyncContinue() =
        eyeForm.AsyncContinue()

    ///Indicates whether or not FSI session listening is turned on.
    member __.Listen 
        with get() = listen
        and set(value) = listen <- value

    ///The listener to attached to FSI.
    member __.Listener = 
        listener

    ///Show the Watch form.
    member __.Show() =
        if eyeForm.IsDisposed then
            eyeForm <- new EyeForm()

        if eyeForm.Visible |> not then
            eyeForm.Show()
            eyeForm.Activate()

    ///Hide the Watch form.
    member __.Hide() =
        eyeForm.Hide()

    ///Manages plugins and plugin watch viewers
    member this.PluginManager = eyeForm.PluginManager

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
///Holds the Eye singleton for the active FSI session
module Eye =
    ///The Eye singleton for the active FSI session
    let eye = new Eye()