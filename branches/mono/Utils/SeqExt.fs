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

module internal Seq =
    let distinctByResolve primaryKey resolveCollision values = seq { //make lazy seq? (YES, Seq.distinct does)
        let cache = System.Collections.Generic.Dictionary<_,_>(HashIdentity.Structural)
        for canidateValue in values do
            let key = primaryKey canidateValue 
            match cache.TryGetValue(key) with
            | true, existingValue -> //collision
                match resolveCollision existingValue canidateValue with
                | x when x >= 0 -> () //if existing equal or greater, keep it
                | _ -> //canidate key wins in collision resolution
                    cache.Remove(key) |> ignore
                    cache.Add(key,canidateValue)
            | false, _ ->
                cache.Add(key, canidateValue)

        yield! cache.Values }

    let partition condition values =     
        let pairs = seq {
            for i in values do
                if condition i then
                    yield Some(i), None
                else
                    yield None, Some(i) }

        pairs |> Seq.choose fst, pairs |> Seq.choose snd
