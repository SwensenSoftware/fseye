#I __SOURCE_DIRECTORY__ //http://stackoverflow.com/questions/4860991/f-for-scripting-location-of-script-file

//should do dynamic action while building to toggle between this local path and the relative path
//#r "FsEye.dll" //release deployment expects this file next to the dll
#r "bin/Release/FsEye.dll"

open Swensen.FsEye
fsi.AddPrintTransformer eye.Listener //attached the listener
let eye = eye //bring it into scope