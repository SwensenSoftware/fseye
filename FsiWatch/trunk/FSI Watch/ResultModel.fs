namespace Swensen.Watch.Model

type ResultModel = { OwnerName:string; Index:int; Value:obj }
    with
        member this.Name = 
            sprintf "%s@Results[%i]" this.OwnerName this.Index
        member this.Text = 
            sprintf "[%i] (%s): %A" 
                this.Index 
                (if obj.ReferenceEquals(this.Value,null) then "type unknown" else this.Value.GetType().ToString()) 
                this.Value
        
        ///Get all the Results for the given IEnumerable value (i.e. each element).
        static member GetResults(ownerName: string, value:System.Collections.IEnumerable) =
            let results =
                value
                |> Seq.cast<obj>
                |> Seq.truncate 100
                |> Seq.mapi (fun i x -> {OwnerName = ownerName; Index = i; Value = x})

            results