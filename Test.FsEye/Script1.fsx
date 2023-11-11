#I @"bin\Debug\net8.0-windows"
#r @"FsEye7.dll"
#r @"FsEye.DataGridView.Plugin7.dll"
#r @"FsEye.PropertyGrid.Plugin7.dll"
#r @"FsEye.TreeView.Plugin7.dll"
#r @"Utils.dll"
#I @"C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\8.0.0-rc.2.23479.10\ref\net8.0\"
#r @"System.Windows.Forms.dll"
#r @"System.Drawing.Common.dll"


#r @"nuget: Microsoft.Extensions.FileProviders.Embedded"
#r @"nuget: Unquote, 6.1.0"
#r @"nuget: Xunit, 2.6.1"
open Swensen.FsEye.Fsi
open Swensen.FsEye.Plugins
open Swensen.Utils
open Swensen.FsEye
open Microsoft.Extensions.FileProviders
//let FsEye = IconResource "FsEye.ico"
//#r @"..\FsEye\bin\Debug\net8.0-windows\FsEye7.dll"

open Swensen.FsEye.IconResource
open System.Windows.Forms
open System.Drawing
open System.Reflection

System.AppDomain.CurrentDomain.GetAssemblies() |> Array.mapi (fun i a -> printfn "%d %A" i a.FullName)


let eye = new Eye()
eye.Show()
eye.Watch("x", 3)  

