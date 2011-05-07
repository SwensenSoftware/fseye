#I __SOURCE_DIRECTORY__ //http://stackoverflow.com/questions/4860991/f-for-scripting-location-of-script-file

//should do dynamic action while building to toggle between this local path and the relative path
#r "FsiWatch.dll" //release deployment expects this file next to the dll
//#r "bin/Release/FsiWatch.dll"

open Swensen.Watch.Forms
open Swensen.Watch.Fsi

fsi.AddPrintTransformer <|
    watch.Listener

//bring it into scope
let watch = watch