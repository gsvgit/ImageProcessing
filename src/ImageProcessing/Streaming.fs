module ImageProcessing.Streaming

open ImageProcessing.ImageProcessing

let listAllFiles dir =
    let files = System.IO.Directory.GetFiles dir
    List.ofArray files

type msg =
    | Img of Image
    | EOS of AsyncReplyChannel<unit>

let imgSaver outDir =
    let outFile (imgName: string) = System.IO.Path.Combine(outDir, imgName)

    MailboxProcessor.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()

            match msg with
            | EOS ch ->
                printfn "Image saver is finished!"
                ch.Reply()
            | Img img ->
                printfn $"Save: %A{img.Name}"
                saveImage img (outFile img.Name)
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

    let imgProcessors =
        filterApplicators
        |> List.map (fun x ->
            let imgSaver = imgSaver outDir
            imgProcessor x imgSaver
        )
        |> Array.ofList

    let filesToProcess = listAllFiles inDir

    for file in filesToProcess do
        //while (imgProcessors |> Array.minBy (fun p -> p.CurrentQueueLength)).CurrentQueueLength > 3 do ()
        (imgProcessors
         |> Array.minBy (fun p -> p.CurrentQueueLength))
            .Post(Img(loadAsImage file))

        cnt <- cnt + 1

    for imgProcessor in imgProcessors do
        imgProcessor.PostAndReply EOS
//imgSaver.PostAndReply EOS
