module ArguCommands

open Argu
//save
//load
//rotate
//applyFilter
(*type Editor =
    | Rotate of bool
    | GaussFilter
    | SharpenFilter
    | LightenFilter
    | DarkenFilter
    | EdgesFilter

type Kernel =
    |Gauss
    |Sharpen
    |Lighten
    |Darken
    |Edges*)
let first (x,_,_) = x
let second (_,x,_) = x
let third (_, _, x) = x

type CliArguments =
    | [<AltCommandLine("-r")>] Rotate of inputPath : string * outputPath : string * clockWise : bool
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Rotate _ -> "rotate image."

// -r /cdjhhsdh /dsjhdsbjs true
