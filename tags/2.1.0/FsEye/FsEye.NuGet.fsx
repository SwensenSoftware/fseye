//reference FsEye.dll (e.g. "Send to F# Interactive" on the project reference availabe in some IDE versions)
//#r @"<path to>\FsEye.dll"

//Execute the following two lines of code to bring the eye singleton into scope and bind it to FSI
open Swensen.FsEye.Fsi //bring the eye singleton into scope
fsi.AddPrintTransformer eye.Listener //attached the listener