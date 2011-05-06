#r @"C:\Unquote\Unquote.dll"
open Swensen.Unquote
#load "WatchTreeModel.fs"
#load "WatchTreeView.fs"
open Swensen.Watch.Forms
#load "WatchForm.fs"
open Swensen.Watch.Forms
#load "FsiHelper.fs"
open Swensen.Watch.Fsi
open Microsoft.FSharp.Compiler.Interactive


//----testing todo, need to refactor out to model view
//lazy load children
//loads private and public instance properties and fields
//loads fields in alphebetic order, then properties in alphebetic order; case ignored
//loads enumerables
//does not load static properties


//add refresh all

open Swensen.Watch.Forms

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
    
    
//Simple example of how we can "break" during evaluation!
async {
    for i in 1..40 do
        watch.Watch("i", i)
        watch.Watch("i*2", i*2)
        watch.Archive()
        if i % 10 = 0 then
            System.Threading.Thread.Sleep(50)
            do! watch.AsyncBreak()
} |> Async.StartImmediate

watch.AsyncContinue()
watch.Show()


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

watch.Watch("test!", 23)

//Async.Parallel(

let f = 23;;

let x = <@ "hi" @>

open System
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

open System.Reflection
let getZeroArgNonUnitMethods (ty:Type) =
    let flags = BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic
    let tyConcreteMethods = ty.GetMethods()
    let tyInterfaces = ty.GetInterfaces() |> Seq.map (fun i -> i.GetMethods(flags)) |> Seq.concat
    let tyMethods = Seq.append tyConcreteMethods tyInterfaces
    tyMethods
    |> Seq.filter 
        (fun m -> m.ReturnType <> typeof<unit> 
                  && m.ReturnType <> typeof<Void> 
                  && m.GetGenericArguments().Length = 0 
                  && m.GetParameters().Length = 0)
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
//            tyMethod.
    
