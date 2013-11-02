namespace Debug.FsEye

open System
open System.Drawing
open System.Windows.Forms

type Student = { mutable Name:string; Age:int }

module Main =
    let initEye() =
        let eye = new Swensen.FsEye.Forms.EyeForm()   
        eye.Watch("x", 3)  
        eye.Watch("y", new System.Collections.Generic.List<int>(Seq.init 200 id))      
        eye.Watch("some null value", null, typeof<System.Collections.Generic.Dictionary<int,string>>)
        let value = ([|3.2; 2.; 1.; -3.; 23.|],[|"a";"b";"c";"d";"e"|])
        eye.Watch("series", value, value.GetType())
        let value = [{Name="Tom"; Age=3};{Name="Jane"; Age=9}] |> Seq.toArray
        eye.Watch("series2", value, value.GetType())
        eye.Watch("eye", eye, eye.GetType())
        eye


    [<STAThread>]
    [<EntryPoint>]
    do
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        Application.Run(initEye()) 
