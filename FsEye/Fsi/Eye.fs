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

///Manages a WatchForm in the context of an FSI session listening for watch additions and updates and reflecting those in the WatchForm.
type Eye() as this = 
    let mutable watchForm = new WatchForm()
    do
        ///prevent form from disposing when closing
        watchForm.Closing.Add(fun args -> args.Cancel <- true ; this.Hide())
    
    ///Indicates whether or not FSI session listening is turned on
    let mutable listen = true

    ///The listener event handler
    let listener =
        //need to figure out a way to not call repeatedly for single evaluation
        fun (_:obj) ->
            if listen then
                listen <- false //we are going to try to not fire every time a 100 item list for example is entered into FSI
                let gui = System.Threading.SynchronizationContext.Current
                async {
                    try
                        let original = System.Threading.SynchronizationContext.Current
                        
                        let watchVars = SessionQueries.getWatchableVariables() 
                        
                        do! Async.SwitchToContext gui
                        
                        this.Show()
                        watchVars |> Array.iter watchForm.Watch

                        do! Async.SwitchToContext original
                        do! Async.Sleep 100
                        do! Async.SwitchToContext gui
                        listen <- true
                        do! Async.SwitchToContext original
                    with e ->
                        let original = System.Threading.SynchronizationContext.Current
                        do! Async.SwitchToContext gui
                        listen <- true //want to make sure we don't leave this as false if there is a problem!
                        printfn "%A" (e.InnerException)
                        do! Async.SwitchToContext original
                } |> Async.Start
                null
            else
                //printfn "listen is false"
                null

    ///Add or update a watch with the given name, value, and type.
    member __.Watch(name, value:obj, ty) =
        watchForm.Watch(name, value, ty)

    ///Add or update a watch with the given name and value (where the type is derived from the type paramater of the value).
    member __.Watch(name, value) =
        watchForm.Watch(name, value)

    ///Take archival snap shot of all current watches using the given label.
    member this.Archive(label) =
        watchForm.Archive(label)

    ///Take archival snap shot of all current watches using a default label based on an archive count.
    member __.Archive() =
        watchForm.Archive()
    
    ///Clear all watches (doesn't include archive nodes).
    member __.ClearArchives() =
        watchForm.ClearArchives()

    ///Clear all watches (doesn't include archive nodes).
    member __.ClearWatches() =
        watchForm.ClearWatches()

    ///Clear all archives (reseting archive count) and watches.
    member __.ClearAll() =
        watchForm.ClearAll()

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
        watchForm.AsyncBreak()

    ///Continue from an AsyncBreak()
    member __.AsyncContinue() =
        watchForm.AsyncContinue()

    ///Indicates whether or not FSI session listening is turned on.
    member __.Listen 
        with get() = listen
        and set(value) = listen <- value

    ///The listener to attached to FSI.
    member __.Listener = 
        listener

    ///Show the Watch form.
    member __.Show() =
        if watchForm.IsDisposed then
            watchForm <- new WatchForm()

        if watchForm.Visible |> not then
            watchForm.Show()
            watchForm.Activate()

    ///Hide the Watch form.
    member __.Hide() =
        watchForm.Hide()

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
///Holds the Eye singleton for the active FSI session
module Eye =
    ///The Eye singleton for the active FSI session
    let eye = new Eye()