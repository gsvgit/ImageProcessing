module ImageProcessing.Streaming

open ImageProcessing.ImageProcessing

let listAllFiles dir =
    let files = System.IO.Directory.GetFiles dir
    List.ofArray files

type msg =
    | Img of Image
    | EOS of AsyncReplyChannel<unit>

let imgSaver saveImage =
    
    MailboxProcessor.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()

            match msg with
            | EOS ch ->
                printfn "Image saver is finished!"
                ch.Reply()
            | Img img ->
                printfn $"Save: %A{img.Name}"
                saveImage img
                return! loop ()
        }
        loop ()
    )


let imgProcessor filterApplicator (imgSaver: MailboxProcessor<_>) =
    let filter = filterApplicator
    MailboxProcessor.Start(fun inbox ->
        let rec loop cnt = async {
            let! msg = inbox.Receive()
            match msg with
            | EOS ch ->
                printfn "Image processor is ready to finish!"
                imgSaver.PostAndReply EOS
                printfn "Image processor is finished!"
                ch.Reply()
            | Img img ->
                printfn $"Filter: %A{img.Name}"
                let filtered = filter img
                imgSaver.Post(Img filtered)
                return! loop (not cnt)
        }
        loop true
    )

let processAllFiles inDir outDir filterApplicators =

    let mutable cnt = 0

    let outFile (imgName: string) = System.IO.Path.Combine(outDir, imgName)

    let saveImageToDir (img:Image) = saveImage img (outFile img.Name)

    let imgProcessors =
        filterApplicators
        |> List.map (fun x ->
            let imgSaver = imgSaver saveImageToDir
            imgProcessor x imgSaver
        )
        |> Array.ofList

    let filesToProcess = listAllFiles inDir

    while cnt < List.length filesToProcess do
        let p = (imgProcessors |> Array.minBy (fun p -> p.CurrentQueueLength))
        if p.CurrentQueueLength < 3 
        then 
           p.Post (Img(loadAsImage (filesToProcess[cnt])))
           cnt <- cnt + 1

(*
    for file in filesToProcess do
        (imgProcessors
         |> Array.minBy (fun p -> p.CurrentQueueLength))
            .Post(Img(loadAsImage file))
*)

    for imgProcessor in imgProcessors do
        imgProcessor.PostAndReply EOS


let processAllLoadedFiles inImages filterApplicators =    
    let result = new ResizeArray<_>()

    let mutable cnt = 0

    let saveImageToArr (img:Image) = result.Add img

    let imgProcessors =
        filterApplicators
        |> List.map (fun x ->
            let imgSaver = imgSaver saveImageToArr
            imgProcessor x imgSaver
        )
        |> Array.ofList
    
    while cnt < List.length inImages do
        let p = (imgProcessors |> Array.minBy (fun p -> p.CurrentQueueLength))
        if p.CurrentQueueLength < 3 
        then 
           p.Post inImages[cnt]
           cnt <- cnt + 1

    for imgProcessor in imgProcessors do
        imgProcessor.PostAndReply EOS
