module ArgCommands

open Argu
open CPUImageProcessing

type Kernel =
    |Gauss
    |Sharpen
    |Lighten
    |Darken
    |Edges

let kernelParser (kernel : Kernel) =
    match kernel with
    |Gauss -> gaussianBlurKernel
    |Sharpen -> sharpenKernel
    |Lighten -> lightenKernel
    |Darken -> darkenKernel
    |Edges -> edgesKernel

let first (x,_,_) = x
let second (_,x,_) = x
let third (_, _, x) = x

type CliArguments =
    | [<AltCommandLine("-rt")>] Rotate of inputPath : string * outputPath : string * clockWise : bool
    | [<AltCommandLine("-fl")>] Filter of inputPath : string * outputPath : string * kernel : Kernel

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Rotate _ -> "rotate image."
            | Filter _ -> "filter image."
