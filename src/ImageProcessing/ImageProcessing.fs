module ImageProcessing.ImageProcessing

open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats




let loadAs2DArray (file:string) =
    let img = Image.Load<L8> file
    let res = Array2D.zeroCreate img.Height img.Width
    for i in 0.. img.Width - 1 do
        for j in 0 .. img.Height - 1 do
            res.[j,i] <- img.Item(i,j).PackedValue
    printfn $"H=%A{img.Height} W=%A{img.Width}"
    res

let save2DByteArrayAsImage (imageData: byte[,]) file =
    let h = imageData.GetLength 0
    let w = imageData.GetLength 1
    printfn $"H=%A{h} W=%A{w}"
    let flat2Darray array2D =
        seq { for x in [0..(Array2D.length1 array2D) - 1] do
                  for y in [0..(Array2D.length2 array2D) - 1] do
                      yield array2D.[x, y] }
        |> Array.ofSeq
    let img = Image.LoadPixelData<L8>(flat2Darray imageData,w,h)
    img.Save file

let gaussianBlurKernel =
    [|
      [| 1; 4;  6;  4;  1|]
      [| 4; 16; 24; 16; 4|]
      [| 6; 24; 36; 24; 6|]
      [| 4; 16; 24; 16; 4|]
      [| 1; 4;  6;  4;  1|]
    |]
    |> Array.map (Array.map (fun x -> (float32 x) / 256.0f))

let edgesKernel =
    [|
      [|0;  0; -1;  0;  0|]
      [|0;  0; -1;  0;  0|]
      [|0;  0;  2;  0;  0|]
      [|0;  0;  0;  0;  0|]
      [|0;  0;  0;  0;  0|]
    |]
    |> Array.map (Array.map float32)

let applyFilter (filter: float32[][]) (img: byte[,]) =
    let imgH = img.GetLength 0
    let imgW = img.GetLength 1
    let filterD = (Array.length filter) / 2
    let filter = Array.concat filter
    let processPixel px py =
        let dataToHandle =
            [|
              for i in px - filterD .. px + filterD do
                for j in py - filterD .. py + filterD do
                    if i < 0 || i >= imgH || j < 0 || j >= imgW
                    then float32 img.[px,py]
                    else float32 img.[i,j]
            |]
        Array.fold2 (fun s x y -> s + x * y) 0.0f filter dataToHandle

    Array2D.mapi (fun x y _ -> byte (processPixel x y)) img
