module MatrixMultiplicationBenchmarks

open BenchmarkDotNet.Attributes
open Brahma.FSharp
open ImageProcessing.Matrices

module DeviceConfig =
    let mutable device : ClDevice option = None
    let getDevice () =
        match device with Some d -> d | None -> ClDevice.GetFirstAppropriateDevice()

type K0Benchmark() =
    let mutable benchmark : (unit -> unit) = Unchecked.defaultof<_>
    let mutable teardown : (unit -> unit) = Unchecked.defaultof<_>

    [<Params(256, 512, 1024, 2048)>]
    member val N = 0 with get, set

    [<Params(8, 16, 32, 64, 128, 256)>]
    member val LWS = 0 with get, set

    [<GlobalSetup>]
    member this.GlobalSetup() =
        if this.N % this.LWS <> 0 then
            failwithf "N=%d not divisible by LWS=%d" this.N this.LWS
        let dev = DeviceConfig.getDevice()
        let ctx = ClContext(dev)
        let q = ctx.QueueProvider.CreateQueue()
        let n = this.N
        let lws = uint this.LWS
        let kf = multiplyKernel0 ctx lws <@(+)@> <@( * )@> <@0f@>
        let rng = System.Random(42)
        let d1 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let d2 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let m1 = ctx.CreateClArray<float32>(d1, HostAccessMode.NotAccessible)
        let m2 = ctx.CreateClArray<float32>(d2, HostAccessMode.NotAccessible)
        let m3 = ctx.CreateClArray<float32>(n * n, HostAccessMode.NotAccessible, deviceAccessMode = DeviceAccessMode.WriteOnly)
        let sg = ctx.CreateClArray<float32>([|0f|], HostAccessMode.NotAccessible)
        let sh = Array.zeroCreate<float32>(1)

        benchmark <- fun () ->
            kf q m1 m2 m3 n |> ignore
            q.PostAndReply(fun ch -> Msg.CreateToHostMsg(sg, sh, ch)) |> ignore

        teardown <- fun () ->
            q.Post(Msg.CreateFreeMsg m1)
            q.Post(Msg.CreateFreeMsg m2)
            q.Post(Msg.CreateFreeMsg m3)
            q.Post(Msg.CreateFreeMsg sg)
            ()

    [<Benchmark>]
    member this.Run() = benchmark()

    [<GlobalCleanup>]
    member this.GlobalCleanup() = teardown()

type K1Benchmark() =
    let mutable benchmark : (unit -> unit) = Unchecked.defaultof<_>
    let mutable teardown : (unit -> unit) = Unchecked.defaultof<_>

    [<Params(256, 512, 1024, 2048)>]
    member val N = 0 with get, set

    [<Params(8, 16, 32, 64, 128, 256)>]
    member val LWS = 0 with get, set

    [<GlobalSetup>]
    member this.GlobalSetup() =
        if this.N % this.LWS <> 0 then
            failwithf "N=%d not divisible by LWS=%d" this.N this.LWS
        let dev = DeviceConfig.getDevice()
        let ctx = ClContext(dev)
        let q = ctx.QueueProvider.CreateQueue()
        let n = this.N
        let lws = uint this.LWS
        let kf = multiplyKernel1 ctx lws <@(+)@> <@( * )@> <@0f@>
        let rng = System.Random(42)
        let d1 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let d2 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let m1 = ctx.CreateClArray<float32>(d1, HostAccessMode.NotAccessible)
        let m2 = ctx.CreateClArray<float32>(d2, HostAccessMode.NotAccessible)
        let m3 = ctx.CreateClArray<float32>(n * n, HostAccessMode.NotAccessible, deviceAccessMode = DeviceAccessMode.WriteOnly)
        let sg = ctx.CreateClArray<float32>([|0f|], HostAccessMode.NotAccessible)
        let sh = Array.zeroCreate<float32>(1)

        benchmark <- fun () ->
            kf q m1 m2 m3 n |> ignore
            q.PostAndReply(fun ch -> Msg.CreateToHostMsg(sg, sh, ch)) |> ignore

        teardown <- fun () ->
            q.Post(Msg.CreateFreeMsg m1)
            q.Post(Msg.CreateFreeMsg m2)
            q.Post(Msg.CreateFreeMsg m3)
            q.Post(Msg.CreateFreeMsg sg)
            ()

    [<Benchmark>]
    member this.Run() = benchmark()

    [<GlobalCleanup>]
    member this.GlobalCleanup() = teardown()

type K2Benchmark() =
    let mutable benchmark : (unit -> unit) = Unchecked.defaultof<_>
    let mutable teardown : (unit -> unit) = Unchecked.defaultof<_>

    [<Params(256, 512, 1024, 2048)>]
    member val N = 0 with get, set

    [<Params(8, 16, 32, 64, 128, 256)>]
    member val LWS = 0 with get, set

    [<GlobalSetup>]
    member this.GlobalSetup() =
        if this.N % this.LWS <> 0 then
            failwithf "N=%d not divisible by LWS=%d" this.N this.LWS
        let dev = DeviceConfig.getDevice()
        let ctx = ClContext(dev)
        let q = ctx.QueueProvider.CreateQueue()
        let n = this.N
        let lws = uint this.LWS
        let kf = multiplyKernel2 ctx lws <@(+)@> <@( * )@> <@0f@>
        let rng = System.Random(42)
        let d1 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let d2 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let m1 = ctx.CreateClArray<float32>(d1, HostAccessMode.NotAccessible)
        let m2 = ctx.CreateClArray<float32>(d2, HostAccessMode.NotAccessible)
        let m3 = ctx.CreateClArray<float32>(n * n, HostAccessMode.NotAccessible, deviceAccessMode = DeviceAccessMode.WriteOnly)
        let sg = ctx.CreateClArray<float32>([|0f|], HostAccessMode.NotAccessible)
        let sh = Array.zeroCreate<float32>(1)

        benchmark <- fun () ->
            kf q m1 m2 m3 n |> ignore
            q.PostAndReply(fun ch -> Msg.CreateToHostMsg(sg, sh, ch)) |> ignore

        teardown <- fun () ->
            q.Post(Msg.CreateFreeMsg m1)
            q.Post(Msg.CreateFreeMsg m2)
            q.Post(Msg.CreateFreeMsg m3)
            q.Post(Msg.CreateFreeMsg sg)
            ()

    [<Benchmark>]
    member this.Run() = benchmark()

    [<GlobalCleanup>]
    member this.GlobalCleanup() = teardown()

