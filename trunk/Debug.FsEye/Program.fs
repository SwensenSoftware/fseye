namespace Debug.FsEye

open System
open System.Drawing
open System.Windows.Forms

module Main =
    let initEye() =
        let eye = new Swensen.FsEye.Forms.WatchForm()
        eye.Watch("x", 3)  
        eye.Watch("y", [1;2;3;4;5;6;7])      
        eye


    [<STAThread>]
    [<EntryPoint>]
    do
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        Application.Run(initEye()) 
