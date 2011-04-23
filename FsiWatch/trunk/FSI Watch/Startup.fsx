#load "InteractiveSessionExt.fs"
#load "ResultModel.fs"
#load "MemberModel.fs"
#load "WatchTreeView.fs"
open Swensen.Watch.Forms
#load "WatchForm.fs"
open Swensen.Watch.Forms
open Swensen.Watch.Fsi
#load "FsiWatch.fs"

//----testing todo, need to refactor out to model view
//lazy load children
//loads private and public instance properties and fields
//loads fields in alphebetic order, then properties in alphebetic order; case ignored
//loads enumerables
//does not load static properties

