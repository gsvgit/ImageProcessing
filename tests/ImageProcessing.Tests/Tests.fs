module Tests

open System
open Xunit
open Brahma.FSharp
open ImageProcessing.ImageProcessing

let rng = Random 42

let randomImage h w =
    let data = Array.init (h * w) (fun _ -> byte (rng.Next 256))
    Image(data, w, h, "test")

let randomFilter size =
    Array.init size (fun _ ->
        Array.init size (fun _ ->
            float32 (rng.NextDouble() * 2.0 - 1.0)))

let imagesEqual (a: Image) (b: Image) =
    a.Width = b.Width
    && a.Height = b.Height
    && a.Data = b.Data

let allZero (img: Image) =
    img.Data |> Seq.forall ((=) 0uy)

let identityFilter = [| [| 1.0f |] |]

let zeroFilter = [| [| 0.0f |] |]

let shiftRightFilter =
    [|
        [| 0.0f; 0.0f; 0.0f |]
        [| 1.0f; 0.0f; 0.0f |]
        [| 0.0f; 0.0f; 0.0f |]
    |]

let shiftDownFilter =
    [|
        [| 0.0f; 1.0f; 0.0f |]
        [| 0.0f; 0.0f; 0.0f |]
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
    let w, h = 8, 8
    let img = randomImage h w
    let result = applyFilter shiftRightFilter img

    for y in 0 .. h - 1 do
        for x in 0 .. w - 1 do
            if x > 0 then
                Assert.Equal(img.Data.[y * w + (x - 1)], result.Data.[y * w + x])
            else
                Assert.Equal(img.Data.[y * w + x], result.Data.[y * w + x])

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Shift down filter shifts image down by one pixel`` () =
    let w, h = 8, 8
    let img = randomImage h w
    let result = applyFilter shiftDownFilter img

    for y in 0 .. h - 1 do
        for x in 0 .. w - 1 do
            if y > 0 then
                Assert.Equal(img.Data.[(y - 1) * w + x], result.Data.[y * w + x])
            else
                Assert.Equal(img.Data.[y * w + x], result.Data.[y * w + x])

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
let ``CPU sequential and CPU parallel rows produce same result`` () =
    let img = randomImage 32 32
    let filter = randomFilter 5
    let resultSeq = applyFilter filter img
    let resultRows = applyFilterCpuParallelRows filter img
    Assert.True(imagesEqual resultSeq resultRows)

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``CPU parallel (pixel) and CPU parallel (rows) produce same result`` () =
    let img = randomImage 32 32
    let filter = randomFilter 5
    let resultPix = applyFilterCpuParallel filter img
    let resultRows = applyFilterCpuParallelRows filter img
    Assert.True(imagesEqual resultPix resultRows)

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Shift right filter with CPU parallel rows shifts image right`` () =
    let w, h = 8, 8
    let img = randomImage h w
    let result = applyFilterCpuParallelRows shiftRightFilter img

    for y in 0 .. h - 1 do
        for x in 0 .. w - 1 do
            if x > 0 then
                Assert.Equal(img.Data.[y * w + (x - 1)], result.Data.[y * w + x])
            else
                Assert.Equal(img.Data.[y * w + x], result.Data.[y * w + x])

[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Gaussian blur preserves constant image`` () =
    let data = Array.create (16 * 16) 128uy
    let img = Image(data, 16, 16, "test")
    let result = applyFilter gaussianBlurKernel img
    Assert.True(
        result.Data |> Seq.forall ((=) 128uy)
    )

// ========== GPU Tests ==========

let context =
    lazy
        let device = ClDevice.GetFirstAppropriateDevice()
        ClContext(device)

let localWorkSize = 64

let applyFilterGPU (filter: float32[][]) (img: Image) =
    let gpuApplier = applyFiltersGPU context.Value localWorkSize
    gpuApplier [ filter ] img

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

    let gpuApplier = applyFiltersGPU context.Value localWorkSize
    let gpuResult = gpuApplier [ filter1; filter2 ] img

    Assert.True(imagesEqual cpuResult gpuResult)

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``GPU identity filter preserves input`` () =
    let img = randomImage 16 16
    let gpuResult = applyFilterGPU identityFilter img
    Assert.True(imagesEqual img gpuResult)
