namespace ImageProcessing

open Argu
open ArgCommands
open Helper
open ImageFolderProcessing
open CPUImageProcessing

module Main =

    [<EntryPoint>]
    let main argv =
        (*let parser = ArgumentParser.Create<ClIArguments>()

        match parser.ParseCommandLine argv with
        | res when res.Contains(Rotate) ->

            let tripleResult = res.GetResult(Rotate)
            let inputPath = first tripleResult
            let outputPath = second tripleResult
            let isClockwise = third tripleResult |> rotationParser

            match System.IO.File.Exists inputPath with
            | false -> rotate2DArray isClockwise |> processAllFiles inputPath outputPath
            | true ->
                let array2D = loadAs2DArray inputPath
                let editedArray2D = rotate2DArray isClockwise array2D
                save2DArrayAsImage editedArray2D outputPath

        | res when res.Contains(Filter) ->
            let tripleResult = res.GetResult(Filter)
            let inputPath = first tripleResult
            let outputPath = second tripleResult
            let kernelArray2D = third tripleResult |> kernelParser

            match System.IO.File.Exists inputPath with
            | false -> applyFilterTo2DArray kernelArray2D |> processAllFiles inputPath outputPath
            | true ->
                let array2D = loadAs2DArray inputPath
                let editedArray2D = applyFilterTo2DArray kernelArray2D array2D
                save2DArrayAsImage editedArray2D outputPath

        | res when res.Contains(Edit) ->
            let tripleResult = res.GetResult(Edit)
            let inputPath = first tripleResult
            let outputPath = second tripleResult
            let modificationList = third tripleResult |> List.map modiParser

            match System.IO.File.Exists inputPath with
            | false -> applyFilterTo2DArray kernelArray2D |> processAllFiles inputPath outputPath
            | true ->
                let array2D = loadAs2DArray inputPath
                let editedArray2D = applyFilterTo2DArray kernelArray2D array2D
                save2DArrayAsImage editedArray2D outputPath
        | _ -> printfn $"Unexpected command.\n {parser.PrintUsage()}"*)
        let f1 x = x + 1
        let f2 x = x + 2
        let f3 x = x + 3
        let megaFunc = processManyModi [f1;f2;f3]
        printf $"%A{megaFunc 0}"

        0
