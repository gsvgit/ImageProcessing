open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open ImageProcessingBenchmarks

[<EntryPoint>]
let main argv =
    let config = DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true)
    BenchmarkSwitcher([|
        typeof<CpuFilterBench>
        typeof<GpuFilterBench>
    |]).Run(argv, config) |> ignore
    0
