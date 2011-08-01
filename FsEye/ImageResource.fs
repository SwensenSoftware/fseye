namespace Swensen.FsEye
open System
open System.Reflection

type ImageResource private (name:string) =
    let loadImageResource =
        let assm = Assembly.GetExecutingAssembly()
        fun name -> System.Drawing.Image.FromStream(assm.GetManifestResourceStream(name))

    let image = loadImageResource name

    member __.Name = name
    member __.Image = image

    static member None = ImageResource "VSObject_Field.bmp"
    static member Field = ImageResource "VSObject_Field.bmp"
    static member Property = ImageResource "VSObject_Properties.bmp"
    static member Method = ImageResource "VSObject_Method.bmp"

