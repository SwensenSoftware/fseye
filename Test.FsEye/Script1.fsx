#I @"G:\coldfar_py\fseye\Test.FsEye\bin\Debug\net7.0"
#r @"FsEye.dll"
#r @"FsEye.DataGridView.Plugin.dll"
#r @"FsEye.PropertyGrid.Plugin.dll"
#r @"FsEye.TreeView.Plugin.dll"
#r @"Utils.dll"
open Swensen.FsEye.Fsi
open Swensen.FsEye.Plugins
open Swensen.Utils
open Swensen.FsEye
open System.Windows.Forms

System.AppDomain.CurrentDomain.GetAssemblies() |> Array.mapi (fun i a -> printfn "%d %A" i a.FullName)
let assm = System.AppDomain.CurrentDomain.GetAssemblies() |>Array.item 25
assm.GetManifestResourceStream(@"Resources\" + "FsEye.ico")
assm.GetManifestResourceNames()


let eye = new Eye()
let eye = eye
let p = new Swensen.FsEye.Plugins.DataGridViewPlugin()
let pi = p :> IPlugin

fsi.AddPrintTransformer eye.Listener 
