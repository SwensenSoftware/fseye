# Installation #

FsEye is currently supported on .NET 4.0 and above including on Mono 2.10.8.1 and above.

Installation instructions are as follows:
  * Download and unzip featured release of FsEye from the [Downloads](https://code.google.com/p/fseye/wiki/Downloads?tm=2) page
  * Load FsEye.fsx within an FSI session, e.g. `#load @"C:\FsEye\FsEye.fsx"` or in Visual Studio add `--load:"C:\FsEye\FsEye.fsx"` to the F# Interactive options under Tools -> F# Tools to have it load on startup. This will automatically bring `eye` into scope for programmatically manipulating FsEye as well as attaching a listener to the FSI session to automatically add and update FsEye watches.
  * Open Swensen.FsEye.Fsi within .fs files in order to gain intellisense to the open `eye` instance.

_On Mono, ensure sure that the --gui+ FSI option is set to enable the WinForms event loop required by FsEye, since unlike on .NET, it is not set by default._

[NuGet packages](https://www.nuget.org/packages/FsEye) are also available for use by plugin authors or as an alternative quick installation method.
# Manual #

## Adding and updating watches ##

Adding and updating watches is done one of two ways:
  1. Send let bindings (or unnamed "it" bindings) to the F# Interactive, FsEye will automatically add, or update an existing, watch in the GUI tree view.
  1. Programmatically add, or update an existing, watch using `eye.Watch(name:string, value:'a)`, e.g. `eye.Watch("watchName", 28.2f)`

An existing root watch may also be refreshed (for example, if its value has been mutated) via the GUI tree right-click context menu -> Refresh.

## Navigating watches ##

Watch sub-nodes are loaded on demand upon expanding a tree node. Loading is done asynchronously (without blocking the GUI) and in parallel. When a watch is fully loaded, it will display the true type of its value (as opposed to the member's reflected possibly super type) along with a text representation of its value. Zero-arg non-void method sub-nodes will not be fully loaded immediately along with field and property nodes upon expansion: they are loaded when selected or expanded.

A watch's value may be copied to the Clipboard via the context menu -> Copy Value. If a watch has no value, or it's value has not completely loaded, the Copy Value menu item will be disabled.

## Archiving watches ##

Archiving allows you to take a snapshot of all current root watches, consolidating them as children of an archive root node. Future watches of the same name will be added freshly to the GUI tree, and the archived watches of the same name will not be updated. This is especially useful for watching how a value changes over the course of several iterations of a loop.

Root watches may be archived using the GUI Archive button or via `eye.Archive(label:string)` or `eye.Archive()`. The Archive button uses the latter parameterless Archive method, which uses an internal archive count as part of a default label.

## Clearing archives and watches ##

Archives and watches may be cleared using the Clear Archives, Clear Watches, or Clear All (i.e. clear both watches and archives) GUI buttons as well as using `eye.ClearArchives()`, `eye.ClearWatches()` and `eye.ClearAll()`. Clear All and Clear Archives reset the internal archive count.

Archives and root watches may be individually removed using the context menu -> Remove.

## Async break and continue ##

A particularly novel feature of FsEye is the ability to perform debugger-like "break" and "continue" within `async` blocks. When code executing inside an async block executes `do! eye.AsyncBreak()`, execution is suspended until the user clicks the Async Continue button on the GUI or uses `eye.AsyncContinue()`. For example,

```
async {
    for i in 1..40 do
        eye.Watch("i", i)
        eye.Watch("i*2", i*2)
        eye.Archive()
        if i % 10 = 0 then
            do! eye.AsyncBreak()
} |> Async.StartImmediate
```

## The FSI Listener ##

By default, FsEye in enabled to listen to FSI adding and updating watches as commands are entered. This may be enabled or disabled using `eye.Listen`.

## Window visibility ##

The FsEye window is designed never to dispose, even if it is closed. It shows automatically whenever a watch is added or updated through FSI listening, and whenever an "async breakpoint" is encountered. It is not shown automatically when watches are added or updated programmatically. It may also be manually shown using `eye.Show()` or manually hidden using `eye.Hide()`

## Plugins ##

Plugins are dlls that contain types that implement two interfaces: `IPlugin` and `IWatchViewer`. `IPlugin`s produce `IWatchViewer`s from traditional FsEye tree view watches to provide alternative views of a watch value. Plugins are deployed either along side FsEye.dll or in the `plugins` sub-folder (preferred) (FsEye automatically loads these upon start-up, though plugins may also be programmatically registered or removed).

FsEye includes two plugins out-of-the-box: FsEye.TreeView.Plugin.dll and FsEye.PropertyGrid.Plugin.dll. The former is simply the traditional FsEye `TreeView`-based watch viewer wrapped as a plugin. The latter wraps the WinForms `PropertyGrid` control.

Plugin watch viewers are displayed in tabs on the right-hand-side of a split panel (the "plugin panel") with the traditional FsEye watch viewer kept on the left-hand-side (the "main panel").  When no plugin watch tabs are open, the plugin panel is hidden. When a watch in the main panel is right-clicked, you will be presented with a context menu containing a `Send To` menu item (which may be hidden or disable based on the context, such as whether the selected watch has its value loaded) which has the following organization

```
Send To -> PluginA -> New
                      -
		      PluginA 1
		      PluginA 2
	-> PluginB -> New
		      -
     		      PluginB 1
```

If we chose `Send To -> PluginA -> New` we would send the selected watch to a new watch viewer instance for the given plugin.

If we chose `Send To -> PluginA -> PluginA 1` we would send the selected watch to the existing watch viewer instance for PluginA (replacing or updating it according to the rules implemented by the plugin author).

The created or updated plugin watch viewer tab will receive receive focus.

Various options for closing plugin tabs are available by right-clicking a given tab. These are Close, Close Others, and Close All.  Close Others will be disabled if there is only one tab. When all tabs are closed, the plugin panel will become hidden.

For more information about Plugins, including details on the plugin API and plugin developer, see [Plugins](http://code.google.com/p/fseye/wiki/Plugins).