type K3Benchmark() =
    let mutable benchmark : (unit -> unit) = Unchecked.defaultof<_>
    let mutable teardown : (unit -> unit) = Unchecked.defaultof<_>

    [<Params(256, 512, 1024, 2048)>]
    member val N = 0 with get, set

    [<Params(8, 16, 32, 64, 128, 256)>]
    member val LWS = 0 with get, set

    [<Params(1, 2, 4, 8)>]
    member val WPT = 0 with get, set

    [<GlobalSetup>]
    member this.GlobalSetup() =
        if this.N % this.LWS <> 0 then
            failwithf "N=%d not divisible by LWS=%d" this.N this.LWS
        if this.N % this.WPT <> 0 then
            failwithf "N=%d not divisible by WPT=%d" this.N this.WPT
        if this.LWS % this.WPT <> 0 then
            failwithf "LWS=%d not divisible by WPT=%d" this.LWS this.WPT
        let dev = DeviceConfig.getDevice()
        let ctx = ClContext(dev)
        let q = ctx.QueueProvider.CreateQueue()
        let n = this.N
        let lws = uint this.LWS
        let wpt = uint this.WPT
        let kf = multiplyKernel3 ctx lws wpt <@(+)@> <@( * )@> <@0f@>
        let rng = System.Random(42)
        let d1 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let d2 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let m1 = ctx.CreateClArray<float32>(d1, HostAccessMode.NotAccessible)
        let m2 = ctx.CreateClArray<float32>(d2, HostAccessMode.NotAccessible)
        let m3 = ctx.CreateClArray<float32>(n * n, HostAccessMode.NotAccessible, deviceAccessMode = DeviceAccessMode.WriteOnly)
        let sg = ctx.CreateClArray<float32>([|0f|], HostAccessMode.NotAccessible)
        let sh = Array.zeroCreate<float32>(1)

        benchmark <- fun () ->
            kf q m1 m2 m3 n |> ignore
            q.PostAndReply(fun ch -> Msg.CreateToHostMsg(sg, sh, ch)) |> ignore

        teardown <- fun () ->
            q.Post(Msg.CreateFreeMsg m1)
            q.Post(Msg.CreateFreeMsg m2)
            q.Post(Msg.CreateFreeMsg m3)
            q.Post(Msg.CreateFreeMsg sg)
            ()

    [<Benchmark>]
    member this.Run() = benchmark()

    [<GlobalCleanup>]
    member this.GlobalCleanup() = teardown()

type K4Benchmark() =
    let mutable benchmark : (unit -> unit) = Unchecked.defaultof<_>
    let mutable teardown : (unit -> unit) = Unchecked.defaultof<_>

    [<Params(256, 512, 1024, 2048)>]
    member val N = 0 with get, set

    [<Params(8, 16, 32, 64, 128, 256)>]
    member val LWS = 0 with get, set

    [<Params(1, 2, 4, 8)>]
    member val TTS = 0 with get, set

    [<GlobalSetup>]
    member this.GlobalSetup() =
        if this.N % this.TTS <> 0 then
            failwithf "N=%d not divisible by TTS=%d" this.N this.TTS
        if this.LWS % this.TTS <> 0 then
            failwithf "LWS=%d not divisible by TTS=%d" this.LWS this.TTS
        let dev = DeviceConfig.getDevice()
        let ctx = ClContext(dev)
        let q = ctx.QueueProvider.CreateQueue()
        let n = this.N
        let lws = uint this.LWS
        let tts = uint this.TTS
        let kf = multiplyKernel4 ctx lws tts <@(+)@> <@( * )@> <@0f@>
        let rng = System.Random(42)
        let d1 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let d2 = Array.init (n * n) (fun _ -> rng.NextSingle())
        let m1 = ctx.CreateClArray<float32>(d1, HostAccessMode.NotAccessible)
        let m2 = ctx.CreateClArray<float32>(d2, HostAccessMode.NotAccessible)
        let m3 = ctx.CreateClArray<float32>(n * n, HostAccessMode.NotAccessible, deviceAccessMode = DeviceAccessMode.WriteOnly)
        let sg = ctx.CreateClArray<float32>([|0f|], HostAccessMode.NotAccessible)
        let sh = Array.zeroCreate<float32>(1)

        benchmark <- fun () ->
            kf q m1 m2 m3 n |> ignore
            q.PostAndReply(fun ch -> Msg.CreateToHostMsg(sg, sh, ch)) |> ignore

        teardown <- fun () ->
            q.Post(Msg.CreateFreeMsg m1)
            q.Post(Msg.CreateFreeMsg m2)
            q.Post(Msg.CreateFreeMsg m3)
            q.Post(Msg.CreateFreeMsg sg)
            ()

    [<Benchmark>]
    member this.Run() = benchmark()

    [<GlobalCleanup>]
    member this.GlobalCleanup() = teardown()
