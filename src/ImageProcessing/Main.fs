namespace ImageProcessing

open Brahma.FSharp

module Main =
    let pathToExamples = "/home/gsv/Projects/TestProj2020/src/ImgProcessing/Examples"
    let inputFolder = System.IO.Path.Combine(pathToExamples,"input")
    let demoFile = System.IO.Path.Combine(inputFolder, "armin-djuhic-ohc29QXbS-s-unsplash.jpg")
    [<EntryPoint>]
    let main (argv: string array) =
        let device =
            //ClDevice.GetAvailableDevices(platform=Platform.Nvidia) |> Seq.head
            ClDevice.GetFirstAppropriateDevice()
        printfn $"Device: %A{device.Name}"

        let context = ClContext(device)
        let applyFiltersGPU = ImageProcessing.applyFiltersGPU context 64

        //let grayscaleImage = ImageProcessing.loadAs2DArray demoFile
        //let blur = ImageProcessing.applyFilter ImageProcessing.gaussianBlurKernel grayscaleImage
        //let edges = ImageProcessing.applyFilter ImageProcessing.edgesKernel blur
        //let edges =  applyFiltersGPU [ImageProcessing.gaussianBlurKernel; ImageProcessing.edgesKernel] grayscaleImage
        //ImageProcessing.save2DByteArrayAsImage edges "../../../../../out/demo_grayscale.jpg"
        Streaming.processAllFiles inputFolder  "../../../../../out/" (applyFiltersGPU [ImageProcessing.gaussianBlurKernel;ImageProcessing.gaussianBlurKernel;ImageProcessing.edgesKernel])
        0
