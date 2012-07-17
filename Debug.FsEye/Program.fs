namespace Debug.FsEye

open System
open System.Drawing
open System.Windows.Forms

module Main =
    let initEye() =
        let eye = new Swensen.FsEye.Forms.WatchForm()   
        eye.Watch("x", 3)  
        eye.Watch("y", new System.Collections.Generic.List<int>(Seq.init 200 id))      
        eye.Watch("some null value", null, typeof<System.Collections.Generic.Dictionary<int,string>>)
        eye


    [<STAThread>]
    [<EntryPoint>]
    do
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        Application.Run(initEye()) 
