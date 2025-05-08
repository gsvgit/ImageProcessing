module ImageProcessing.Matrices

open Brahma.FSharp

type Kernels = K0 = 0 | K1 = 1 | K2 = 2 | K3 = 3 | K4 = 4
let rand = new System.Random()

let getRandomMatrix (n: uint) init = 
    Array.Parallel.init (int n) (fun i -> Array.init (int n) init) 

let cpuMxM opAdd opMult zero (m1 : array<array<_>>) (m2: array<array<_>>) =
    let res = Array.init (m1.Length * m1.Length) (fun _ -> zero)
    for i in 0..m1.Length - 1 do
      for j in 0..m1.Length - 1 do
        for k in 0..m1.Length - 1 do
            res.[i*m1.Length + j] <- opAdd res.[i * m1.Length + j]  (opMult m1.[i].[k] m2.[k].[j])
    res, 0.0

let cpuParallelMxM opAdd opMult zero (m1 : array<array<_>>) (m2: array<array<_>>) =
    let res = Array.init (m1.Length * m1.Length) (fun _ -> zero)
    m1 
    |> Array.Parallel.iteri (fun i row -> 
        for j in 0..m1.Length - 1 do
          for k in 0..m1.Length - 1 do
            res.[i*m1.Length + j] <- opAdd res.[i * m1.Length + j]  (opMult row.[k] m2.[k].[j])
       )
    res, 0.0

let check opAdd opMult zero (m1 : array<array<_>>) (m2: array<array<_>>) (m3:array<_>) =
    let res,_ = cpuMxM opAdd opMult zero (m1 : array<array<_>>) (m2: array<array<_>>)
    Array.iteri2 (fun i r1 r2 -> if r1 <> r2 then printfn $"Expected {r1}, got {r2}") res m3


let getRandomIntMatrix n = getRandomMatrix n (fun i -> rand.Next(-10,10))
let getRandomByteMatrix n = getRandomMatrix n (fun i -> rand.Next() |> byte)
let getRandomFloat32Matrix n = getRandomMatrix n (fun i -> rand.NextSingle())
let getRandomFloat64Matrix n = getRandomMatrix n (fun i -> rand.NextDouble())
let getRandomOptionIntMatrix n = getRandomMatrix n (fun i -> let x = rand.Next(-10,10) in if x % 3 = 0 then Some x else None)

let multiplyKernel4 (clContext: ClContext) (localWorkSize:uint) (threadTileSize:uint) opAdd opMult zero =
    let localWorkSize = int localWorkSize
    let threadTileSize = int threadTileSize
    let localBufSize = FSharp.Quotations.Evaluator.QuotationEvaluator.Evaluate <@localWorkSize * localWorkSize@>
    let threadLocalBufSize = FSharp.Quotations.Evaluator.QuotationEvaluator.Evaluate <@threadTileSize * threadTileSize@>
    let kernel =
        <@
            fun (r: Range2D) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n ->
                let localBaseRow = r.LocalID0 * threadTileSize
                let localBaseCol = r.LocalID1 * threadTileSize
                let globalBaseRow = r.GlobalID0 * threadTileSize
                let globalBaseCol = r.GlobalID1 * threadTileSize

                let m1Submatrix = localArray localBufSize
                let m2Submatrix = localArray localBufSize

                let m2Buf = threadLocalArray threadTileSize

                let res = threadLocalArray threadLocalBufSize

                for i in 0 .. threadLocalBufSize - 1 do res.[i] <- %zero

                for t in 0 ..  (n / localWorkSize) - 1 do
                   for i in 0 .. threadTileSize - 1 do
                      for j in 0 .. threadTileSize - 1 do
                        let tiledRow = localWorkSize * t + localBaseRow + i
                        let tiledCol = localWorkSize * t + localBaseCol + j
                        let targetElem =  (localBaseRow + i) * localWorkSize + localBaseCol + j
                        m1Submatrix[targetElem] <- m1[(globalBaseRow + i) * n + tiledCol]
                        m2Submatrix[targetElem] <- m2[tiledRow * n + globalBaseCol + j]

                   barrierLocal()

                   for k in 0 .. localWorkSize - 1 do
                       
                       for i in 0 .. threadTileSize - 1 do
                          m2Buf[i] <- m2Submatrix[k * localWorkSize + localBaseCol + i]

                       for i in 0 .. threadTileSize - 1 do
                          let m1Val = m1Submatrix[(localBaseRow + i) * localWorkSize + k]
                          for j in 0 .. threadTileSize - 1 do
                             let x = (%opMult) m1Val m2Buf[j]
                             let y = (%opAdd) res[i * threadTileSize + j] x 
                             res[i * threadTileSize + j] <- y
                   barrierLocal()

                for i in 0 .. threadTileSize - 1 do  
                    for j in 0 .. threadTileSize - 1 do  
                        m3.[(globalBaseRow + i) * n + globalBaseCol + j] <- res[i * threadTileSize + j]
        @>

    let kernel = clContext.Compile kernel

    fun (commandQueue: MailboxProcessor<_>) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n  ->

        let ndRange =
            Range2D(
                n / threadTileSize,
                n / threadTileSize,
                localWorkSize / threadTileSize,
                localWorkSize / threadTileSize
            )

        let kernel = kernel.GetKernel()
        commandQueue.Post(Msg.MsgSetArguments(fun () -> kernel.KernelFunc ndRange m1 m2 m3 n))
        commandQueue.Post(Msg.CreateRunMsg<_, _> kernel)
        m3

