module ImageProcessingBenchmarks

open BenchmarkDotNet.Attributes
open Brahma.FSharp
open ImageProcessing.ImageProcessing

[<AbstractClass>]
type FilterBenchmark() =
    member val Image: Image = Unchecked.defaultof<_> with get, set

    [<Params(100, 200, 500, 1000, 2000, 4000, 8000)>]
    member val Size = 0 with get, set

    [<GlobalSetup>]
    abstract member GlobalSetup: unit -> unit

type CpuFilterBench() =
    inherit FilterBenchmark()

    [<Params("CPUSequential", "CPUParallel", "CPUParallelRows")>]
    member val Device = "" with get, set

    override this.GlobalSetup() =
        let rng = System.Random(42)
        let data = Array.init (this.Size * this.Size) (fun _ -> byte (rng.Next 256))
        this.Image <- Image(data, this.Size, this.Size, "bench")

    [<Benchmark>]
    member this.Run() =
        match this.Device with
        | "CPUSequential" -> applyFilter gaussianBlurKernel this.Image |> ignore
        | "CPUParallel" -> applyFilterCpuParallel gaussianBlurKernel this.Image |> ignore
        | "CPUParallelRows" -> applyFilterCpuParallelRows gaussianBlurKernel this.Image |> ignore
        | _ -> failwithf "Unknown CPU device: %s" this.Device

type GpuFilterBench() =
    inherit FilterBenchmark()

    let mutable applier : (list<float32[][]> -> Image -> Image) option = None

    [<Params("POCL", "Nvidia", "IntelGPU")>]
    member val Device = "" with get, set

    [<Params(8, 16, 32, 64, 128, 256)>]
    member val LWS = 0 with get, set

    override this.GlobalSetup() =
        let rng = System.Random(42)
        let data = Array.init (this.Size * this.Size) (fun _ -> byte (rng.Next 256))
        this.Image <- Image(data, this.Size, this.Size, "bench")

        let platform =
            match this.Device with
            | "POCL" -> Platform.Custom "Portable*"
            | "Nvidia" -> Platform.Nvidia
            | "IntelGPU" -> Platform.Intel
            | _ -> failwithf "Unknown GPU device: %s" this.Device

        let device = ClDevice.GetAvailableDevices(platform = platform) |> Seq.head
        printfn $"  Device: %s{device.Name}"
        let ctx = ClContext(device)
        applier <- Some(applyFiltersGPU ctx this.LWS)

    [<Benchmark>]
    member this.Run() =
        match applier with
        | Some applier -> applier [gaussianBlurKernel] this.Image |> ignore
        | None -> failwith "GPU applier not initialized"
