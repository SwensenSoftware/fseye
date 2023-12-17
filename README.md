[User Guide](../../wiki/UserGuide) | [Downloads](http://www.swensensoftware.com/fseye) | [Release Notes](../../wiki/ReleaseNotes) | [Issues](../../issues)

# .NET 7 / .NET 8 supported now!
---
Once we build VisualFSharp solution and change FX_NO_WINFORMS conditional compilation symbol to build a winform based fsi, copy the fsi.dll into FSharp SDK folder, fseye is back to VS2022 again!!
---
How to use:
```
#r @"FsEye7.dll"
#r @"FsEye.DataGridView.Plugin7.dll"
#r @"FsEye.PropertyGrid.Plugin7.dll"
#r @"FsEye.TreeView.Plugin7.dll"
#r @"Utils.dll"
#I @"C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\8.0.0-rc.2.23479.10\ref\net8.0\"
```
---


FsEye is a visual object tree inspector for the F# Interactive. Taking advantage of the built-in WinForms event loop, it listens for additions and updates to variables within FSI sessions, allowing you to reflectively examine properties of captured values through a visual interface. It also allows you to programmatically add and update eye watches, effectively ending the era of `printf` REPL debugging.

![screen shot](https://github.com/ingted/fseye/assets/4289161/3f6a9bcc-329f-4e96-95f4-890b78f3b0fb)

Features
  * Monitors FSI for watch additions and updates
  * Asynchronous, parallel, lazy loading of child nodes
  * Asynchronous Break and Continue debugging
  * View large or infinite sequences in 100 element lazy loaded chunks
  * View public and non-public member values, including fields, properties, and lazily forced return values for zero-arg non-void call members
  * Programmatic control of FsEye watches
  * Pretty F# name printing
  * Copy watch values to the Clipboard with the right-click context menu
  * Support for plugins with PropertyGrid, DataGridView, and TreeView-based plugins provided out-of-the box

---

[![Build status](https://ci.appveyor.com/api/projects/status/mmy4kyngu0d8lxu4?svg=true)](https://ci.appveyor.com/project/stephen-swensen/fseye)

You are welcome to [Pay What You Want](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=BFNS7ZMAL3JZQ) for FsEye via PayPal.

Copyright 2011-2016 [Swensen Software](http://www.swensensoftware.com)
