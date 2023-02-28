module ArgCommands

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

type ClIArguments =
    | [<Unique; AltCommandLine("-rt")>] Rotate of inputPath: string * outputPath: string * isClockwise: bool
    | [<Unique; AltCommandLine("-fl")>] Filter of inputPath: string * outputPath: string * kernel: Kernel

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Rotate _ -> "rotate images to the right when parameter is true or to the left when false."
            | Filter _ -> "set filter to images."