let multiplyKernel3 (clContext: ClContext) (localWorkSize:uint) (workPerThread:uint) opAdd opMult zero =
    let localWorkSize = int localWorkSize
    let workPerThread = int workPerThread
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
                let res = threadLocalArray workPerThread
                for i in 0 .. workPerThread - 1 do res.[i] <- %zero
                
                for t in 0 ..  (n / localWorkSize) - 1 do
                   let tiledRow = localWorkSize * t + localRow
                   for w in 0 .. workPerThread - 1 do
                      let tiledCol = localWorkSize * t + (localCol * workPerThread) + w
                      m1Submatrix[localRow * localWorkSize + (localCol * workPerThread) + w] <- m1[globalRow * n + tiledCol]
                      m2Submatrix[localRow * localWorkSize + (localCol * workPerThread) + w] <- m2[tiledRow * n + (globalCol* workPerThread) + w]

                   barrierLocal()

                   for k in 0 .. localWorkSize - 1 do
                       for w in 0 .. workPerThread - 1 do
                          let x = (%opMult) m1Submatrix.[localRow * localWorkSize + k] m2Submatrix.[localWorkSize * k + (localCol * workPerThread) + w]
                          let y = (%opAdd) res[w] x 
                          res[w] <- y
                   barrierLocal()

                for w in 0 .. workPerThread - 1 do  m3.[globalRow * n + (globalCol * workPerThread) + w] <- res[w]
        @>

    let kernel = clContext.Compile kernel

    fun (commandQueue: MailboxProcessor<_>) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n  ->

        let ndRange =
            Range2D(
                n,
                n / workPerThread,
                localWorkSize,
                localWorkSize / workPerThread
            )

        let kernel = kernel.GetKernel()
        commandQueue.Post(Msg.MsgSetArguments(fun () -> kernel.KernelFunc ndRange m1 m2 m3 n))
        commandQueue.Post(Msg.CreateRunMsg<_, _> kernel)
        m3


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
                let mutable res = %zero
                
                for t in 0 ..  (n / localWorkSize) - 1 do
                   let tiledRow = localWorkSize * t + localRow
                   let tiledCol = localWorkSize * t + localCol
                   m1Submatrix[localRow * localWorkSize + localCol] <- m1[globalRow * n + tiledCol]
                   m2Submatrix[localRow * localWorkSize + localCol] <- m2[tiledRow * n + globalCol]

                   barrierLocal()

                   for k in 0 .. localWorkSize - 1 do
                       let x = (%opMult) m1Submatrix.[localRow * localWorkSize + k] m2Submatrix.[localWorkSize * k + localCol]
                       let y = (%opAdd) res x 
                       res <- y
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
            fun (r: Range2D) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n  ->
                let i = r.GlobalID0
                let j = r.GlobalID1

                let mutable res = %zero
                for k in 0 .. n - 1 do
                    let x = ((%opMult) m1.[i * n + k] m2.[n * k + j])
                    let y = (%opAdd) res x
                    res <- y
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

let multiplyKernel0 (clContext: ClContext) (localWorkSize: uint) opAdd opMult zero =
    let kernel =
        <@
            fun (r: Range2D) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n  ->
                let i = r.GlobalID0
                let j = r.GlobalID1

                m3.[i * n + j] <- %zero
                for k in 0 .. n - 1 do
                    m3.[i * n + j] <- (%opAdd) m3.[i * n + j] ((%opMult) m1.[i * n + k] m2.[n * k + j])
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

let applyMultiplyGPU<'a,'b,'e,'f> (kernel:Kernels) (clContext: ClContext) (numToRun:uint) localWorkSize workPerThread (opAdd:Quotations.Expr<'a -> 'b -> 'a>) (opMult:Quotations.Expr<'e -> 'f -> 'b>) (zero:Quotations.Expr<'a>) =    
    let kernel = 
        match kernel with 
        | Kernels.K0 -> multiplyKernel0 clContext localWorkSize opAdd opMult zero
        | Kernels.K1 -> multiplyKernel1 clContext localWorkSize opAdd opMult zero
        | Kernels.K2 -> multiplyKernel2 clContext localWorkSize opAdd opMult zero
        | Kernels.K3 -> multiplyKernel3 clContext localWorkSize workPerThread opAdd opMult zero
        | Kernels.K4 -> multiplyKernel4 clContext localWorkSize workPerThread opAdd opMult zero
        | x -> failwithf $"Unexpected kernel {x}."
    
    let queue = clContext.QueueProvider.CreateQueue()
    
    let numToRun = int numToRun

    fun (m1: 'e[][]) (m2: 'f[][]) ->
        let result : 'a[] = Array.zeroCreate(m1.Length * m1.Length)
        let start = System.DateTime.Now
        for i in 0 .. numToRun - 1 do
            let m1_gpu =
                clContext.CreateClArray<_>(Array.concat m1, HostAccessMode.NotAccessible)
            
            let m2_gpu =
                clContext.CreateClArray<_>(Array.concat m2, HostAccessMode.NotAccessible)
            
            let m3_gpu =
                clContext.CreateClArray(
                    m1.Length * m1.Length,
                    HostAccessMode.NotAccessible,
                    deviceAccessMode=DeviceAccessMode.WriteOnly,
                    allocationMode = AllocationMode.Default
                )
            
            let x = kernel queue m1_gpu m2_gpu m3_gpu m1.Length
            
            let result = queue.PostAndReply(fun ch -> Msg.CreateToHostMsg(m3_gpu, result, ch))
            
            queue.Post(Msg.CreateFreeMsg m1_gpu)
            
            queue.Post(Msg.CreateFreeMsg m2_gpu)
            
            queue.Post(Msg.CreateFreeMsg m3_gpu)

        let totalTime = (System.DateTime.Now - start).TotalMilliseconds
        
        result, (totalTime / (float numToRun))
        