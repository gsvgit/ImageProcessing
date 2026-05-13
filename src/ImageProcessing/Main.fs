namespace ImageProcessing

open Argu
open Argu.ArguAttributes
open Brahma.FSharp
open System.Diagnostics

type Platforms =
    | CPUSequential = 1
    | CPUParallel = 2
    | CPUOpencl = 3
    | Nvidia = 4
    | IntelGPU = 5
    | AnyGPU = 6

[<CliPrefix(CliPrefix.DoubleDash)>]
[<NoAppSettings>]
type ImageProcessingArguments =
    | Input of string
    | Output of string
    | Platform of Platforms
    | WorkGroupSize of uint
    | Workers of list<Platforms>
    with
    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _       -> "Image or directory to process."
            | Output _      -> "Output file or directory."
            | Platform _    -> "Processor: CPUSequential / CPUParallel / CPUOpencl / Nvidia / IntelGPU / AnyGPU"
            | WorkGroupSize _ -> "Work group size for GPU/OpenCL (default: 64)"
            | Workers _     -> "Streaming mode. List of parallel workers for filters application."

module Main =
    let defaultFilters = [
        ImageProcessing.gaussianBlurKernel
        ImageProcessing.gaussianBlurKernel
        ImageProcessing.gaussianBlurKernel
        ImageProcessing.gaussianBlurKernel
        ImageProcessing.edgesKernel
    ]

    let createGPUApplier platform filters lws =
        let device =
            match platform with
            | Platforms.CPUOpencl ->
                ClDevice.GetAvailableDevices(platform = Platform.Custom "Portable*") |> Seq.head
            | Platforms.Nvidia ->
                ClDevice.GetAvailableDevices(platform = Platform.Nvidia) |> Seq.head
            | Platforms.IntelGPU ->
                ClDevice.GetAvailableDevices(platform = Platform.Intel) |> Seq.head
            | Platforms.AnyGPU ->
                ClDevice.GetFirstAppropriateDevice()
            | _ -> failwithf "Not a GPU platform: %A" platform
        printfn $"  Device: %s{device.Name}"
        let ctx = ClContext(device)
        (ImageProcessing.applyFiltersGPU ctx lws) filters

    [<EntryPoint>]
    let main (argv: string array) =
        let parser = ArgumentParser.Create<ImageProcessingArguments>(programName = "ImageProcessing")
        let results = parser.ParseCommandLine argv
        let input = results.GetResult(Input, defaultValue = "")
        let output = results.GetResult(Output, defaultValue = "out.jpg")
        let platform = results.GetResult(Platform, defaultValue = Platforms.CPUSequential)
        let workGroupSize = results.GetResult(WorkGroupSize, defaultValue = 64u)
        let workers = results.GetResult(Workers, defaultValue = [])

        let filters = defaultFilters

        let runSingleImage () =
            printfn "\n========== Single Image ==========\n"

            match platform with
            | Platforms.CPUSequential ->
                let sw = Stopwatch.StartNew()
                let image = ImageProcessing.loadAsImage input
                let loadTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Load                : {loadTime,8:F1} ms"

                sw.Restart()
                let mutable current = image
                for filter in filters do
                    current <- ImageProcessing.applyFilter filter current
                let processTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Process             : {processTime,8:F1} ms"

                sw.Restart()
                ImageProcessing.saveImage current output
                let saveTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Save                : {saveTime,8:F1} ms"
                printfn $"  ----------------------------------"
                printfn $"  Total               : {loadTime + processTime + saveTime,8:F1} ms"

            | Platforms.CPUParallel ->
                let sw = Stopwatch.StartNew()
                let image = ImageProcessing.loadAsImage input
                let loadTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Load                : {loadTime,8:F1} ms"

                sw.Restart()
                let mutable current = image
                for filter in filters do
                    current <- ImageProcessing.applyFilterCpuParallel filter current
                let processTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Process             : {processTime,8:F1} ms"

                sw.Restart()
                ImageProcessing.saveImage current output
                let saveTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Save                : {saveTime,8:F1} ms"
                printfn $"  ----------------------------------"
                printfn $"  Total               : {loadTime + processTime + saveTime,8:F1} ms"

            | Platforms.CPUOpencl
            | Platforms.Nvidia
            | Platforms.IntelGPU
            | Platforms.AnyGPU ->
                let device =
                    match platform with
                    | Platforms.CPUOpencl ->
                        ClDevice.GetAvailableDevices(platform = Platform.Custom "Portable*") |> Seq.head
                    | Platforms.Nvidia ->
                        ClDevice.GetAvailableDevices(platform = Platform.Nvidia) |> Seq.head
                    | Platforms.IntelGPU ->
                        ClDevice.GetAvailableDevices(platform = Platform.Intel) |> Seq.head
                    | _ -> ClDevice.GetFirstAppropriateDevice()
                printfn $"  Device: %s{device.Name}"

                let sw = Stopwatch.StartNew()
                let image = ImageProcessing.loadAsImage input
                let loadTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Load                : {loadTime,8:F1} ms"

                let applier = createGPUApplier platform filters (int workGroupSize)

                sw.Restart()
                let result = applier image
                let processTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Process             : {processTime,8:F1} ms"

                sw.Restart()
                ImageProcessing.saveImage result output
                let saveTime = sw.Elapsed.TotalMilliseconds
                printfn $"  Save                : {saveTime,8:F1} ms"
                printfn $"  ----------------------------------"
                printfn $"  Total               : {loadTime + processTime + saveTime,8:F1} ms"

            | _ ->
                printfn $"Unknown platform: %A{platform}"
                exit 1

            0

        let runStreaming () =
            printfn "\n========== Streaming ==========\n"
            printfn $"  Workers: %A{workers}\n"

            if not (System.IO.Directory.Exists input) then
                printfn "Error: --input must be a directory for streaming mode"
                1
            else
                let appliers = workers |> List.map (fun p ->
                    match p with
                    | Platforms.CPUSequential ->
                        printfn $"  Worker: CPUSequential"
                        ImageProcessing.applyFiltersCPU filters
                    | Platforms.CPUParallel ->
                        printfn $"  Worker: CPUParallel"
                        ImageProcessing.applyFiltersCPUParallel filters
                    | Platforms.CPUOpencl
                    | Platforms.Nvidia
                    | Platforms.IntelGPU
                    | Platforms.AnyGPU ->
                        createGPUApplier p filters (int workGroupSize)
                    | _ ->
                        printfn $"  Unknown platform: %A{p}"
                        failwithf "Unknown platform: %A{p}"
                )

                printfn ""
                let sw = Stopwatch.StartNew()
                Streaming.processAllFiles input output appliers
                let totalTime = sw.Elapsed.TotalMilliseconds
                printfn $"  ----------------------------------"
                printfn $"  Total               : {totalTime,8:F1} ms"
                0

        match workers with
        | _ :: _ -> runStreaming ()
        | [] -> runSingleImage ()
