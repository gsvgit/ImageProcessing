namespace ImageProcessing

open Argu
open Argu.ArguAttributes
open Brahma.FSharp

type Platforms = CPU = 1 | NVidia = 2 | IntelGPU = 3 | AnyGPU = 4

[<CliPrefix(CliPrefix.DoubleDash)>]
[<NoAppSettings>]
type ImageProcessingArguments =
    | Input of string
    | Output of string
    | Platform of Platforms
    | WorkGroupSize of uint
    | MatrixSize of uint    
    with
    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _  -> "Image to process."
            | Output _ -> "File to store result."
            | Platform _ -> "Where to run."
            | WorkGroupSize _ -> "Work group size."
            | MatrixSize _ -> "Number of columns (or rows). We use square matrices."
module Main =
    //let pathToExamples = "/home/gsv/Projects/TestProj2020/src/ImgProcessing/Examples"
    //let inputFolder = System.IO.Path.Combine(pathToExamples, "input")
    //let outputFolder = System.IO.Path.Combine(pathToExamples, "output")

    //let demoFileName = "armin-djuhic-ohc29QXbS-s-unsplash.jpg"
    //let demoFile =
    //    System.IO.Path.Combine(inputFolder, demoFileName)

    [<EntryPoint>]
    let main (argv: string array) =
        let parser = ArgumentParser.Create<ImageProcessingArguments>(programName = "ImageProcessing")
        let results = parser.ParseCommandLine argv
        let input = results.GetResult(Input, defaultValue = "")
        let output = results.GetResult(Output, defaultValue = "out.jpg")
        let platform = results.GetResult(Platform, defaultValue = Platforms.CPU)
        let workGroupSize = results.GetResult(WorkGroupSize, defaultValue = 64u)
        let matrixSize = results.GetResult(MatrixSize, defaultValue = 512u)

        let filters = [
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.edgesKernel
        ]

        
        let applyFiltersOnGPU =
            let device =
                match platform with 
                | Platforms.AnyGPU -> ClDevice.GetFirstAppropriateDevice()
                | _ -> 
                    let platform =
                        match platform with 
                        | Platforms.NVidia -> Platform.Nvidia
                        | Platforms.IntelGPU -> Platform.Intel
                    ClDevice.GetAvailableDevices(platform = platform)
                    |> Seq.head
            printfn $"Device: %A{device.Name}"

            let context = ClContext(device)
            ImageProcessing.applyFiltersGPU context 64
(*
        match platform with
        | Platforms.CPU -> 
            let mutable image = ImageProcessing.loadAs2DArray input
            printfn $"Device: CPU"
            let start = System.DateTime.Now
            for filter in filters do
                image <- ImageProcessing.applyFilter filter image
            printfn $"CPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"
            ImageProcessing.save2DByteArrayAsImage image output
        | _ ->             
            let start = System.DateTime.Now
            let grayscaleImage = ImageProcessing.loadAsImage input
            printfn $"Image reading time: {(System.DateTime.Now - start).TotalMilliseconds} ms"

            let start = System.DateTime.Now
            let result = applyFiltersOnGPU filters grayscaleImage
            printfn $"GPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"            
            printfn $"R: %A{result}"
            ImageProcessing.saveImage result output
*)

        let start = System.DateTime.Now

        Streaming.processAllFiles input output [
            applyFiltersOnGPU filters
        ]

        printfn
            $"TotalTime = %f{(System.DateTime.Now
                              - start)
                                 .TotalMilliseconds}"

        
        0
