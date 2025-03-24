namespace ImageProcessing

open Argu
open Argu.ArguAttributes
open Brahma.FSharp
open FSharp.Quotations.Evaluator.QuotationEvaluationExtensions

type Platforms = CPU = 1 | CPUParallel = 2 | NVidia = 3 | IntelGPU = 4 | AnyGPU = 5
type MatrixTypes = MT_byte = 1 | MT_int = 2 | MT_float32 = 3 //| MT_OptInt = 4
type Semirings = MinPlus = 1 | Arithmetic = 2

[<CliPrefix(CliPrefix.DoubleDash)>]
[<NoAppSettings>]
type ImageProcessingArguments =    
    | Platform of Platforms
    | WorkGroupSize of uint
    | MatrixSize of uint
    | Kernel of Matrices.Kernels
    | Check of bool
    | MatrixType of MatrixTypes 
    | Semiring of Semirings

    with
    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Platform _ -> "Where to run."
            | WorkGroupSize _ -> "Work group size."
            | MatrixSize _ -> "Number of columns (or rows). We use square matrices."
            | Kernel _ -> "Kernel to run."
            | Check _ -> "Whether check result correctness."
            | MatrixType _ -> "Type of elements of matrices."
            | Semiring _ -> "Semiring to operate with matrices."
module Main =
    
    [<EntryPoint>]
    let main (argv: string array) =
        let parser = ArgumentParser.Create<ImageProcessingArguments>(programName = "ImageProcessing")
        let results = parser.ParseCommandLine argv
        let platform = results.GetResult(Platform, defaultValue = Platforms.CPU)
        let workGroupSize = results.GetResult(WorkGroupSize, defaultValue = 64u)
        let matrixSize = results.GetResult(MatrixSize, defaultValue = 512u)
        let kernel = results.GetResult(Kernel, defaultValue = Matrices.Kernels.K1)
        let check = results.GetResult(Check, defaultValue = false)
        let matrixType = results.GetResult(MatrixType, defaultValue = MatrixTypes.MT_int)
        let semiring = results.GetResult(Semiring, defaultValue = Semirings.Arithmetic)

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
        match matrixType with 
        | MatrixTypes.MT_byte ->
            let m1 = Matrices.getRandomByteMatrix matrixSize
            let m2 = Matrices.getRandomByteMatrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    Matrices.applyMultiplyGPU kernel context workGroupSize <@(+)@> <@( * )@> 0uy
                    , Matrices.check (+) ( * ) 0uy m1 m2
                | Semirings.MinPlus -> 
                    Matrices.applyMultiplyGPU kernel context workGroupSize <@min@> <@(+)@> 255uy
                    , Matrices.check min (+) 255uy m1 m2
                | x -> failwithf $"Unexpected semiring {x}."
            let start = System.DateTime.Now
            let res = mXm m1 m2

            printfn $"GPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"

            if check then checker res
            
        | MatrixTypes.MT_int -> 
            let m1 = Matrices.getRandomIntMatrix matrixSize
            let m2 = Matrices.getRandomIntMatrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    Matrices.applyMultiplyGPU kernel context workGroupSize <@(+)@> <@( * )@> 0
                    , Matrices.check (+) ( * ) 0 m1 m2
                | Semirings.MinPlus -> 
                    Matrices.applyMultiplyGPU kernel context workGroupSize <@min@> <@(+)@> System.Int32.MaxValue 
                    , Matrices.check min (+) System.Int32.MaxValue m1 m2
                | x -> failwithf $"Unexpected semiring {x}."

            let start = System.DateTime.Now
            let res = mXm m1 m2

            printfn $"GPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"

            if check then checker res

       (* | MatrixTypes.MT_OptInt -> 
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
                    Matrices.applyMultiplyGPU kernel context workGroupSize  opAdd opMult None
                    , Matrices.check (opAdd.Compile()) (opMult.Compile()) None m1 m2
                //| Semirings.MinPlus -> 
                //    Matrices.applyMultiplyGPU kernel context workGroupSize <@min@> <@(+)@> System.Int32.MaxValue 
                //    , Matrices.check min (+) System.Int32.MaxValue m1 m2
                | x -> failwithf $"Unexpected semiring {x}."

            let start = System.DateTime.Now
            let res = mXm m1 m2

            printfn $"GPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"

            if check then checker res
*)
        | MatrixTypes.MT_float32 ->
            let m1 = Matrices.getRandomFloat32Matrix matrixSize
            let m2 = Matrices.getRandomFloat32Matrix matrixSize
            let mXm, checker  = 
                match semiring with 
                | Semirings.Arithmetic -> 
                    Matrices.applyMultiplyGPU kernel context workGroupSize <@(+)@> <@( * )@> 0f
                    , Matrices.check (+) ( * ) 0f m1 m2
                | Semirings.MinPlus -> 
                    Matrices.applyMultiplyGPU kernel context workGroupSize <@min@> <@(+)@> System.Single.MaxValue
                    , Matrices.check min (+) System.Single.PositiveInfinity m1 m2
                | x -> failwithf $"Unexpected semiring {x}."

            let start = System.DateTime.Now
            let res = mXm m1 m2

            printfn $"GPU processing time: {(System.DateTime.Now - start).TotalMilliseconds} ms"

            if check then checker res

        0
