FsEye is implemented using the .NET WinForms API in F#. From the forward facing `Eye` class, through each WinForms component, to the back-end model, FsEye was specifically designed to be flexible enough to be adopted by other projects interested in adding visual object inspectors to their .NET REPL environments. The author is very interested in accommodating such projects and implementing additional flexibility to that end (for example, the Type printing used in the model is F# specific, this may be an area where a pluggable Type printer may bring value).

The following table shows instance members in the public API on the left-hand side row headers, and class names in the column headers on the top listed left-to-right, outer-most-to-innermost in the delegation chain. A mark in a cell indicates that the given class exposes the given member.

| | **Eye** | **WatchForm** | **WatchPanel** | **WatchTreeView** |
|:|:--------|:--------------|:---------------|:------------------|
| **Watch(name, value, ty)** | x       | x             | x              | x                 |
| **Watch(name, value)** | x       | x             | x              | x                 |
| **Archive(label)** | x       | x             | x              | x                 |
| **Archive()** | x       | x             | x              | x                 |
| **ClearArchives()** | x       | x             | x              | x                 |
| **ClearWatches()** | x       | x             | x              | x                 |
| **ClearAll()** | x       | x             | x              | x                 |
| **AsyncBreak()** | x       | x             | x              |                   |
| **AsyncContinue()** | x       | x             | x              |                   |