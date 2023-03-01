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

type Rotation =
    | Clockwise
    | Counterclockwise

let rotationParser rotation =
    match rotation with
    | Clockwise -> true
    | Counterclockwise -> false

(*type Modifications =
    | Filter of Kernel * Modifications list
    | Rotate of Rotation * Modifications
    | Stop

let modiParser modi=
    match modi with
    | Filter (kernel, Stop) -> [kernelParser kernel |> applyFilterTo2DArray]
    | Rotate rotation -> rotationParser rotation |> rotate2DArray*)

type Modifications =
    | Filter of Kernel
    | Rotate of Rotation

let modiParser modi=
    match modi with
    | Filter kernel -> kernelParser kernel |> applyFilterTo2DArray
    | Rotate rotation -> rotationParser rotation |> rotate2DArray

type ClIArguments =
    | [<Unique; AltCommandLine("-rt")>] Rotate of inputPath: string * outputPath: string * rotation: Rotation
    | [<Unique; AltCommandLine("-fl")>] Filter of inputPath: string * outputPath: string * kernel: Kernel
    | [< AltCommandLine("-ed")>] Edit of inputPath: string * outputPath: string * modList: list<Modifications>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Rotate _ -> "rotate images to the right when parameter is true or to the left when false."
            | Filter _ -> "set filter to images."
            | Edit _ -> "edit image using sequence of modifications."
