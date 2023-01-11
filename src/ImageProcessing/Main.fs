namespace ImageProcessing

module Main =
    let pathToExamples = "/home/gsv/Projects/TestProj2020/src/ImgProcessing/Examples"
    let inputFolder = System.IO.Path.Combine(pathToExamples,"input")
    let demoFile = System.IO.Path.Combine(inputFolder, "armin-djuhic-ohc29QXbS-s-unsplash.jpg")
    [<EntryPoint>]
    let main (argv: string array) =
        let grayscaleImage = ImageProcessing.loadAs2DArray demoFile
        let blur = ImageProcessing.applyFilter ImageProcessing.gaussianBlurKernel grayscaleImage
        let edges = ImageProcessing.applyFilter ImageProcessing.edgesKernel blur
        ImageProcessing.save2DByteArrayAsImage edges "../../../../../out/demo_grayscale.jpg"
        0
