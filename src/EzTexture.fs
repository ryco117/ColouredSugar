module EzTexture

open System.Drawing

open SixLabors.ImageSharp

type Texture = {Width: int; Height: int; Data: uint8[]}

let ReadFileToTexture (filePath: string) =
    let img = Image.Load<PixelFormats.Rgba32> filePath
    let width, height = img.Width, img.Height
    let bytesPerRow = 4 * width
    let imgData = Array.zeroCreate (bytesPerRow * height)
    for j = 0 to height - 1 do
        let row = img.GetPixelRowSpan j
        for i = 0 to width - 1 do
            let p = row.[i]
            imgData.[(height - 1 - j) * bytesPerRow + 4 * i] <- p.R
            imgData.[(height - 1 - j) * bytesPerRow + 4 * i + 1] <- p.G
            imgData.[(height - 1 - j) * bytesPerRow + 4 * i + 2] <- p.B
            imgData.[(height - 1 - j) * bytesPerRow + 4 * i + 3] <- p.A
    {Width = width; Height = height; Data = imgData}

let StringToTexture (str: string) (font: Font)=
    let img =
        let textRows = str.Split '\n'
        let width = Array.fold (fun acc (e: string) -> max acc e.Length) 0 textRows
        let height = textRows.Length
        new Bitmap(
            int (float32 width * font.SizeInPoints),
            height * font.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb)
    let width, height = img.Width, img.Height
    let graphics = Graphics.FromImage img
    graphics.TextRenderingHint <- Text.TextRenderingHint.AntiAlias
    let p = System.Drawing.PointF(0.f, 0.f)
    graphics.DrawString(str, font, Brushes.White, p)
    let bytesPerRow = 4 * width
    let data = Array.zeroCreate (bytesPerRow * height)
    for j = 0 to height - 1 do
        for i = 0 to width - 1 do
            let p = img.GetPixel(i, j)
            data.[(height - 1 - j) * bytesPerRow + 4 * i] <- p.R
            data.[(height - 1 - j) * bytesPerRow + 4 * i + 1] <- p.G
            data.[(height - 1 - j) * bytesPerRow + 4 * i + 2] <- p.B
            data.[(height - 1 - j) * bytesPerRow + 4 * i + 3] <- p.A
    {Width = width; Height = height; Data = data}