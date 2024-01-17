namespace Swensen.FsEye
open System
open System.Reflection
open Microsoft.Extensions.FileProviders

///Icon resource used to identify tree view node member classifications and provide FsEye form icon.
type IconResource (name:string) = //should probably make IDisposible to dispose of Icon
    let loadIconResource =
        let assm = Assembly.GetExecutingAssembly()
        ////fun name -> new System.Drawing.Icon(assm.GetManifestResourceStream(@"FsEye.Resources." + name))
        //printfn "%A" assm

        //let resc = assm.GetManifestResourceStream(@"Resources\" + name)

        //printfn "%s length %A" name resc.Length

        let embeddedFileProvider = new EmbeddedFileProvider(assm, "FsEye7")
        let certificateFileInfo = embeddedFileProvider.GetFileInfo(@"Resources\" + "FsEye.ico")
        let resc = certificateFileInfo.CreateReadStream()

        fun name -> new System.Drawing.Icon(resc)


    let image = loadIconResource name

    member __.Name = name
    member __.Icon = image

//F# doesn't allow public static fields so we save our selves a lot of boiler-plate using a module pretending to be the static part of the class
////[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>] 
///Icon resource used to identify tree view node member classifications and provide FsEye form icon.
module IconResource =
    //let name = "FsEye.ico"
    let FsEye = IconResource "FsEye.ico"