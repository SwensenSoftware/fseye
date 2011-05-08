//#r "FsEye.dll" //release deployment expects this file next to the dll
#r "bin/Release/FsEye.dll"

open Swensen.FsEye
fsi.AddPrintTransformer eye.Listener //attached the listener
let eye = eye //bring it into scope    

//Simple example of how we can "break" during evaluation!
async {
    for i in 1..40 do
        eye.Watch("i", i)
        eye.Watch("i*2", i*2)
        eye.Archive()
        if i % 10 = 0 then
            System.Threading.Thread.Sleep(50)
            do! eye.AsyncBreak()
} |> Async.StartImmediate

eye.AsyncContinue()
eye.Show()


let work loops = 
    for i in 1I..(loops*1000000I) do () 
    loops
    
type SlowType() =
    member this.One = work 1I
    member this.Two = work 1I
    member this.Three = work 1I |> ignore ; failwith "Some exception occurred" ; 3
    member this.Four = work 1I
    member this.Four3 = work 1I
    member this.Four2 = work 2I
    member this.Four1 = work 4I
    member this.Fou5 = work 3I
    member private this.Fou23 = work 5I
    member this.Fou234 = work 6I
    member this.Fousd = work 1I
    member private this.Fous =work 3I
    member this.Foug = work 2I

eye.Watch("test!", 23)

//Async.Parallel(

let f = 23;;

let x = <@ "hi" @>

//use the following to determine whether is generic or nongeneric or nonseq type (to display right type info)
open System
#r "bin/Release/Unquote.dll"
open Swensen.Unquote
type SeqType =
    | Generic
    | NonGeneric of Type
    | NonSeq

let ty = typeof<list<int>>
let tyInterfaces = ty.GetInterfaces()
let tyGenericSeq = tyInterfaces |> Array.tryFind(fun i -> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<seq<_>>)
match tyGenericSeq with
| Some(tyGenericSeq) -> tyGenericSeq.FSharpName
| None -> "not found"

//also try to avoid display immediately seq's which are lazy (e.g. known concrete type, or perhaps having Item.[int] property lookup)

open System.Reflection
let getZeroArgNonUnitMethods (ty:Type) =
    let flags = BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic
    let tyConcreteMethods = ty.GetMethods()
    let tyInterfaces = ty.GetInterfaces() |> Seq.map (fun i -> i.GetMethods(flags)) |> Seq.concat
    let tyMethods = Seq.append tyConcreteMethods tyInterfaces
    tyMethods
    |> Seq.filter 
        (fun m -> m.ReturnType <> typeof<unit> //doesn't return unit
                  && m.ReturnType <> typeof<Void> //doesn't return Void
                  && m.GetGenericArguments().Length = 0 //doesn't take generic parameters
                  && m.GetParameters().Length = 0 //doesn't take normal parameters
                  && not ((m.Name.StartsWith("get_") || m.Name.StartsWith("set_")))) //is not a property (FSharp does not set the IsSpecialName bit: http://code.google.com/p/moq/issues/detail?id=238)
    |> Seq.map
        (fun m ->
            let suffix = m.Name + "()"
            if m.DeclaringType.IsInterface then
                m.DeclaringType.FSharpName + "." + suffix
            else
                suffix)                
    |> Seq.sortBy (fun name -> name.ToLower())

    //|> Seq.map (fun m -> m.DeclaringType.FSharpName + "." + m.Name)

//    tyMethods
//    |> Seq.filter 
//        (fun tyMethod ->
//            tyMethod..

//features
//Monitors FSI for watch additions and updates
//Asycronous, parallel, lazy loading of child nodes
//Asyncronouse Break and Continue debugging
//View large or infinite sequences in 100 element lazy loaded chunks
//View Public and Non-public value members
//Programatic access to Gui commands and watch addition and updates through FSI
//Pretty F# name printing via Unquote
