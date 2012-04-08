namespace Swensen.FsEye
open System
open System.Reflection

///Icon resource used to identify tree view node member classifications and provide FsEye form icon.
type IconResource (name:string) = //should probably make IDisposible to dispose of Icon
    let loadIconResource =
        let assm = Assembly.GetExecutingAssembly()
        fun name -> new System.Drawing.Icon(assm.GetManifestResourceStream(name))

    let image = loadIconResource name

    member __.Name = name
    member __.Icon = image

//F# doesn't allow public static fields so we save our selves a lot of boiler-plate using a module pretending to be the static part of the class
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>] 
///Icon resource used to identify tree view node member classifications and provide FsEye form icon.
module IconResource =
    let FsEye = IconResource "FsEye.ico"