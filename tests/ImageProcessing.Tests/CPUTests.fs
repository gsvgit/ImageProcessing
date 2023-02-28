namespace CPUTests

open Expecto
open CPUImageProcessing

module RotationTests =
    [<Tests>]
    let tests =
        testList
            "RotationTests"
            [
              testCase "360 degree clockwise rotation"
              <| fun _ ->
                  let actualResultPath = __SOURCE_DIRECTORY__ + "/Images/input/yaroslav-beregovyi-g--c9rWAiCY-unsplash.jpg"
                  let expectedResult = loadAs2DArray actualResultPath
                  let actualResult =
                      expectedResult |> rotate2DArray true |> rotate2DArray true |> rotate2DArray true |> rotate2DArray true

                  Expect.equal actualResult expectedResult
                      $"Unexpected: %A{actualResult}.\n Expected: %A{expectedResult}"

              testCase "90 degree clockwise rotation"
              <| fun _ ->
                  let actualResultPath = __SOURCE_DIRECTORY__ + "/Images/input/yaroslav-beregovyi-g--c9rWAiCY-unsplash.jpg"
                  let expectedResultPath = __SOURCE_DIRECTORY__ + "/Images/expected/ddd.jpg"

                  let image2DArray = loadAs2DArray actualResultPath

                  let actualResult =
                      image2DArray |> rotate2DArray true
                  let expectedResult = loadAs2DArray expectedResultPath

                  Expect.equal actualResult expectedResult
                      $"Unexpected: %A{actualResult}.\n Expected: %A{expectedResult}"

            ]
