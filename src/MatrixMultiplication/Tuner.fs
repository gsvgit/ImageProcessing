module ImageProcessing.Tuner

open ImageProcessing.Matrices
open Brahma.FSharp

let tune (kernel:Kernels) (clContext: ClContext) (numToRun:uint) (opAdd:Quotations.Expr<'a -> 'b -> 'a>) (opMult:Quotations.Expr<'e -> 'f -> 'b>) (zero:Quotations.Expr<'a>) m1 m2 = 
    let mutable localWorkSize = 2u
    let mutable workPerThread = 2u
    let mutable cruBestTime = System.Double.MaxValue
    let powOf2 = [for i in 1..8 -> pown 2 i |> uint]
    for lws in powOf2 do
       for wpt in powOf2 do
          try 
            let _,time = applyMultiplyGPU kernel clContext numToRun lws wpt opAdd opMult zero m1 m2
            printfn $"local work size: {lws}; work per thread: {wpt} --- {time} ms."
            if time < cruBestTime
            then
               cruBestTime <- time
               localWorkSize <- lws
               workPerThread <- wpt
          with 
          | e -> printfn $"local work size: {lws}; work per thread: {wpt} --- bad configuration."

    printfn $"Best configuration: local work size: {localWorkSize}; work per thread: {workPerThread}."
    [||],0.0