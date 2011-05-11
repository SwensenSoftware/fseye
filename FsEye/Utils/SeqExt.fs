namespace Swensen.SeqExt

module Seq =
    let distinctByResolve primaryKey resolveCollision values =
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

        cache.Values :> seq<_>