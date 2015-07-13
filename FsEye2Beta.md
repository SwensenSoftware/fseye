# Introduction #

This wiki page is dedicated to documenting the current FsEye 2.0.0 beta release.

One of the key features in this release is the implementation of plugin system for FsEye, see [issue 25](https://code.google.com/p/fseye/issues/detail?id=25). The other big feature that can be expected for the final FsEye 2.0.0 release is a robust settings system, see [issue 22](https://code.google.com/p/fseye/issues/detail?id=22) and other related issues.

We are very interested in feedback from FsEye users and prospective plugin authors alike. Bug reports or feature requests may be created as new issues in the [Issues](http://code.google.com/p/fseye/issues/list) tab. General feedback may be added as comments at the bottom of this page.

Although this is a beta release, it is well-tested and very usable as-is.

# For All Users #

Plugins are dlls that contain types that implement two interfaces: `IPlugin` and `IWatchViewer`. `IPlugin`s produce `IWatchViewer`s from traditional FsEye tree view watches to provide alternative views of a watch value. Plugins are deployed either along side FsEye.dll or in the `plugins` sub-folder (preferred) (FsEye automatically loads these upon start-up, though plugins may also be programmatically registered or removed).

FsEye includes two plugins out-of-the-box: FsEye.TreeView.Plugin.dll and FsEye.PropertyGrid.Plugin.dll. The former is simply the traditional FsEye `TreeView`-based watch viewer wrapped as a plugin. The latter wraps the WinForms `PropertyGrid` control.

# For GUI Users #

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

# For API Users #

The following public classes have been renamed to reflect the growing breadth of functionality they now handle:

  * WatchPanel -> EyePanel
  * WatchForm -> EyeForm

The following types now expose a public property `PluginManager` which is used to programmatically manipulate plugins: `Eye` (the primary surface for manipulating FsEye within FSI), `EyeForm`, and `EyePanel`.

The `PluginManager` object is responsible for managing plugins. As mentioned previously, plugin authors implement two interfaces, `IPlugin` and `IWatchViewer`. These interfaces are wrapped by two record types, `ManagedPlugin` and `ManagedWatchViewer`. The following are the definitions for these types (with some members removed for simplicity):

```
///Represents a plugin watch viewer being managed by the PluginManager
type ManagedWatchViewer =
  {///The unique ID of the watch viewer instance 
   ID: string;
   ///The watch viewer instance which is being managed
   WatchViewer: IWatchViewer;
   ///The owning ManagedPlugin
   ManagedPlugin: ManagedPlugin;}
///Represents a plugin being managed by the PluginManager
and ManagedPlugin =
  {///The plugin being managed
   Plugin: IPlugin;
   ///The owning plugin manager
   PluginManager: PluginManager;}
  with
    member ManagedWatchViewers : seq<ManagedWatchViewer>
  end
///Manages FsEye watch viewer plugins
and PluginManager =
  class
	///Initialize and load any plugins in the "plugins" folder relative to the executing assembly
    new : unit -> PluginManager
	///Register the given plugin and return the managed plugin wrapping it. If a managed plugin wrapping a plugin of the same name exists, 
    ///removes it (and all of its associated managed watch viewers).
    member RegisterPlugin : plugin:IPlugin -> ManagedPlugin
    ///Remove the given managed plugin (and all of its managed watch viewers).
    member RemoveManagedPlugin : mp:ManagedPlugin -> unit
    ///Remove the managed plugin (and all of its managed watch viewers) by name.
    member RemoveManagedPlugin : name:string -> unit
    ///Remove the given managed watch viewer, disposing the watch viewer's Control.
    member RemoveManagedWatchViewer : mwv:ManagedWatchViewer -> unit
    ///Remove the managed watch viewer by id, disposing the watch viewer's Control.
    member RemoveManagedWatchViewer : id:string -> unit
    ///Create a new watch viewer for the given managed plugin, sending the given label, value and type.
    ///Returns the ManagedWatchViewer which wraps the created watch viewer.
    member
      SendTo : managedPlugin:ManagedPlugin * label:string * value:obj *
               valueTy:System.Type -> ManagedWatchViewer
    ///Send the given label, value and type to the given, existing managed watch viewer.
    member
      SendTo : mwv:ManagedWatchViewer * label:string * value:obj *
               valueTy:System.Type -> unit
    member ManagedPlugins : seq<ManagedPlugin>
    member ManagedWatchViewers : seq<ManagedWatchViewer>
    member WatchAdded : IEvent<ManagedWatchViewer>
    member WatchRemoved : IEvent<ManagedWatchViewer>
    member WatchUpdated : IEvent<ManagedWatchViewer>
  end

```

For API users interested in the `IPlugin` and `IWatchViewer` interfaces, see the section for plugin authors.

Note that adding, removing, and updating watches triggers the `WatchAdded`, `WatchRemoved`, and `WatchUpdated` events of the plugin manager. The GUI plugin panel listens to these events to add, update (select), and remove the tabs it controls.

We are particularly concerned and receptive about feedback on this API. It has been difficult to come up with a design that is both user friendly and respects the encapsulation we are trying to achieve with the `PluginManager` (i.e. `ManagedPlugin`s are exclusively controlled by the plugin manager, they should be protected against mutation to the greatest extent possible. Additionally the `ManagedPlugin` and `ManagedWatchViewer` provide additional wrapper information for the `PluginManager` beyond what plugin authors are required to supply by the wrapped interfaces).

# For Plugin Authors #

Plugin authors should be well versed in the documentation provided for all other types of users. The following are the definitions for the interfaces plugin authors are required to implement, they are found in the Swensen.FsEye namespace:

```
///Specifies a watch viewer interface, an instance which can add or update one or more watches with 
///a custom watch viewer control
type IWatchViewer =
  interface
    ///Add or update a watch with the given label, value, and type. Note: you can choose to 
    ///disregard the label and type if desired, but will almost certainly need the value.
    abstract member Watch : string * obj * System.Type -> unit
    ///The underlying watch viewer control. Exists as a property of IWatchViewer 
    ///since you may or may not own the control (i.e. you cannot directly implement IWatchViewer on the control).
    abstract member Control : System.Windows.Forms.Control
  end
  
///Specificies a watch view plugin, capable of creating watch viewer instances
type IPlugin =
  interface
    ///Create an instance of this plugin's watch viewer
    abstract member CreateWatchViewer : unit -> IWatchViewer
    ///Returns true or false depending on whether the given instance and its type (which we may need if 
    ///the instance is null) are watchable: if false, then FsEye will not allow creating a watch for a value 
    ///of the given type. Plugin authors should be mindful of the performance impact this method may have.
    abstract member IsWatchable : obj * System.Type -> bool
    //The name of the plugin
    abstract member Name : string
  end
```

these should be implemented as class libraries (.dll assembly files) targeting .NET <=4.5 with processor AnyCPU or x86 platforms (FsEye.dll, FsEye.PropertyGrid.Plugin.dll, and FsEye.TreeView.Plugin.dll all target .NET 4.0 with processor AnyCPU platform).

`PluginManager.RegisterPlugin` is a nice way to interactively develop and test plugins through FSI.

The out-of-the-box [PropertyGrid](https://code.google.com/p/fseye/source/browse/trunk/FsEye.PropertyGrid.Plugin/PropertyGridPlugin.fs?r=379) and [TreeView](https://code.google.com/p/fseye/source/browse/trunk/FsEye.TreeView.Plugin/TreeViewPlugin.fs?r=379) plugins may service as an implementation reference (see links to code).

We are interested in feedback regarding the robustness and suitability of the plugin interfaces for plugin author purposes. One of the concerns we have is regarding version: Plugin authors must reference FsEye.dll to implement the required interfaces: does this present a problem when FsEye increments its assembly version number when a plugin it tries to load was built against a previous version of FsEye? Do we need binding redirects or something like that?

If you are developing a plugin, we'd be interested to hear about it! Eventually, we plan to have a "PluginGallery" wiki page to feature links to 3rd party FsEye plugins.

# Screenshots #

![https://fseye.googlecode.com/svn/images/v2beta-screenshot-sendto.png](https://fseye.googlecode.com/svn/images/v2beta-screenshot-sendto.png)
![https://fseye.googlecode.com/svn/images/v2beta-screenshot-close.png](https://fseye.googlecode.com/svn/images/v2beta-screenshot-close.png)