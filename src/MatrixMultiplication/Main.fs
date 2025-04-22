namespace ImageProcessing

open Argu
open Argu.ArguAttributes
open Brahma.FSharp
open FSharp.Quotations.Evaluator.QuotationEvaluationExtensions

type Platforms = CPU = 1 | CPUParallel = 2 | NVidia = 3 | IntelGPU = 4 | AnyGPU = 5 | PoclCPU = 6
type MatrixTypes = MT_byte = 1 | MT_int = 2 | MT_float32 = 3 | MT_OptInt = 4 | MT_float64 = 5
type Semirings = MinPlus = 1 | Arithmetic = 2

[<CliPrefix(CliPrefix.DoubleDash)>]
[<NoAppSettings>]
type ImageProcessingArguments =
    | Platform of Platforms
    | WorkGroupSize of uint
    | WorkPerThread of uint
    | MatrixSize of uint
    | Kernel of Matrices.Kernels
    | Check of bool
    | MatrixType of MatrixTypes 
    | Semiring of Semirings
    | NumToRun of uint
    | Tune of bool

    with
    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Platform _ -> "Where to run."
            | WorkGroupSize _ -> "Work group size."
            | WorkPerThread _ -> "Number of result matrix cells computed by one thread."
            | MatrixSize _ -> "Number of columns (or rows). We use square matrices."
            | Kernel _ -> "Kernel to run."
            | Check _ -> "Whether check result correctness."
            | MatrixType _ -> "Type of elements of matrices."
            | Semiring _ -> "Semiring to operate with matrices."
            | NumToRun _ -> "How many times run the kernel specified."
            | Tune _ -> "Run parameters tuning, not benchmarks."

