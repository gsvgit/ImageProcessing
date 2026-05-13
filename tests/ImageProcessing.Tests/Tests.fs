module Tests

open System
open Xunit
open Brahma.FSharp
open ImageProcessing.ImageProcessing

let rng = Random 42

let randomImage h w =
    Array2D.init h w (fun _ _ -> byte (rng.Next 256))

let randomFilter size =
    Array.init size (fun _ ->
        Array.init size (fun _ ->
            float32 (rng.NextDouble() * 2.0 - 1.0)))

let imgToFlat (img: byte[,]) =
    let h = img.GetLength 0
    let w = img.GetLength 1
    Array.init (h * w) (fun i -> img.[i / w, i % w])

let flatToImg (data: byte[]) h w =
    Array2D.init h w (fun i j -> data.[i * w + j])

let imagesEqual (a: byte[,]) (b: byte[,]) =
    let ha = a.GetLength 0
    let wa = a.GetLength 1
    let hb = b.GetLength 0
    let wb = b.GetLength 1
    ha = hb
    && wa = wb
    && seq {
        for i in 0 .. ha - 1 do
            for j in 0 .. wa - 1 do
                yield a.[i, j] = b.[i, j]
    }
    |> Seq.forall id

let allZero (img: byte[,]) =
    seq {
        for i in 0 .. img.GetLength 0 - 1 do
            for j in 0 .. img.GetLength 1 - 1 do
                yield img.[i, j]
    }
    |> Seq.forall ((=) 0uy)

let identityFilter = [| [| 1.0f |] |]

let zeroFilter = [| [| 0.0f |] |]

let shiftRightFilter =
    [|
        [| 0.0f; 1.0f; 0.0f |]
        [| 0.0f; 0.0f; 0.0f |]
        [| 0.0f; 0.0f; 0.0f |]
    |]

let shiftDownFilter =
    [|
        [| 0.0f; 0.0f; 0.0f |]
        [| 1.0f; 0.0f; 0.0f |]
        [| 0.0f; 0.0f; 0.0f |]
    |]

// ========== CPU Tests ==========

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Identity filter preserves input`` () =
    let img = randomImage 16 16
    let result = applyFilter identityFilter img
    Assert.True(imagesEqual img result)

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Zero filter produces black image`` () =
    let img = randomImage 16 16
    let result = applyFilter zeroFilter img
    Assert.True(allZero result)

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Shift right filter shifts image right by one pixel`` () =
    let img = randomImage 8 8
    let result = applyFilter shiftRightFilter img

    for x in 0 .. 7 do
        for y in 0 .. 7 do
            if x > 0 then
                Assert.Equal(img.[x - 1, y], result.[x, y])
            else
                Assert.Equal(img.[x, y], result.[x, y])

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Shift down filter shifts image down by one pixel`` () =
    let img = randomImage 8 8
    let result = applyFilter shiftDownFilter img

    for x in 0 .. 7 do
        for y in 0 .. 7 do
            if y > 0 then
                Assert.Equal(img.[x, y - 1], result.[x, y])
            else
                Assert.Equal(img.[x, y], result.[x, y])

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``CPU sequential and CPU parallel produce same result`` () =
    let img = randomImage 32 32
    let filter = randomFilter 5
    let resultSeq = applyFilter filter img
    let resultPar = applyFilterCpuParallel filter img
    Assert.True(imagesEqual resultSeq resultPar)

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Gaussian blur preserves constant image`` () =
    let img = Array2D.create 16 16 128uy
    let result = applyFilter gaussianBlurKernel img
    Assert.True(
        seq {
            for i in 0 .. result.GetLength 0 - 1 do
                for j in 0 .. result.GetLength 1 - 1 do
                    yield result.[i, j]
        }
        |> Seq.forall ((=) 128uy)
    )

// ========== GPU Tests ==========

let context =
    lazy
        let device = ClDevice.GetFirstAppropriateDevice()
        ClContext(device)

let localWorkSize = 64

let applyFilterGPU (filter: float32[][]) (img2d: byte[,]) =
    let h = img2d.GetLength 0
    let w = img2d.GetLength 1
    let flat = imgToFlat img2d
    let image = Image(flat, w, h, "test")
    let gpuApplier = applyFiltersGPU context.Value localWorkSize
    let result = gpuApplier [ filter ] image
    flatToImg result.Data h w

let gpuCpuMatchWithFilter (filter: float32[][]) =
    let img = randomImage 16 16
    let cpuResult = applyFilter filter img
    let gpuResult = applyFilterGPU filter img
    Assert.True(imagesEqual cpuResult gpuResult)

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU matches CPU with identity filter`` () =
    gpuCpuMatchWithFilter identityFilter

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU matches CPU with Gaussian blur filter`` () =
    gpuCpuMatchWithFilter gaussianBlurKernel

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU matches CPU with edge detection filter`` () =
    gpuCpuMatchWithFilter edgesKernel

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU matches CPU with random 5x5 filter`` () =
    let filter = randomFilter 5
    gpuCpuMatchWithFilter filter

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU matches CPU with shift right filter`` () =
    gpuCpuMatchWithFilter shiftRightFilter

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU matches CPU with multiple chained filters`` () =
    let img = randomImage 16 16
    let filter1 = randomFilter 3
    let filter2 = randomFilter 5

    let cpuResult = img |> applyFilter filter1 |> applyFilter filter2

    let h = img.GetLength 0
    let w = img.GetLength 1
    let flat = imgToFlat img
    let image = Image(flat, w, h, "test")
    let gpuApplier = applyFiltersGPU context.Value localWorkSize
    let gpuImage = gpuApplier [ filter1; filter2 ] image
    let gpuResult = flatToImg gpuImage.Data h w

    Assert.True(imagesEqual cpuResult gpuResult)

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU identity filter preserves input`` () =
    let img = randomImage 16 16
    let gpuResult = applyFilterGPU identityFilter img
    Assert.True(imagesEqual img gpuResult)
