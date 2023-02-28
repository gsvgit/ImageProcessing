namespace ImageProcessing

open ImageFolderProcessing
open CPUImageProcessing
open ArguCommands
open Argu
open System

module Main =

    [<EntryPoint>]
    let main argv =
        let parser = ArgumentParser.Create<CliArguments>(programName = "aaa")
        (*let usage = parser.PrintUsage()
        let results = parser.Parse [| "--detach" ; "--listener" ; "localhost" ; "8080" |]
        //let all = results.GetAllResults()
        let detach = results.Contains Detach
        let listener = results.GetResults Listener*)
        match parser.ParseCommandLine argv with
            | p when p.Contains(Rotate) ->
                let input = first <| p.GetResult(Rotate)
                let output = second <| p.GetResult(Rotate)
                let clockwise = third <| p.GetResult(Rotate)
                let array2D = loadAs2DArray input
                let result = rotate2DArray clockwise array2D
                save2DArrayAsImage result output
            | _ ->
                printfn "%s" "Error"
        0
