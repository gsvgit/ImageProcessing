open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open MatrixMultiplicationBenchmarks

[<EntryPoint>]
let main argv =
    let config = DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true)
    BenchmarkSwitcher([|
        typeof<K0Benchmark>
        typeof<K1Benchmark>
        typeof<K2Benchmark>
        typeof<K3Benchmark>
        typeof<K4Benchmark>
    |]).Run(argv, config) |> ignore

    0
