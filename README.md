[User Guide](../../wiki/UserGuide) | [Downloads](http://www.swensensoftware.com/fseye) | [Release Notes](../../wiki/ReleaseNotes) | [Issues](../../issues)

---

FsEye is a visual object tree inspector for the F# Interactive. Taking advantage of the built-in WinForms event loop, it listens for additions and updates to variables within FSI sessions, allowing you to reflectively examine properties of captured values through a visual interface. It also allows you to programmatically add and update eye watches, effectively ending the era of `printf` REPL debugging.

![screen shot](http://www.swensensoftware.com/static/fseye/front-page-example.png)

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
