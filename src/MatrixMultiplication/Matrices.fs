module ImageProcessing.Matrices

open Brahma.FSharp

let rand = new System.Random()

let getRandomMatrix (n: uint) init = 
    
    [|
        for i in 0 .. int n - 1 -> Array.init (int n) init
    |]

let check opAdd opMult zero (m1 : array<array<_>>) (m2: array<array<_>>) (m3:array<_>) =
    let res = Array.init (m1.Length * m1.Length) (fun _ -> zero)
    for i in 0..m1.Length - 1 do
      for j in 0..m1.Length - 1 do
        for k in 0..m1.Length - 1 do
            res.[i*m1.Length + j] <- opAdd res.[i * m1.Length + j]  (opMult m1.[i].[k] m2.[k].[j])

    Array.iteri2 (fun i r1 r2 -> if r1 <> r2 then printfn $"Expected {r1}, got {r2}") res m3


let getRandomIntMatrix n= getRandomMatrix n (fun i -> rand.Next(-10,10))
let getRandomFloatMatrix n= getRandomMatrix n (fun i -> rand.NextDouble())

let multiplyKernel2 (clContext: ClContext) (localWorkSize:uint) opAdd opMult zero =
    let localWorkSize = int localWorkSize
    let size = FSharp.Quotations.Evaluator.QuotationEvaluator.Evaluate <@localWorkSize * localWorkSize@>
    let kernel =
        <@
            fun (r: Range2D) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n ->
                let localRow = r.LocalID0
                let localCol = r.LocalID1
                let globalRow = r.GlobalID0
                let globalCol = r.GlobalID1

                let m1Submatrix = localArray size
                let m2Submatrix = localArray size
                let mutable res = zero
                
                for t in 0 ..  (n / localWorkSize) - 1 do
                   let tiledRow = localWorkSize * t + localRow
                   let tiledCol = localWorkSize * t + localCol
                   m1Submatrix[localRow * localWorkSize + localCol] <- m1[globalRow * n + tiledCol]
                   m2Submatrix[localRow * localWorkSize + localCol] <- m2[tiledRow * n + globalCol]

                   barrierLocal()

                   for k in 0 .. localWorkSize - 1 do
                       res <- (%opAdd) res ((%opMult) m1Submatrix.[localRow * localWorkSize + k] m2Submatrix.[localWorkSize * k + localCol])                       
                   barrierLocal()

                m3.[globalRow * n + globalCol] <- res
        @>

    let kernel = clContext.Compile kernel

    fun (commandQueue: MailboxProcessor<_>) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n  ->

        let ndRange =
            Range2D(
                n,
                n,
                localWorkSize,
                localWorkSize
            )

        let kernel = kernel.GetKernel()
        commandQueue.Post(Msg.MsgSetArguments(fun () -> kernel.KernelFunc ndRange m1 m2 m3 n))
        commandQueue.Post(Msg.CreateRunMsg<_, _> kernel)
        m3

let multiplyKernel1 (clContext: ClContext) (localWorkSize: uint) opAdd opMult zero =
    let kernel =
        <@
            fun (r: Range2D) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n ->
                let i = r.GlobalID0
                let j = r.GlobalID1

                let mutable res = zero
                for k in 0 .. n - 1 do
                    res <- (%opAdd) res ((%opMult) m1.[i * n + k] m2.[n * k + j])
                m3.[i * n + j] <- res
        @>

    let kernel = clContext.Compile kernel
    let localWorkSize = int localWorkSize
    fun (commandQueue: MailboxProcessor<_>) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n  ->

        let ndRange =
            Range2D(
                n,
                n,
                localWorkSize,
                localWorkSize
            )

        let kernel = kernel.GetKernel()
        commandQueue.Post(Msg.MsgSetArguments(fun () -> kernel.KernelFunc ndRange m1 m2 m3 n))
        commandQueue.Post(Msg.CreateRunMsg<_, _> kernel)
        m3

let applyMultiplyGPU<'a,'b,'e,'f> (clContext: ClContext) localWorkSize (opAdd:Quotations.Expr<'a -> 'b -> 'a>) (opMult:Quotations.Expr<'e -> 'f -> 'b>) (zero:'a) =
    //let kernel = multiplyKernel1 clContext localWorkSize opAdd opMult zero
    let kernel = multiplyKernel2 clContext localWorkSize opAdd opMult zero
    let queue = clContext.QueueProvider.CreateQueue()

    fun (m1: 'e[][]) (m2: 'f[][]) ->
        printfn "!!!1!!!"

        let m1_gpu =
            clContext.CreateClArray<_>(Array.concat m1, HostAccessMode.NotAccessible)
        printfn "!!!2!!!"
        let m2_gpu =
            clContext.CreateClArray<_>(Array.concat m2, HostAccessMode.NotAccessible)
        printfn "!!!3!!!"

        let m3_gpu =
            clContext.CreateClArray(
                m1.Length * m1.Length,
                HostAccessMode.NotAccessible,
                allocationMode = AllocationMode.Default
            )
        printfn "!!!4!!!"
        let x = kernel queue m1_gpu m2_gpu m3_gpu m1.Length
        printfn "!!!5!!!"
        let result : 'a[] = Array.zeroCreate(m1.Length * m1.Length)
        printfn "!!!6!!!"
        let result = queue.PostAndReply(fun ch -> Msg.CreateToHostMsg(m3_gpu, result, ch))
        printfn "!!!7!!!"
        queue.Post(Msg.CreateFreeMsg m1_gpu)
        printfn "!!!8!!!"
        queue.Post(Msg.CreateFreeMsg m2_gpu)
        printfn "!!!9!!!"
        queue.Post(Msg.CreateFreeMsg m3_gpu)
        printfn "!!!10!!!"
        result