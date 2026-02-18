module Tests

open System
open Xunit


[<Fact>]
[<Trait("Category", "CPU_Tests")>]
let ``Dummy fact for CPU`` () =
    Assert.True(true)


[<Fact>]
[<Trait("Category", "GPU_Tests")>]
let ``Dummy fact for GPU`` () =
    Assert.True(true)