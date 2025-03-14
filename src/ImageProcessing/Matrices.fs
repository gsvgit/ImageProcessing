module ImageProcessing.Matrices

open Brahma.FSharp

let rand = new System.Random()

let getRandomMatrix n init = 
    
    [|
        for i in 0 .. n - 1 -> Array.init n init
    |]

let getRandomIntMatrix n= getRandomMatrix n (fun i -> rand.Next())
let getRandomFloatMatrix n= getRandomMatrix n (fun i -> rand.NextDouble())

let multiplyKernel2 (clContext: ClContext) localWorkSize opAdd opMult zero =
    let kernel =
        <@
            fun (r: Range2D) (m1: ClArray<_>) (m2: ClArray<_>) (m3: ClArray<_>) n ->
                let row = r.LocalID0
                let col = r.LocalID1
                let globalRow = localWorkSize * r.GlobalID0 + row
                let globalCol = localWorkSize * r.GlobalID1 + col

                let m1Submatrix = localArray (localWorkSize * localWorkSize)
                let m2Submatrix = localArray (localWorkSize * localWorkSize)
                let mutable res = zero
                

                for t in 0 ..  (n / localWorkSize) - 1 do
                   let tiledRow = localWorkSize*t + row
                   let tiledCol = localWorkSize*t + col
                   m1Submatrix[row * localWorkSize + col] <- m1[tiledCol*n + globalRow]
                   m2Submatrix[row * localWorkSize + col] <- m2[globalCol*n + tiledRow]

                   barrierLocal()

                   for k in 0 .. localWorkSize - 1 do
                       res <- (%opAdd) res ((%opMult) m1Submatrix.[row * localWorkSize + k] m2Submatrix.[localWorkSize * k + col])
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

let multiplyKernel1 (clContext: ClContext) localWorkSize opAdd opMult zero =
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
    let kernel = multiplyKernel1 clContext localWorkSize opAdd opMult zero
    let queue = clContext.QueueProvider.CreateQueue()

    fun (m1: 'e[][]) (m2: 'f[][]) ->

        let m1_gpu =
            clContext.CreateClArray<_>(Array.concat m1, HostAccessMode.NotAccessible)

        let m2_gpu =
            clContext.CreateClArray<_>(Array.concat m2, HostAccessMode.NotAccessible)


        let m3_gpu =
            clContext.CreateClArray(
                m1.Length * m1.Length,
                HostAccessMode.NotAccessible,
                allocationMode = AllocationMode.Default
            )
        let _ = kernel queue m1_gpu m2_gpu m3_gpu m1.Length
        let result : 'a[] =
            Array.zeroCreate(m1.Length * m1.Length)

        let result = queue.PostAndReply(fun ch -> Msg.CreateToHostMsg(m3_gpu, result, ch))
        queue.Post(Msg.CreateFreeMsg m1_gpu)
        queue.Post(Msg.CreateFreeMsg m2_gpu)
        queue.Post(Msg.CreateFreeMsg m3_gpu)
        result