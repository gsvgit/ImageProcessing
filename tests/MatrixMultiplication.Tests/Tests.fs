module MatrixMultiplicationTests

open Xunit
open Brahma.FSharp
open ImageProcessing.Matrices

let context =
    lazy
        let device = ClDevice.GetFirstAppropriateDevice()
        ClContext(device)

let n = 64u
let localWorkSize = 8u
let workPerThread = 2u

let private testInt (kernel: Kernels) =
    let m1 = getRandomIntMatrix n
    let m2 = getRandomIntMatrix n
    let cpuResult, _ = cpuMxM (+) ( * ) 0 m1 m2
    let gpuMul = applyMultiplyGPU kernel context.Value 1u localWorkSize workPerThread <@(+)@> <@( * )@> <@0@>
    let gpuResult, _ = gpuMul m1 m2
    Assert.Equal<System.Collections.Generic.IEnumerable<int>>(cpuResult, gpuResult)

let private testFloat32 (kernel: Kernels) =
    let m1 = getRandomFloat32Matrix n
    let m2 = getRandomFloat32Matrix n
    let cpuResult, _ = cpuMxM (+) ( * ) 0f m1 m2
    let gpuMul = applyMultiplyGPU kernel context.Value 1u localWorkSize workPerThread <@(+)@> <@( * )@> <@0f@>
    let gpuResult, _ = gpuMul m1 m2
    for i in 0 .. cpuResult.Length - 1 do
        let diff = abs (cpuResult.[i] - gpuResult.[i])
        Assert.True(diff < 1e-5f || diff / (abs cpuResult.[i]) < 1e-5f)

let private testByte (kernel: Kernels) =
    let m1 = getRandomByteMatrix n
    let m2 = getRandomByteMatrix n
    let cpuResult, _ = cpuMxM (+) ( * ) 0uy m1 m2
    let gpuMul = applyMultiplyGPU kernel context.Value 1u localWorkSize workPerThread <@(+)@> <@( * )@> <@0uy@>
    let gpuResult, _ = gpuMul m1 m2
    Assert.Equal<System.Collections.Generic.IEnumerable<byte>>(cpuResult, gpuResult)

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K0 multiplies int matrices`` () = testInt Kernels.K0

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K1 multiplies int matrices`` () = testInt Kernels.K1

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K2 multiplies int matrices`` () = testInt Kernels.K2

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K3 multiplies int matrices`` () = testInt Kernels.K3

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K4 multiplies int matrices`` () = testInt Kernels.K4

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K0 multiplies float32 matrices`` () = testFloat32 Kernels.K0

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K1 multiplies float32 matrices`` () = testFloat32 Kernels.K1

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K2 multiplies float32 matrices`` () = testFloat32 Kernels.K2

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K3 multiplies float32 matrices`` () = testFloat32 Kernels.K3

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K4 multiplies float32 matrices`` () = testFloat32 Kernels.K4

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K0 multiplies byte matrices`` () = testByte Kernels.K0

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K1 multiplies byte matrices`` () = testByte Kernels.K1

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K2 multiplies byte matrices`` () = testByte Kernels.K2

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K3 multiplies byte matrices`` () = testByte Kernels.K3

[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``K4 multiplies byte matrices`` () = testByte Kernels.K4
