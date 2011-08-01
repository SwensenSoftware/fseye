namespace Swensen.FsEye
open System
open System.Reflection

///Image resource used to identify tree view node member classifications and provide FsEye form icon.
type ImageResource (name:string) =
    let loadImageResource =
        let assm = Assembly.GetExecutingAssembly()
        fun name -> System.Drawing.Image.FromStream(assm.GetManifestResourceStream(name))

    let image = loadImageResource name

    member __.Name = name
    member __.Image = image

//F# doesn't allow public static fields so we save our selves a lot of boiler-plate using a module pretending to be the static part of the class
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>] 
///Image resource used to identify tree view node member classifications and provide FsEye form icon.
module ImageResource =
    let Default = ImageResource "VSObject_Field.bmp" //VS also used "field" as default for everything!
    
    let Field = ImageResource "VSObject_Field.bmp"
    let Property = ImageResource "VSObject_Properties.bmp"
    let Method = ImageResource "VSObject_Method.bmp"

    let FriendField = ImageResource "VSObject_Field_Friend.bmp"
    let FriendProperty = ImageResource "VSObject_Properties_Friend.bmp"
    let FriendMethod = ImageResource "VSObject_Method_Friend.bmp"

    let PrivateField = ImageResource "VSObject_Field_Private.bmp"
    let PrivateProperty = ImageResource "VSObject_Properties_Private.bmp"
    let PrivateMethod = ImageResource "VSObject_Method_Private.bmp"

    let ProtectedField = ImageResource "VSObject_Field_Protected.bmp"
    let ProtectedProperty = ImageResource "VSObject_Properties_Protected.bmp"
    let ProtectedMethod = ImageResource "VSObject_Method_Protected.bmp"

    let SealedField = ImageResource "VSObject_Field_Sealed.bmp"
    let SealedProperty = ImageResource "VSObject_Properties_Sealed.bmp"
    let SealedMethod = ImageResource "VSObject_Method_Sealed.bmp"

    let WatchImages = [
        Default
        Field; Property; Method
        FriendField; FriendProperty; FriendMethod
        PrivateField; PrivateProperty; PrivateMethod
        ProtectedField; ProtectedProperty; ProtectedMethod
        SealedField; SealedProperty; SealedMethod]