module Main =
    let optIntZero = <@None@>

    [<EntryPoint>]
    let main (argv: string array) =
        let parser = ArgumentParser.Create<ImageProcessingArguments>(programName = "MatrixMultiplication")
        let results = parser.ParseCommandLine argv
        let platform = results.GetResult(Platform, defaultValue = Platforms.CPU)
        let workGroupSize = results.GetResult(WorkGroupSize, defaultValue = 64u)
        let workPerThread = results.GetResult(WorkPerThread, defaultValue = 1u)
        let matrixSize = results.GetResult(MatrixSize, defaultValue = 512u)
        let kernel = results.GetResult(Kernel, defaultValue = Matrices.Kernels.K1)
        let check = results.GetResult(Check, defaultValue = false)
        let matrixType = results.GetResult(MatrixType, defaultValue = MatrixTypes.MT_int)
        let semiring = results.GetResult(Semiring, defaultValue = Semirings.Arithmetic)
        let numToRun = results.GetResult(NumToRun, defaultValue = 1u)
        let tune = results.GetResult(Tune, defaultValue = false)


        let time mXm checker m1 m2 =
            let start = System.DateTime.Now
            let res,_ = mXm m1 m2
            printfn $"Processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"
            if check then checker res

        let cpuParallelKernel (opAdd:Quotations.Expr<_>) (opMult:Quotations.Expr<_>) zero = 
            Matrices.cpuParallelMxM 
                (opAdd.Compile())
                (opMult.Compile())
                (FSharp.Quotations.Evaluator.QuotationEvaluator.Evaluate zero)

        let cpuKernel (opAdd:Quotations.Expr<_>) (opMult:Quotations.Expr<_>) zero = 
            Matrices.cpuMxM 
                (opAdd.Compile())
                (opMult.Compile())
                (FSharp.Quotations.Evaluator.QuotationEvaluator.Evaluate zero)

        let gpuKernel opAdd opMult zero =

            let device =
                match platform with 
                | Platforms.AnyGPU -> ClDevice.GetFirstAppropriateDevice()
                | _ -> 
                    let platform =
                        match platform with 
                        | Platforms.NVidia -> Platform.Nvidia
                        | Platforms.IntelGPU -> Platform.Intel
                        | Platforms.PoclCPU -> Platform.Custom "Portable*"
                    ClDevice.GetAvailableDevices(platform = platform)
                    |> Seq.head

            printfn $"Device: %A{device.Name}"

            let context = ClContext(device)

            if tune 
            then ImageProcessing.Tuner.tune kernel context numToRun opAdd opMult zero
            else Matrices.applyMultiplyGPU kernel context numToRun workGroupSize workPerThread opAdd opMult zero
        
        let inline mXmKernel opAdd opMult zero = 
            match platform with 
            | Platforms.CPUParallel -> cpuParallelKernel opAdd opMult zero
            | Platforms.CPU         -> cpuKernel opAdd opMult zero
            | _ -> gpuKernel opAdd opMult zero
       
        match matrixType with 
        | MatrixTypes.MT_byte ->
            let m1 = Matrices.getRandomByteMatrix matrixSize
            let m2 = Matrices.getRandomByteMatrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    mXmKernel <@(+)@> <@( * )@> <@0uy@>
                    , Matrices.check (+) ( * ) 0uy m1 m2
                | Semirings.MinPlus -> 
                    mXmKernel <@min@> <@(+)@> <@255uy@>
                    , Matrices.check min (+) 255uy m1 m2
                | x -> failwithf $"Unexpected semiring {x}."
            
            time mXm checker m1 m2

        | MatrixTypes.MT_int -> 
            let m1 = Matrices.getRandomIntMatrix matrixSize
            let m2 = Matrices.getRandomIntMatrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    mXmKernel <@(+)@> <@( * )@> <@0@>
                    , Matrices.check (+) ( * ) 0 m1 m2
                | Semirings.MinPlus -> 
                    mXmKernel <@min@> <@(+)@> <@System.Int32.MaxValue@>
                    , Matrices.check min (+) System.Int32.MaxValue m1 m2
                | x -> failwithf $"Unexpected semiring {x}."
            
            time mXm checker m1 m2

        | MatrixTypes.MT_OptInt -> 
            let m1 = Matrices.getRandomOptionIntMatrix matrixSize
            let m2 = Matrices.getRandomOptionIntMatrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    let opAdd =
                        <@fun a b ->
                            match a, b with 
                            | Some x, Some y -> Some (x + y)
                            | _ -> None
                            @>
                    let opMult =
                        <@fun a b -> 
                            match a, b with 
                            | Some x, Some y -> Some (x * y)
                            | _ -> None
                            @>
                    mXmKernel opAdd opMult optIntZero
                    , Matrices.check (opAdd.Compile()) (opMult.Compile()) None m1 m2
                | Semirings.MinPlus -> 
                    let opAdd =
                        <@fun a b ->
                            match a, b with 
                            | Some x, Some y -> Some (min x y)
                            | None, Some x
                            | Some x, None -> Some x
                            | None, None -> None
                            @>
                    let opMult =
                        <@fun a b -> 
                            match a, b with 
                            | Some x, Some y -> Some (x + y)
                            | _ -> None
                            @>
                    mXmKernel opAdd opMult optIntZero 
                    , Matrices.check (opAdd.Compile()) (opMult.Compile()) None m1 m2
                | x -> failwithf $"Unexpected semiring {x}."

            time mXm checker m1 m2

        | MatrixTypes.MT_float32 ->
            let m1 = Matrices.getRandomFloat32Matrix matrixSize
            let m2 = Matrices.getRandomFloat32Matrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    mXmKernel <@(+)@> <@( * )@> <@0f@>
                    , Matrices.check (+) ( * ) 0f m1 m2
                | Semirings.MinPlus -> 
                    mXmKernel <@min@> <@(+)@> <@System.Single.MaxValue@>
                    , Matrices.check min (+) System.Single.PositiveInfinity m1 m2
                | x -> failwithf $"Unexpected semiring {x}."

            time mXm checker m1 m2

        | MatrixTypes.MT_float64 ->
            let m1 = Matrices.getRandomFloat64Matrix matrixSize
            let m2 = Matrices.getRandomFloat64Matrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    mXmKernel <@(+)@> <@( * )@> <@0.0@>
                    , Matrices.check (+) ( * ) 0.0 m1 m2
                | Semirings.MinPlus -> 
                    mXmKernel <@min@> <@(+)@> <@System.Double.MaxValue@>
                    , Matrices.check min (+) System.Double.PositiveInfinity m1 m2
                | x -> failwithf $"Unexpected semiring {x}."      

            time mXm checker m1 m2

        
        0
