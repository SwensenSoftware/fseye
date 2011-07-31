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

        pairs |> Seq.choose (fst>>id), pairs |> Seq.choose (snd>>id)
