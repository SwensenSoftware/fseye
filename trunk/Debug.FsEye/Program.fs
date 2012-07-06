namespace Debug.FsEye

open System
open System.Drawing
open System.Windows.Forms

module Main =
    let initEye() =
        let eye = new Swensen.FsEye.Forms.WatchForm()
        eye.Closing.Add(fun x -> x.Cancel <- false) //undo the close supression that the watch form otherwise tries to undertake.
        eye.Watch("x", 3)  
        eye.Watch("y", new System.Collections.Generic.List<int>(Seq.init 200 id))      
        eye


    [<STAThread>]
    [<EntryPoint>]
    do
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        Application.Run(initEye()) 
