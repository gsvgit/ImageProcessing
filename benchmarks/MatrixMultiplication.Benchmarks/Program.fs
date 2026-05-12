open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open Brahma.FSharp
open MatrixMultiplicationBenchmarks

[<EntryPoint>]
let main argv =
    let bdnArgs, device =
        let idx = argv |> Array.tryFindIndex (fun a -> a = "--device")
        match idx with
        | Some i when i + 1 < argv.Length ->
            let name = argv.[i + 1]
            let dev =
                match name.ToLower() with
                | "nvidia" ->
                    ClDevice.GetAvailableDevices(platform = Platform.Nvidia) |> Seq.tryHead
                | "intel" ->
                    ClDevice.GetAvailableDevices(platform = Platform.Intel) |> Seq.tryHead
                | "cpu" | "pocl" ->
                    ClDevice.GetAvailableDevices(platform = Platform.Custom "Portable*") |> Seq.tryHead
                | _ ->
                    printfn $"Unknown device '{name}', using first appropriate"
                    None
            Array.append argv.[..i - 1] argv.[i + 2 ..], dev
        | _ -> argv, None

    let dev =
        match device with
        | Some d -> d
        | None ->
            printfn "Using first appropriate OpenCL device"
            ClDevice.GetFirstAppropriateDevice()

    printfn $"Device: %s{dev.Name}"
    DeviceConfig.device <- Some dev

    let config = DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true)
    BenchmarkSwitcher([|
        typeof<K0Benchmark>
        typeof<K1Benchmark>
        typeof<K2Benchmark>
        typeof<K3Benchmark>
        typeof<K4Benchmark>
    |]).Run(bdnArgs, config) |> ignore

    0
