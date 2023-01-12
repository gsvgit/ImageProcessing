namespace ImageProcessing

open Brahma.FSharp

module Main =
    let pathToExamples = "/home/gsv/Projects/TestProj2020/src/ImgProcessing/Examples"
    let inputFolder = System.IO.Path.Combine(pathToExamples,"input")
    let demoFile = System.IO.Path.Combine(inputFolder, "armin-djuhic-ohc29QXbS-s-unsplash.jpg")
    [<EntryPoint>]
    let main (argv: string array) =
        let nvidiaDevice =
            ClDevice.GetAvailableDevices(platform=Platform.Nvidia) |> Seq.head
        let intelDevice =
            ClDevice.GetAvailableDevices(platform=Platform.Intel) |> Seq.head
            //ClDevice.GetFirstAppropriateDevice()
        //printfn $"Device: %A{device.Name}"

        let nvContext = ClContext(nvidiaDevice)
        let applyFiltersOnNvGPU = ImageProcessing.applyFiltersGPU nvContext 64

        let intelContext = ClContext(intelDevice)
        let applyFiltersOnIntelGPU = ImageProcessing.applyFiltersGPU intelContext 64

        let filters = [ImageProcessing.gaussianBlurKernel;ImageProcessing.gaussianBlurKernel;ImageProcessing.edgesKernel]

        //let grayscaleImage = ImageProcessing.loadAs2DArray demoFile
        //let blur = ImageProcessing.applyFilter ImageProcessing.gaussianBlurKernel grayscaleImage
        //let edges = ImageProcessing.applyFilter ImageProcessing.edgesKernel blur
        //let edges =  applyFiltersGPU [ImageProcessing.gaussianBlurKernel; ImageProcessing.edgesKernel] grayscaleImage
        //ImageProcessing.save2DByteArrayAsImage edges "../../../../../out/demo_grayscale.jpg"
        let start = System.DateTime.Now
        Streaming.processAllFiles inputFolder  "../../../../../out/" [applyFiltersOnNvGPU filters; applyFiltersOnIntelGPU filters]
        printfn $"TotalTime = %f{(System.DateTime.Now - start).TotalMilliseconds}"
        0
