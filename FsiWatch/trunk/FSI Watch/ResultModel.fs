namespace Swensen.Watch.Model

type ResultModel = { OwnerName:string; Index:int; Value:obj }
    with
        member this.Name = 
            sprintf "%s@Results[%i]" this.OwnerName this.Index
        member this.Text = 
            sprintf "[%i]: %s" this.Index (if obj.ReferenceEquals(this.Value, null) then "null" else this.Value.ToString())
        static member GetResults(ownerName: string, value:System.Collections.IEnumerable) =
            value
            |> Seq.cast<obj>
            |> Seq.truncate 100
            |> Seq.mapi (fun i x -> {OwnerName = ownerName; Index = i; Value = x})

