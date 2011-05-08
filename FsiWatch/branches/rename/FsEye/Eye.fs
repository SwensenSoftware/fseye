namespace Swensen.FsEye
open Swensen.FsEye.Forms

type Eye(watchForm:WatchForm) = 
    //value indicates whether or not FSI session listening is turned on
    let mutable listen = true

    //the listener event handler
    let listener =
        //need to figure out a way to not call repeatedly for single evaluation
        fun (_:obj) ->
            if listen then
                //printfn "listen is true"
                try
                    if watchForm.Visible |> not then
                        watchForm.Show()
                        watchForm.Activate()

                    FsiHelper.getWatchableFsiVariables() 
                    |> Array.iter watchForm.Watch
                    //System.Threading.Timer(new TimerCallback(fun _ -> listen <- true), 0, 1000)
                    null
                with e ->
                    printfn "%A" (e.InnerException)
                    null
            else
                //printfn "listen is false"
                null

    ///Add or update a watch with the given name, value, and type.
    member __.Watch(name, value, ty) =
        watchForm.Watch(name, value, ty)

    ///Add or update a watch with the given name and value.
    member __.Watch(name, value) =
        watchForm.Watch(name, value)

    ///Take archival snap shot of all current watches using the given label.
    member this.Archive(label: string) =
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

    ///Clear all archives and watches.
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
        watchForm.Show()
        watchForm.Activate()

    ///Hide the Watch form.
    member __.Hide() =
        watchForm.Hide()

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Eye =
    let eye = new Eye(new WatchForm())