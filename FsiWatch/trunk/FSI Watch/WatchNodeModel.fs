namespace Swensen.Watch.Model
open System.Reflection

type WatchKind = 
    | Field
    | Property

type WatchProtection =
    | NonPublic
    | Public

type WatchModel = { Name:string; LazyValue: Lazy<obj>; Kind: WatchKind; Protection: WatchProtection; Type: System.Type }
    with
        member this.Text =
            sprintf "%s (%s %s %s): %s" 
                (this.Name)
                (this.Protection |> function | Public -> "public" | NonPublic -> "private") //put all privates under one node
                (this.Kind |> function | Field -> "field" | Property -> "property") //use icons
                (this.Type.Name)
                (if obj.ReferenceEquals(this.LazyValue.Value, null) then "null" else this.LazyValue.Value.ToString())
        
        static member GetProperties(value:obj) =
            if obj.ReferenceEquals(value, null) then Seq.empty
            else
                seq {
                    let props = 
                        value.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                    for p in props do
                        if p.GetIndexParameters() = Array.empty then //non-indexed property
                            let isPublic  = p.GetGetMethod(true).IsPublic
                            let propValue = lazy (
                                try
                                    p.GetValue(value, Array.empty)
                                with e ->
                                    e :> obj
                            )
                            yield {Name = p.Name; LazyValue = propValue; Kind = Property; Protection = (if isPublic then Public else NonPublic); Type = p.PropertyType}
                }
        
        static member GetFields(value:obj) =
            if obj.ReferenceEquals(value, null) then Seq.empty
            else
                seq {
                    let fields = 
                        value.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                    for f in fields do
                        let isPublic  = f.IsPublic
                        let fieldValue = lazy ( //making lazy cause may want to consider timeouts and async loading
                            try 
                                f.GetValue(value)
                            with e ->
                                e :> obj
                        )
                        yield {Name = f.Name; LazyValue = fieldValue; Kind = Field; Protection = (if isPublic then Public else NonPublic); Type = f.FieldType}
                }

        static member GetFieldsAndProperties(value:obj) =
            seq { yield! WatchModel.GetFields(value) ; yield! WatchModel.GetProperties(value) }