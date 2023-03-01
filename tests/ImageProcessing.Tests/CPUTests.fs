namespace CPUTests

open Expecto
open CPUImageProcessing

module RotationTests =
    [<Tests>]
    let tests =
        testList
            "RotationTests"
            [ testCase "360 degree image clockwise rotation is equal to the original"
              <| fun _ ->
                  let actualResultPath =
                      __SOURCE_DIRECTORY__ + "/Images/input/yaroslav-beregovyi-g--c9rWAiCY-unsplash.jpg"

                  let expectedResult = loadAs2DArray actualResultPath

                  let actualResult =
                      expectedResult
                      |> rotate2DArray true
                      |> rotate2DArray true
                      |> rotate2DArray true
                      |> rotate2DArray true

                  Expect.equal actualResult expectedResult $"Unexpected: %A{actualResult}.\n Expected: %A{expectedResult}. "

              testCase "360 degree image counterclockwise rotation is equal to the original"
              <| fun _ ->
                  let actualResultPath =
                      __SOURCE_DIRECTORY__ + "/Images/input/dhruv-N9UuFddi7hs-unsplash.jpg"

                  let expectedResult = loadAs2DArray actualResultPath

                  let actualResult =
                      expectedResult
                      |> rotate2DArray true
                      |> rotate2DArray true
                      |> rotate2DArray true
                      |> rotate2DArray true

                  Expect.equal actualResult expectedResult $"Unexpected: %A{actualResult}.\n Expected: %A{expectedResult}. "

              testProperty "360 degree byte array2D counter/clockwise rotation is equal to the original"
              <| fun (arr: byte[,]) ->

                  let resultsArray =
                      [| arr |> rotate2DArray true |> rotate2DArray true |> rotate2DArray true |> rotate2DArray true
                         arr |> rotate2DArray false |> rotate2DArray false |> rotate2DArray false |> rotate2DArray false |]

                  Expect.allEqual resultsArray arr $"Unexpected: %A{resultsArray} and original {arr}.\n Expected equality. "

              testProperty "applying new filters to byte array2D changes the original"
              <| fun (arr: byte[,]) ->

                  let sharpenArray = applyFilterTo2DArray sharpenKernel arr
                  let darkenArray = applyFilterTo2DArray darkenKernel arr
                  let lightenArray = applyFilterTo2DArray lightenKernel arr

                  Expect.notEqual arr sharpenArray $"Unexpected: %A{sharpenArray} and original {arr}.\n Expected inequality. "
                  Expect.notEqual arr darkenArray $"Unexpected: %A{darkenArray} and original {arr}.\n Expected inequality. "
                  Expect.notEqual arr lightenArray $"Unexpected: %A{lightenArray} and original {arr}.\n Expected inequality. "

              ]