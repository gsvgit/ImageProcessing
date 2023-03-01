module Helper

let funcComposition funcList = funcList |> List.fold (>>) id
