namespace Swensen.Watch.Model
open System.Reflection

type MemberModelKind = 
    | Field
    | Property

type MemberModelProtection =
    | NonPublic
    | Public

///Represents a field or property member of a Value. Member Type is not null.
type MemberModel = { Name:string; Value: obj; Kind: MemberModelKind; Protection: MemberModelProtection; MemberType: System.Type }
    with
        ///The text representation of this model
        member this.Text =
            sprintf "%s (%s %s %s): %A" 
                (this.Name)
                (this.Protection |> function | Public -> "public" | NonPublic -> "private") //put all privates under one node
                (this.Kind |> function | Field -> "field" | Property -> "property") //use icons
                (this.MemberType.ToString())
                this.Value
        
        ///Get all the properties of value. If value is null returns Seq.empty.
        static member GetProperties(value:obj) =
            if obj.ReferenceEquals(value, null) then Seq.empty
            else
                seq {
                    let props = 
                        value.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                    for p in props do
                        if p.GetIndexParameters() = Array.empty then //non-indexed property
                            let isPublic  = p.GetGetMethod(true).IsPublic
                            let propValue = //maybe make lazy for timeouts and async
                                try
                                    p.GetValue(value, Array.empty)
                                with e ->
                                    e :> obj
                            yield {Name = p.Name; Value = propValue; Kind = Property; Protection = (if isPublic then Public else NonPublic); MemberType = p.PropertyType}
                }
        
        ///Get all the fields of value. If value is null returns Seq.empty.
        static member GetFields(value:obj) =
            if obj.ReferenceEquals(value, null) then Seq.empty
            else
                seq {
                    let fields = 
                        value.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)

                    for f in fields do
                        let isPublic  = f.IsPublic
                        let fieldValue = //maybe make lazy for timeouts and async
                            try 
                                f.GetValue(value)
                            with e ->
                                e :> obj
                        yield {Name = f.Name; Value = fieldValue; Kind = Field; Protection = (if isPublic then Public else NonPublic); MemberType = f.FieldType}
                }

        static member GetFieldsAndProperties(value:obj) =
            seq { yield! MemberModel.GetFields(value) ; yield! MemberModel.GetProperties(value) }

        ///Get field and property members sorted by name and partitioned by kind. If value is null returns Seq.empty.
        static member GetMembers(value:obj) =
            MemberModel.GetFieldsAndProperties(value)
                |> Seq.sortBy (fun x -> x.Name)
                |> Seq.toArray
                |> Array.partition (fun x -> x.Protection = MemberModelProtection.NonPublic)