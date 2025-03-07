namespace ImageProcessing

open Argu
open Argu.ArguAttributes
open Brahma.FSharp

type Platforms = CPU = 1 | NVidia = 2 | IntelGPU = 3 | AnyGPU = 4

[<CliPrefix(CliPrefix.DoubleDash)>]
[<NoAppSettings>]
type ImageProcessingArguments =
    | [<Mandatory>] Input of string
    | Output of string
    | Platform of Platforms
    with
    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _  -> "Image to process."
            | Output _ -> "File to store result."
            | Platform _ -> "Where to run."
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
        let inputFile = results.GetResult(Input, defaultValue = "")
        let outputFile = results.GetResult(Output, defaultValue = "out.jpg")
        let platform = results.GetResult(Platform, defaultValue = Platforms.CPU)
        
        let filters = [
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.edgesKernel
        ]

        
        match platform with
        | Platforms.CPU -> 
            let mutable image = ImageProcessing.loadAs2DArray inputFile
            let start = System.DateTime.Now
            for filter in filters do
                image <- ImageProcessing.applyFilter filter image
            printfn $"CPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"
            ImageProcessing.save2DByteArrayAsImage image outputFile
        | _ -> 
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
            let applyFiltersOnGPU = ImageProcessing.applyFiltersGPU context 64

        
            let start = System.DateTime.Now
            let grayscaleImage = ImageProcessing.loadAsImage inputFile
            printfn $"Image reading time: {(System.DateTime.Now - start).TotalMilliseconds} ms"

            let start = System.DateTime.Now
            let result = applyFiltersOnGPU filters grayscaleImage
            printfn $"GPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"            
            ImageProcessing.saveImage result outputFile

(*
        let start = System.DateTime.Now

        Streaming.processAllFiles inputFolder outputFolder [
            //applyFiltersOnNvGPU filters
            applyFiltersOnIntelGPU filters
        ]

        printfn
            $"TotalTime = %f{(System.DateTime.Now
                              - start)
                                 .TotalMilliseconds}"
*)
        
        0
