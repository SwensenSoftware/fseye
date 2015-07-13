# Release Notes #

## v2.1.0, 6/14/14 ##

A new DataGridView-based plugin.

  * [Issue 30](https://code.google.com/p/fseye/issues/detail?id=30), Create and publish NuGet package for plugin authors to develop against
  * [Issue 43](https://code.google.com/p/fseye/issues/detail?id=43), Add DataGridView-based plugin

## v2.0.1, 9/10/13 ##

A couple high-priority bug fixes.

  * [Issue 39](https://code.google.com/p/fseye/issues/detail?id=39), Incorrect version of multiple top-level bindings of the same name from the same interactive submission is displayed
  * [Issue 40](https://code.google.com/p/fseye/issues/detail?id=40), Occasional "SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize" exception kills FSI

## v2.0.0, 12/9/12 ##

Final non-beta release of v2.0.0, which includes the new plugin system as well as new support for Mono.

  * [Issue 37](https://code.google.com/p/fseye/issues/detail?id=37), Add support for Mono

## v2.0.0 beta 4, 8/9/12 ##

Critical bug fix for issue affecting both plugin authors and users.

  * [Issue 36](https://code.google.com/p/fseye/issues/detail?id=36), FsEye Plugin Loading Error when loading plugins marked as from "unsafe" internet location (affects included plugins)

## v2.0.0 beta 3, 7/31/12 ##

This release includes fixes and enhancements affecting plugin authors.

  * [Issue 34](https://code.google.com/p/fseye/issues/detail?id=34), Include FsEye.dll dir in plugin search
  * [Issue 35](https://code.google.com/p/fseye/issues/detail?id=35), Plugins fail to load if the plugin dir contains any non-dlls

## v2.0.0 beta 2, 7/23/12 ##

This release includes a high-priority fix affecting plugin authors.

  * [Issue 32](https://code.google.com/p/fseye/issues/detail?id=32), IWatchViewer.Watch signature needs to be changed slightly
  * [Issue 33](https://code.google.com/p/fseye/issues/detail?id=33), Improve plugin loading error message for ReflectionTypeLoadException

## v2.0.0 beta 1, 7/20/12 ##

This beta release primarily features work on a plugin system for FsEye. See the FsEye2Beta wiki page for documentation.

  * [Issue 25](https://code.google.com/p/fseye/issues/detail?id=25), Create plugin architecture and manager
  * [Issue 26](https://code.google.com/p/fseye/issues/detail?id=26), Create "View PropertyGrid" right-click context menu action
  * [Issue 27](https://code.google.com/p/fseye/issues/detail?id=27), Should not show "Copy Value" context menu item for organizational nodes such as "Non-public" and "Rest"

## v1.0.1, 4/13/12 ##

Maintenance release.

  * [Issue 19](https://code.google.com/p/fseye/issues/detail?id=19), Crashes when trying to see details of System.Windows.Forms.WebBrowser
  * [Issue 20](https://code.google.com/p/fseye/issues/detail?id=20), Bug: exception loading nodes for Ole object

## v1.0.0 final, 8/10/11 ##

  * [Issue 8](https://code.google.com/p/fseye/issues/detail?id=8), Implement Babel Icons
  * [Issue 9](https://code.google.com/p/fseye/issues/detail?id=9), Honor DebuggerBrowsable(DebuggerBrowsableState.Never)
  * [Issue 10](https://code.google.com/p/fseye/issues/detail?id=10), Remove Unquote dependency
  * [Issue 11](https://code.google.com/p/fseye/issues/detail?id=11), XML doc file missing
  * [Issue 12](https://code.google.com/p/fseye/issues/detail?id=12), Call Member nodes should only refresh the first time they are selected
  * [Issue 13](https://code.google.com/p/fseye/issues/detail?id=13), IEnumerator valued Watch.Children may only be enumerated once
  * [Issue 14](https://code.google.com/p/fseye/issues/detail?id=14), Performance slow when objects that require complex printing are submitted to FSI

## v1.0.0 beta 2, 6/02/11 ##

Feature enhancements and defect fixes.

  * [Issue 1](https://code.google.com/p/fseye/issues/detail?id=1), Defect: Should automatically reset Eye when the underlying WatchForm becomes disposed due to an exception
  * [Issue 2](https://code.google.com/p/fseye/issues/detail?id=2), Enhancement: Include method members in addition to field and property members as children of a watch
  * [Issue 3](https://code.google.com/p/fseye/issues/detail?id=3), Enhancement: Include fields, properties, and method from inherited base-class and explicit interfaces of a watch
  * [Issue 4](https://code.google.com/p/fseye/issues/detail?id=4), Enhancement: Upon loading, watches should display and load child watches for true value type when not null
  * [Issue 5](https://code.google.com/p/fseye/issues/detail?id=5), Enhancement: Add right-click context menu Copy Value function for loaded watches
  * [Issue 6](https://code.google.com/p/fseye/issues/detail?id=6), Enhancement: Add right-click context menu Refresh function for root watches
  * [Issue 7](https://code.google.com/p/fseye/issues/detail?id=7), Enhancement: Add right-click context menu Remove function for root watches and archives

## v1.0.0 beta 1, 5/07/11 ##

Initial release