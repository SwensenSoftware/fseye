//open System.Reflection
//Assembly.LoadFile @"C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\7.0.13\ref\net7.0\System.Windows.Forms.dll"
//#r @"G:\coldfar_py\fseye\FsEye\bin\Debug\net8.0-windows\FsEye7.dll"

#I @"G:\coldfar_py\fseye\Test.FsEye\bin\Debug\net8.0-windows"
#r @"FsEye7.dll"
#r @"FsEye.DataGridView.Plugin7.dll"
#r @"FsEye.PropertyGrid.Plugin7.dll"
#r @"FsEye.TreeView.Plugin7.dll"
#r @"Utils.dll"

#I @"C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\8.0.0-rc.2.23479.10\ref\net8.0\"
//#I @"C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\7.0.13\ref\net7.0\"
#r @"System.Windows.Forms.dll"
#r @"System.Drawing.Common.dll"
//#r @"System.Windows.Forms.Primitives.dll"

#r @"nuget: Microsoft.Extensions.FileProviders.Embedded"
#r @"nuget: Unquote, 6.1.0"
#r @"nuget: Xunit, 2.6.1"
open Swensen.FsEye.Fsi
open Swensen.FsEye.Plugins
open Swensen.Utils
open Swensen.FsEye
open Microsoft.Extensions.FileProviders
//let FsEye = IconResource "FsEye.ico"
//#r @"G:\coldfar_py\fseye\FsEye\bin\Debug\net8.0-windows\FsEye7.dll"

open Swensen.FsEye.IconResource
open System.Windows.Forms
open System.Drawing
open System.Reflection

//MessageBox.Show("orz")

System.AppDomain.CurrentDomain.GetAssemblies() |> Array.mapi (fun i a -> printfn "%d %A" i a.FullName)
//let assm = System.AppDomain.CurrentDomain.GetAssemblies() |>Array.item 46
//assm.GetManifestResourceStream(@"Resources/" + "FsEye.ico")
//assm.GetManifestResourceStream(@"FsEye.Resources." + "FsEye.ico")
//assm.GetManifestResourceNames()

//let embeddedFileProvider = new EmbeddedFileProvider(assm, "FsEye7")
//let certificateFileInfo = embeddedFileProvider.GetFileInfo(@"Resources\" + "FsEye.ico")
//let resc = certificateFileInfo.CreateReadStream()



let eye = new Eye()

eye.Show()

//let eye = eye
let p = new Swensen.FsEye.Plugins.DataGridViewPlugin()
let pi = p :> IPlugin

fsi.AddPrintTransformer eye.Listener 

task {

    let eye2 = new Swensen.FsEye.Forms.EyeForm(new PluginManager())   
    eye2.Watch("x", 3)  

    eye2.Show()
}

Assembly.GetCallingAssembly()




open System.Threading
open System.Threading.Tasks

Task.Run(System.Action(fun () -> 
    try
        let f = new Form()
        f.Show()
        f.Refresh()
    with
    | exn ->
        printfn "%s" exn.Message
)
)
