module ArgCommands

open Argu
open CPUImageProcessing

type Processor =
    | Gauss
    | Sharpen
    | Lighten
    | Darken
    | Edges
    | RotationR
    | RotationL

let processorParser p =
    match p with
    | Gauss -> applyFilterTo2DArray gaussianBlurKernel
    | Sharpen -> applyFilterTo2DArray sharpenKernel
    | Lighten -> applyFilterTo2DArray lightenKernel
    | Darken -> applyFilterTo2DArray darkenKernel
    | Edges -> applyFilterTo2DArray edgesKernel
    | RotationR -> rotate2DArray true
    | RotationL -> rotate2DArray false

type ClIArguments =
    | [<Mandatory; AltCommandLine("-pt")>] Paths of inputPath: string * outputPath: string
    | [<Mandatory; MainCommand>] Process of list<Processor>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Paths _ -> "input: path to a file or a directory, output: path to a file or directory."
            | Process _ -> "list of available processors."
