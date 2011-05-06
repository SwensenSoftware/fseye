#I __SOURCE_DIRECTORY__ //http://stackoverflow.com/questions/4860991/f-for-scripting-location-of-script-file
//#r "FSI_Watch.dll"
#r "bin/Release/FSI_Watch.dll"

open Swensen.Watch.Forms
open Swensen.Watch.Fsi
//initialize watch and attached fsi listener
let watch = new WatchForm()

fsi.AddPrintTransformer <|
    //need to figure out a way to not call repeatedly for single evaluation
    fun (_:obj) ->
        try
            if watch.Visible |> not then
                watch.Show()
                watch.Activate()

            FsiHelper.getWatchableFsiVariables()
            |> watch.Watch

            null
        with e ->
            printfn "%A" (e.InnerException)
            null

//only way can get at
module FsiWatch = 
    let watch = watch