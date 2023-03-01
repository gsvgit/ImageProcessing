namespace ImageProcessing

open Argu
open ArgCommands
open Helper
open ImageFolderProcessing
open CPUImageProcessing

module Main =

    [<EntryPoint>]
    let main argv =

        let errorHandler =
            ProcessExiter(
                colorizer =
                    function
                    | ErrorCode.HelpText -> None
                    | _ -> Some System.ConsoleColor.DarkYellow
            )

        let parser = ArgumentParser.Create<ClIArguments>(errorHandler = errorHandler)

        match parser.ParseCommandLine argv with

        | res when res.Contains(Paths) && res.Contains(Process) ->

            let inputPath, outputPath = res.GetResult(Paths)

            let processor =
                res.GetResult(Process) |> List.map processorParser |> funcComposition

            match System.IO.File.Exists inputPath with
            | false -> processor |> processAllFiles inputPath outputPath
            | true ->
                let image = loadAs2DArray inputPath
                let processedImage = processor image
                save2DArrayAsImage processedImage outputPath
        | _ -> printfn $"Unexpected command.\n {parser.PrintUsage()}"

        0
