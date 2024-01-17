namespace WinFormsApp1
open System
open System.Windows.Forms

open Form1

module main =

    type ApplicationConfiguration () =
    
        static member Initialize() =        
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(false)
            Application.SetHighDpiMode(HighDpiMode.SystemAware)
        


    [<STAThread>]
    [<EntryPoint>]
    let main argv =
        let _ = ApplicationConfiguration.Initialize()
        Application.Run(new Form1())
        0