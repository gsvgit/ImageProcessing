module ImageFolderProcessing

open CPUImageProcessing

let listAllImages directory =

    let allowableExtensions =
        [| ".jpg"; ".jpeg"; ".png"; ".gif"; ".webp"; ".pbm"; ".bmp"; ".tga"; ".tiff" |]

    let allFilesSeq = System.IO.Directory.EnumerateFiles directory

    let allowableFilesSeq =
        Seq.filter (fun (path: string) -> Array.contains (System.IO.Path.GetExtension path) allowableExtensions) allFilesSeq

    printfn $"Images in %A{directory} directory : %A{allowableFilesSeq}"

    List.ofSeq allowableFilesSeq

let processAllFiles inputDirectory outputDirectory imageEditor =

    let generatePath (filePath: string) =
        System.IO.Path.Combine(outputDirectory, System.IO.Path.GetFileName filePath)

    let imageProcessAndSave path =
        let image = loadAs2DArray path
        let editedImage = imageEditor image
        generatePath path |> save2DArrayAsImage editedImage

    listAllImages inputDirectory |> List.map imageProcessAndSave |> ignore
