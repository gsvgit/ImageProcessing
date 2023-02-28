namespace ImageProcessing

open ImageFolderProcessing
open CPUImageProcessing
open ArgCommands
open Argu

module Main =

    [<EntryPoint>]
    let main argv =
        let parser = ArgumentParser.Create<CliArguments>(programName = "aaa")

        match parser.ParseCommandLine argv with
            | p when p.Contains(Rotate) ->
                let input = first <| p.GetResult(Rotate)
                let output = second <| p.GetResult(Rotate)
                let clockwise = third <| p.GetResult(Rotate)

                match System.IO.Path.GetExtension input with
                | "" ->
                    processAllFiles input output (rotate2DArray clockwise)
                | _ ->
                    let array2D = loadAs2DArray input
                    let result = rotate2DArray clockwise array2D
                    save2DArrayAsImage result output
            | p when p.Contains(Filter) ->
                let input =  p.GetResult(Filter) |> first
                let output = p.GetResult(Filter) |> second
                let kernel = p.GetResult(Filter) |> third |>  kernelParser

                match System.IO.Path.GetExtension input with
                | "" ->
                    processAllFiles input output (applyFilterTo2DArray kernel)
                | _ ->
                    let array2D = loadAs2DArray input
                    let result = applyFilterTo2DArray kernel array2D
                    save2DArrayAsImage result output
            | _ ->
                printfn "%s" "Error"
        0
