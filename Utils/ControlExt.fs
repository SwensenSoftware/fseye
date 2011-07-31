(*
Copyright 2011 Stephen Swensen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*)

namespace Swensen.Utils

module Control =
    let inline update< ^a when ^a :(member BeginUpdate: unit -> unit) and ^a : (member EndUpdate: unit -> unit)> (this: ^a) f =
        (^a : (member BeginUpdate: unit -> unit) (this))
        f()
        (^a : (member EndUpdate: unit -> unit) (this))