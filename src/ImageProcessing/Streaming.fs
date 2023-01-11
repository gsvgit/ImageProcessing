module ImageProcessing.Streaming

let listAllFiles dir =
    let files = System.IO.Directory.GetFiles dir
    List.ofArray files

type msg =
    | Img of string*byte[,]
    | EOS of AsyncReplyChannel<unit>

let imgSaver outDir =
    let outFile (fileFullPath:string) =
        System.IO.Path.Combine(outDir, System.IO.Path.GetFileName fileFullPath)

    MailboxProcessor.Start(fun inbox ->
        let rec loop () =
            async{
                let! msg = inbox.Receive()
                match msg with
                | EOS ch ->
                    printfn "Image saver is finished!"
                    ch.Reply()
                | Img (file, img) ->
                    printfn $"Save: %A{file}"
                    ImageProcessing.save2DByteArrayAsImage img (outFile file)
                    return! loop ()
            }
        loop ()
        )

let imgProcessor filterApplicator (imgSaver:MailboxProcessor<_>) =

    let filter = filterApplicator

    MailboxProcessor.Start(fun inbox ->
        let rec loop cnt =
            async{
                let! msg = inbox.Receive()
                match msg with
                | EOS ch ->
                    printfn "Image processor is ready to finish!"
                    imgSaver.PostAndReply EOS
                    printfn "Image processor is finished!"
                    ch.Reply()
                | Img (file,img) ->
                    printfn $"Filter: %A{file}"
                    let filtered = filter img
                    imgSaver.Post (Img (file,filtered))
                    return! loop (not cnt)
            }
        loop true
        )

let processAllFiles inDir outDir filterApplicator =
    let imgSaver = imgSaver outDir
    let imgProcessor = imgProcessor filterApplicator imgSaver
    let filesToProcess = listAllFiles inDir
    for file in filesToProcess do
        imgProcessor.Post <| Img(file, ImageProcessing.loadAs2DArray file)
    imgProcessor.PostAndReply EOS
