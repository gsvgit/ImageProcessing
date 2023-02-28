namespace ImageProcessing

open Argu
open ArgCommands
open Helper
open ImageFolderProcessing
open CPUImageProcessing

module Main =

    [<EntryPoint>]
    let main argv =

        let parser = ArgumentParser.Create<ClIArguments>()

        match parser.ParseCommandLine argv with
        | res when res.Contains(Rotate) ->

            let tripleResult = res.GetResult(Rotate)
            let inputPath = first tripleResult
            let outputPath = second tripleResult
            let isClockwise = third tripleResult

            match System.IO.Path.GetExtension inputPath with
            | "" -> rotate2DArray isClockwise |> processAllFiles inputPath outputPath
            | _ ->
                let array2D = loadAs2DArray inputPath
                let editedArray2D = rotate2DArray isClockwise array2D
                save2DArrayAsImage editedArray2D outputPath

        | res when res.Contains(Filter) ->
            let tripleResult = res.GetResult(Filter)
            let inputPath = first tripleResult
            let outputPath = second tripleResult
            let kernelArray2D = third tripleResult |> kernelParser

            match System.IO.Path.GetExtension inputPath with
            | "" -> applyFilterTo2DArray kernelArray2D |> processAllFiles inputPath outputPath
            | _ ->
                let array2D = loadAs2DArray inputPath
                let editedArray2D = applyFilterTo2DArray kernelArray2D array2D
                save2DArrayAsImage editedArray2D outputPath
        | _ -> printfn $"Unexpected command.\n {parser.PrintUsage()}"

        0
