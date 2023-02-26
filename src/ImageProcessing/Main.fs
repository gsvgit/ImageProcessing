namespace ImageProcessing

open ImageFolderProcessing
open CPUImageProcessing

module Main =

    [<EntryPoint>]
    let main args =
        printfn "Arguments passed to function : %A" args
        0
