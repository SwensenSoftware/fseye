namespace Swensen.FsEye

open System.Reflection
open System.Runtime.InteropServices
open System.Drawing

open FSharp.NativeInterop

module Win32 =
    let WM_MOUSEWHEEL = 0x20a
    let TVM_SETEXTENDEDSTYLE = 0x1100 + 0x2c
    let TVM_GETEXTENDEDSTYLE = 0x1100 + 0x2d;
    let TVS_EX_DOUBLEBUFFER = 0x0004;

    [<DllImport("user32.dll")>]
    extern nativeint WindowFromPoint(Point pt)

    [<DllImport("user32.dll")>]
    extern nativeint SendMessage(nativeint hWnd, int msg, nativeint wp, nativeint lp)
