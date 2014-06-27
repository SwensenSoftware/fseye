namespace Debug.FsEye

open System
open System.Drawing
open System.Windows.Forms
open System.Xml

type IHuman =
    abstract member Name : string

type Student = 
    { Name:string; Age:int }
        interface IHuman with
            member this.Name = this.Name

type Professor = 
    { Name:string; Age:int; Rank: int }
        interface IHuman with
            member this.Name = this.Name

module Main =
    let initEye() =
        let eye = new Swensen.FsEye.Forms.EyeForm()   
        eye.Watch("x", 3)  
        eye.Watch("y", new System.Collections.Generic.List<int>(Seq.init 200 id))      
        eye.Watch("some null value", null, typeof<System.Collections.Generic.Dictionary<int,string>>)
        let value = ([|3.2; 2.; 1.; -3.; 23.|],[|"a";"b";"c";"d";"e"|])
        eye.Watch("series", value, value.GetType())
        let value = [{Student.Name="Tom"; Age=3};{Name="Jane"; Age=9}] 
        eye.Watch("series2", value, value.GetType())

        let value = [
            {Professor.Name="Jane"; Age=9; Rank=23} :> IHuman; 
            {Student.Name="Tom"; Age=3} :> IHuman; ]
        eye.Watch("series3", value, value.GetType())

        let doc = new XmlDocument()
        doc.AppendChild(doc.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"")) |> ignore
        let root = doc.CreateElement("Root")
        root.AppendChild(doc.CreateElement("Apple"))|>ignore
        doc.AppendChild(root)|>ignore
        eye.Watch("xmldoc", doc)

        eye.Watch("eye", eye, eye.GetType())
        eye


    [<STAThread>]
    [<EntryPoint>]
    do
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        Application.Run(initEye()) 
