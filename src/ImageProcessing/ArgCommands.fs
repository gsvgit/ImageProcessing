module ArgCommands

open System.Diagnostics
open Argu
open CPUImageProcessing

type Kernel =
    | Gauss
    | Sharpen
    | Lighten
    | Darken
    | Edges

let kernelParser kernel =
    match kernel with
    | Gauss -> gaussianBlurKernel
    | Sharpen -> sharpenKernel
    | Lighten -> lightenKernel
    | Darken -> darkenKernel
    | Edges -> edgesKernel

type Rotation =
    | Clockwise
    | Counterclockwise

let rotationParser rotation =
    match rotation with
    | Clockwise -> true
    | Counterclockwise -> false

type Processor =
    | Gauss
    | Sharpen
    | Lighten
    | Darken
    | Edges
    | Clockwise
    | Counterclockwise

let processorParser p =
    match p with
    | Clockwise -> rotate2DArray true
    | Counterclockwise -> rotate2DArray false
    | Gauss -> applyFilterTo2DArray gaussianBlurKernel
    | Sharpen -> applyFilterTo2DArray sharpenKernel
    | Lighten -> applyFilterTo2DArray lightenKernel
    | Darken -> applyFilterTo2DArray darkenKernel
    | Edges -> applyFilterTo2DArray edgesKernel

type ClIArguments =
    | [< AltCommandLine("-pr")>] Process of inputPath: string * outputPath: string
    | [<AltCommandLine("-li")>] ProcessList of Processor list

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Process _ -> "edit image using sequence of modifications."
            | ProcessList _ -> "ne visivai"
