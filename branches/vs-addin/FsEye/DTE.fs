//http://fortysix-and-two.blogspot.com/2010/05/accessing-visual-studios-automation-api.html

module Swensen.FsEye.DTE

#if INTERACTIVE

#r "EnvDTE"
#r "EnvDTE80.dll" 
#r "EnvDTE90.dll"
#r "EnvDTE100.dll"
#r "Extensibility"

#endif

open System
open System.Diagnostics

let getParentProcess (processId:int) =
    let proc = Process.GetProcessById processId
    let myProcID = new PerformanceCounter("Process", "ID Process", proc.ProcessName)
    let myParentID = new PerformanceCounter("Process", "Creating Process ID", proc.ProcessName)
    myParentID.NextValue() |> int
    
let currentProcess = Process.GetCurrentProcess().Id

let myVS = getParentProcess currentProcess

open EnvDTE
open EnvDTE80
open EnvDTE90
open EnvDTE100
open System.Runtime.InteropServices
open System.Runtime.InteropServices.ComTypes

module Msdev =
    
    [<DllImport("ole32.dll")>]  
    extern int GetRunningObjectTable([<In>]int reserved, [<Out>] IRunningObjectTable& prot)
 
    [<DllImport("ole32.dll")>]  
    extern int CreateBindCtx([<In>]int reserved,  [<Out>]IBindCtx& ppbc)

let tryFindInRunningObjectTable (name:string) =
    //let result = new Dictionary<_,_>()
    let mutable rot = null
    if Msdev.GetRunningObjectTable(0,&rot) <> 0 then failwith "GetRunningObjectTable failed."
    let mutable monikerEnumerator = null
    rot.EnumRunning(&monikerEnumerator)
    monikerEnumerator.Reset()
    let mutable numFetched = IntPtr.Zero
    let monikers = Array.init<ComTypes.IMoniker> 1 (fun _ -> null)
    let mutable result = None
    while result.IsNone && (monikerEnumerator.Next(1, monikers, numFetched) = 0) do
        let mutable ctx = null
        if Msdev.CreateBindCtx(0, &ctx) <> 0 then failwith "CreateBindCtx failed"
            
        let mutable runningObjectName = null
        monikers.[0].GetDisplayName(ctx, null, &runningObjectName)
        
        if runningObjectName = name then
            let mutable runningObjectVal = null
            if rot.GetObject( monikers.[0], &runningObjectVal) <> 0 then failwith "GetObject failed"
            result <- Some runningObjectVal
        
        //result.[runningObjectName] <- runningObjectVal
    result

let getVS2008ROTName id = 
    sprintf "!VisualStudio.DTE.9.0:%i" id

let getVS2010ROTName id = 
    sprintf "!VisualStudio.DTE.10.0:%i" id
    
let myDTE = (tryFindInRunningObjectTable (getVS2010ROTName myVS) |> Option.get) :?> DTE2

//--------------------------------------------------

let _applicationObject = myDTE

open Extensibility
let _addInInstance = 
    { new AddIn with  
        override this.Collection = null
        override this.Description with get() = "" and set(_) = ()
        override this.ProgID with get() = ""// and set(_) = ()
        override this.Guid with get() = "{7DCD2618-1C62-4185-897E-2CCA01149507}"
        override this.Connected with get() = false and set(_) = ()
        override this.Object with get() = null and set(_) = ()
        override this.DTE with get() = null //and set(_) = ()
        override this.Name with get() = null// and set(_) = ()
        override this.Remove() = ()
        override this.SatelliteDllPath with get() = null// and set(_) = ()
    }

// ctlProgID - the ProgID for your user control.
let ctlProgID = "Swensen.FsEye.Forms.WatchPanel";

// asmPath - the path to your user control DLL.
// Replace the <Path to VS Project> with the path to
// the folder where you created the WindowsCotrolLibrary.
// Remove the line returns from the path before 
// running the add-in.
let asmPath = @"C:\Users\Stephen\Documents\Visual Studio 2010\Projects\FsEye\code\FsEye\bin\Release\FsEye.dll";

// guidStr - a unique GUID for the user control.
let guidStr = "{cd187e5b-abd9-4b4b-83a3-6e99e74e0597}"

let toolWins = _applicationObject.Windows :?> Windows2
// Create the new tool window, adding your user control.
let mutable objTemp = null :> obj
let toolWin = toolWins.CreateToolWindow2(_addInInstance, asmPath, ctlProgID, "FsEye", guidStr, &objTemp)

// The tool window must be visible before you do anything 
// with it, or you will get an error.
if toolWin <> null then
    toolWin.Visible <- true