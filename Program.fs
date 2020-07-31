(*
Coloured Sugar - Compute Shader driven audio visualizer
Copyright (C) 2020  Ryan Andersen

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.
*)

module Program

open OpenTK
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL
open OpenTK.Input

open SixLabors.ImageSharp

open EzCamera

// Try to use same RNG source application-wide
let random = new System.Random ()
let randF () = float32 (random.NextDouble ())
let randNormF () = (randF () - 0.5f) * 2.f

type ColouredSugar() as world =
    inherit GameWindow(
        1280, 720,
        new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8, 1),
        "Coloured Sugar", GameWindowFlags.Default)
    do world.VSync <- VSyncMode.On
    do world.Icon <-new System.Drawing.Icon "images/ColouredSugar.ico"
    let console = new ConsoleControls.Controller ()
    do console.Show false
    let mutable preFullscreenSize = System.Drawing.Size(1, 1)

    let camera = new EzCamera()
    let screenshotScale = 1.
    let screenshotsDir = "./screenshots"
    let mutable tick = 0UL
    let fpsWait: uint64 = 300UL
    let mutable mouseX = 0.f
    let mutable mouseY = 0.f
    let mutable mouseScroll = 0.f
    let mutable mouseLeftDown = false
    let mutable mouseRightDown = false
    let mutable targetCameraVelocity = Vector3.Zero
    let mutable holdShift = false
    let mutable autoRotate = true
    let mutable audioResponsive = true

    let sphere = new EzObjects.ColouredSphere(Vector3(0.75f, 0.75f, 0.75f), 3)
    let mutable sphereVelocity = new Vector3(-0.4f, 0.4f, -0.3f)
    let defaultSphereScale = new Vector3 0.125f
    do sphere.Scale <- Vector3.Zero

    let mutable overlay = true
    let texture = EzTexture.ReadFileToTexture "images/HelpMenu.png"
    let billboard = {
        new EzObjects.TexturedBillboard(Vector3.Zero, Vector3.One, 0.f, texture) with
        override this.Update _ =
            let textureWidth, textureHeight =
                match texture with
                | {Width = width; Height = height; Data = _} ->
                    let aspect = (float32 width * float32 world.Height) / (float32 height * float32 world.Width)
                    let f x = 2.f * min x 0.95f
                    let w = f (float32 width / float32 world.Width)
                    let h = f (float32 height / float32 world.Height)
                    if (w / h - aspect) / (aspect + w / h) > 0.01f then
                        aspect * h, h
                    else
                        w, w/aspect
            this.Position <- Vector3(-0.95f, 0.95f - textureHeight, 0.f)
            this.Scale <- Vector3(textureWidth, textureHeight, 1.f)
     }

    // Particle System
    let particleCount = 1024*1024
    let mutable particleRenderVAO = 0
    let mutable particleVBO = 0
    let mutable particleVelocityArray = 0
    let mutable particleCompShader = 0
    let mutable particleRenderShader = 0
    let defaultMass = 0.005f
    let mutable blackHole = new Vector4()
    let mutable curlAttractor = new Vector4()
    let mutable whiteHole = new Vector4()

    // Audio handler funciton
    let onDataAvail samplingRate (complex: NAudio.Dsp.Complex[]) =
        blackHole.W <- 0.f
        curlAttractor.W <- 0.f
        whiteHole.W <- 0.f
        if complex.Length > 0 then
            let mag (c: NAudio.Dsp.Complex) = sqrt(c.X*c.X + c.Y*c.Y)
            let detailedFreq i len =
                let flog = log (float32 i + 1.f)
                let ffrac = flog - float32 (int flog)
                floor flog / log (float32 len), ffrac
            let freqResolution = samplingRate / float complex.Length
            let analyze (arr: NAudio.Dsp.Complex[]) =
                let mutable max_mag = mag arr.[0]
                let mutable max_i = 0
                let mutable sum = max_mag
                for i = 1 to arr.Length - 1 do
                    let m = mag arr.[i]
                    sum <- sum + m
                    if m > max_mag then
                        max_mag <- m
                        max_i <- i
                let fl, ff = detailedFreq max_i arr.Length
                max_i, max_mag, sum, fl, ff
            let roundToInt f = int (round f)
            let bassEnd = roundToInt (300. / freqResolution)
            let midsStart = roundToInt (300. / freqResolution)
            let midsEnd = roundToInt (2_500. / freqResolution)
            let highStart = roundToInt (2_500. / freqResolution)
            let highEnd = roundToInt (16_000. / freqResolution)
            let bassMaxI, bassMaxMag, bassSum, bassFreqLog, bassFreqFrac = analyze (Array.sub complex 1 bassEnd)
            let midsMaxI, midsMaxMag, midsSum, midsFreqLog, midsFreqFrac = analyze (Array.sub complex midsStart (midsEnd - midsStart))
            let highMaxI, highMaxMag, highSum, highFreqLog, highFreqFrac = analyze (Array.sub complex highStart (highEnd - highStart))
            let negYaw = -camera.Yaw
            let toWorldSpace x y =
                if camera.Perspective then
                    Vector3(x * cos negYaw, y, x * sin negYaw)
                else
                    Vector3(x, y, 0.f)
            if bassMaxMag > 0.0175f then
                let X = 2.f * bassFreqLog - 1.f
                let Y = 2.f * bassFreqFrac - 1.f
                whiteHole.W <- defaultMass * bassSum * 1.15f
                whiteHole.Xyz <- toWorldSpace X Y
            if midsMaxMag > 0.000_125f then
                let X = 2.f * midsFreqLog - 1.f
                let Y = 2.f * midsFreqFrac - 1.f
                curlAttractor.W <- defaultMass * (sqrt midsSum) * 5.f
                curlAttractor.Xyz <- toWorldSpace  X Y
            if highMaxMag > 0.000_09f then
                let X = 2.f * highFreqLog - 1.f
                let Y = 2.f * highFreqFrac - 1.f
                blackHole.W <- defaultMass * (sqrt highSum) * 4.75f
                blackHole.Xyz <- toWorldSpace X Y
    let onClose () =
        blackHole.W <- 0.f
        curlAttractor.W <- 0.f
        whiteHole.W <- 0.f
    let audioOutCapture = new EzSound.AudioOutStreamer(onDataAvail, onClose)

    override this.OnKeyDown e =
        match e.Key, (e.Alt, e.Shift, e.Control, e.Command), e.IsRepeat with
        // Toggle Overlay/Help
        | Key.F1, _, false -> overlay <- not overlay
        // Escape
        | Key.F4, (true, false, false, false), _
        | Key.Escape, _, _ -> this.Exit ()
        // Fullscreen
        | Key.Enter, (true, false, false, false), false
        | Key.F11, (false, false, false, false), false ->
            (*if this.WindowBorder = WindowBorder.Hidden then
                this.WindowBorder <- WindowBorder.Resizable
                this.WindowState <- WindowState.Normal
                this.ClientSize <- preFullscreenSize
            else
                preFullscreenSize <- this.ClientSize
                this.WindowBorder <- WindowBorder.Hidden
                let device =
                    let x = this.X
                    let y = this.Y
                    DisplayDevice.GetDisplay (
                        Array.find
                            (fun e -> (DisplayDevice.GetDisplay e).Bounds.Contains(x, y))
                            (System.Enum.GetValues(typeof<DisplayIndex>) :?> DisplayIndex[]))
                this.X <- device.Bounds.Left
                this.Y <- device.Bounds.Top
                //this.Size <- System.Drawing.Size(device.Width, device.Height - 1)   // NOTE : A resize seems to be needed in order for changes to border visibility to always be noticed
                this.ClientSize <- System.Drawing.Size(device.Width, device.Height)*)
            if this.WindowState = WindowState.Fullscreen then
                this.WindowBorder <- WindowBorder.Resizable
                this.WindowState <- WindowState.Normal
                this.ClientSize <- preFullscreenSize
            else
                preFullscreenSize <- this.ClientSize
                this.WindowBorder <- WindowBorder.Hidden
                this.WindowState <- WindowState.Fullscreen
        // Alternate perspectives
        | Key.P, (true, false, false, false), false ->
            camera.Perspective <- not camera.Perspective
            if not camera.Perspective then
                camera.Yaw <- 0.f
        // Toggle sphere
        | Key.X, _, false ->
            if sphere.Scale.X > 0.f then
                sphere.Scale <- Vector3.Zero
            else
                sphere.Scale <- defaultSphereScale
        // Reset states of objects
        | Key.F5, _, false ->
            let velocities = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 0.f else randNormF () * 0.01f)
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVelocityArray)
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, velocities.Length * sizeof<float32>, velocities)
            let positions = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 1.f else randNormF ())
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVBO)
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, positions.Length * sizeof<float32>, positions)
            sphereVelocity <- new Vector3(0.25f, 0.5f, 0.4f)
            sphere.Position <- new Vector3(0.f)
            camera.Position <- new Vector3(0.f, 0.f, 0.975f)
            mouseScroll <- 0.f
        // Toggle auto rotate
        | Key.Z, _, false -> autoRotate <- not autoRotate
        // Toggle responsive to audio-out
        | Key.R, _, false ->
            if audioResponsive then
                audioResponsive <- false
                if audioOutCapture.Capturing () then
                    audioOutCapture.StopCapturing ()
            else
                audioResponsive <- true
                audioOutCapture.Reset ()
        // Movement keys
        | Key.A, _, false ->
            targetCameraVelocity.X <- targetCameraVelocity.X - 1.f
        | Key.D, _, false ->
            targetCameraVelocity.X <- targetCameraVelocity.X + 1.f
        | Key.W, _, false ->
            targetCameraVelocity.Z <- targetCameraVelocity.Z + 1.f
        | Key.S, _, false ->
            targetCameraVelocity.Z <- targetCameraVelocity.Z - 1.f
        // Hold LEFT-SHIFT to reduce walk speed
        | Key.ShiftLeft, _, false ->
            holdShift <- true
        // Toggle debug console
        | Key.Tilde, _, false -> console.Show(not (console.Visible()))
        // Save screenshot
        | Key.F12, _ , false ->
            GL.Enable EnableCap.Multisample
            GL.ReadBuffer ReadBufferMode.Front
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1)
            let width = int (float this.Width * screenshotScale)
            let height = int (float this.Height * screenshotScale)
            let framebufferId = GL.GenFramebuffer ()
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId)
            let renderbufferId = GL.GenRenderbuffer ()
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferId)
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, RenderbufferStorage.Rgb8, width, height)
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderbufferId)
            let depthbufferId = GL.GenRenderbuffer ()
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthbufferId)
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, RenderbufferStorage.DepthComponent, width, height)
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthbufferId)
            GL.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
            GL.Viewport(0, 0, width, height)

            // Perform draw
            GL.BindVertexArray particleRenderVAO
            GL.UseProgram particleRenderShader
            GL.DrawArrays(PrimitiveType.Points, 0, particleCount)
            let projView = camera.ProjView ()
            if sphere.Scale.X > 0.f then
                // Draw sphere
                GL.Disable EnableCap.CullFace
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line)
                sphere.Draw projView
                GL.Enable EnableCap.CullFace
                GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill)

            // Create a copy framebuffer to store multisampled buffer onto
            let copyBufferId = GL.GenFramebuffer ()
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, copyBufferId)
            let copyRenderbufferId = GL.GenRenderbuffer ()
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, copyRenderbufferId)
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgb8, width, height)
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, copyRenderbufferId)
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebufferId)
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, copyBufferId)
            GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest)

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, copyBufferId)
            GL.ReadBuffer ReadBufferMode.ColorAttachment0
            let rawImage = Array.zeroCreate<uint8> (width * height * 3)
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, &rawImage.[0])
            Async.Start (async {
                let bytesPerRow = width * 3
                let defaultName = screenshotsDir + "/awesome"
                let defaultPath = screenshotsDir + "/awesome.png"
                let filePath =
                    let rec newPath i =
                        let path = defaultName + (string i) + ".png"
                        if System.IO.File.Exists path then
                            newPath (i+1)
                        else
                            path
                    if System.IO.File.Exists defaultPath then
                        newPath 1
                    else
                        defaultPath
                let managedImage = new Image<PixelFormats.Rgb24>(width, height)
                for j = 0 to height - 1 do
                    for i = 0 to width - 1 do
                        managedImage.[i, j] <-
                            new PixelFormats.Rgb24 (
                                rawImage.[bytesPerRow * (height - j - 1) + 3 * i],
                                rawImage.[bytesPerRow * (height - j - 1) + 3 * i + 1],
                                rawImage.[bytesPerRow * (height - j - 1) + 3 * i + 2])
                let file = System.IO.File.OpenWrite filePath
                managedImage.SaveAsPng file
                printfn "Screenshot saved to '%s'" filePath
                file.Close ()
            })
            GL.Disable EnableCap.Multisample
            GL.DeleteRenderbuffer renderbufferId
            GL.DeleteRenderbuffer copyRenderbufferId
            GL.DeleteRenderbuffer depthbufferId
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
            GL.DeleteFramebuffer framebufferId
            GL.DeleteFramebuffer copyBufferId
            GL.Viewport(0, 0, this.Width, this.Height)
        // Default handling
        | _ -> base.OnKeyDown e
    override _.OnKeyUp e =
        match e.Key with
        // Movement keys
        | Key.A ->
            targetCameraVelocity.X <- targetCameraVelocity.X + 1.f
        | Key.D ->
            targetCameraVelocity.X <- targetCameraVelocity.X - 1.f
        | Key.W ->
            targetCameraVelocity.Z <- targetCameraVelocity.Z - 1.f
        | Key.S ->
            targetCameraVelocity.Z <- targetCameraVelocity.Z + 1.f
        | Key.ShiftLeft ->
            holdShift <- false
        | _ -> base.OnKeyUp e
    override this.OnMouseMove e =
        mouseX <- (float32 e.X / float32 this.Width) * 2.f - 1.f
        mouseY <- (float32 e.Y / float32 this.Height) * -2.f + 1.f
        base.OnMouseMove e
    override _.OnMouseWheel e =
        mouseScroll <- mouseScroll + e.DeltaPrecise
        base.OnMouseWheel e
    override _.OnMouseDown e =
        if e.Button = MouseButton.Left then
            mouseLeftDown <- true
        if e.Button = MouseButton.Right then
            mouseRightDown <- true
        base.OnMouseDown e
    override _.OnMouseUp e =
        if e.Button = MouseButton.Left then
            mouseLeftDown <- false
        if e.Button = MouseButton.Right then
            mouseRightDown <- false
        base.OnMouseUp e
    override _.OnLoad eventArgs =
        // Create screenshots directory
        System.IO.Directory.CreateDirectory screenshotsDir |> ignore

        // Set default values
        GL.ClearColor (0.f, 0.f, 0.f, 1.f)
        GL.CullFace CullFaceMode.Back
        GL.Enable EnableCap.CullFace
        GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill)
        GL.DepthFunc DepthFunction.Less
        GL.Enable EnableCap.DepthTest
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)
        GL.Enable EnableCap.Blend

        // Load particle system
        particleCompShader <- EzShader.CreateComputeShader "shaders/particle_comp.glsl"
        particleRenderShader <- EzShader.CreateShaderProgram "shaders/particle_vert.glsl" "shaders/particle_frag.glsl"
        let particlePos = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 1.f else randNormF ())
        particleVBO <- GL.GenBuffer ()
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVBO)
        GL.BufferData(BufferTarget.ShaderStorageBuffer, particlePos.Length * sizeof<float32>, particlePos, BufferUsageHint.StreamDraw)
        let velocities = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 0.f else randNormF () * 0.01f)
        particleVelocityArray <- GL.GenBuffer ()
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVelocityArray)
        GL.BufferData(BufferTarget.ShaderStorageBuffer, velocities.Length * sizeof<float32>, velocities, BufferUsageHint.StreamDraw)
        particleRenderVAO <- GL.GenVertexArray ()
        GL.BindVertexArray particleRenderVAO
        GL.BindBuffer(BufferTarget.ArrayBuffer, particleVBO)
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0)
        GL.EnableVertexAttribArray 0
        GL.BindBuffer(BufferTarget.ArrayBuffer, particleVelocityArray)
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0)
        GL.EnableVertexAttribArray 1

        base.OnLoad eventArgs
    override _.OnUnload eventArgs =
        (audioOutCapture :> System.IDisposable).Dispose ()
        base.OnUnload eventArgs
    override this.OnResize eventArgs =
        GL.Viewport (0, 0, this.Width, this.Height)
        base.OnResize eventArgs
    override this.OnRenderFrame eventArgs =
        let deltaTime = float32 eventArgs.Time
        GL.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

        if sphere.Scale.X > 0.f then
            // Update sphere
            sphereVelocity.Y <- sphereVelocity.Y - deltaTime
            let mutable pos = sphere.Position + deltaTime*sphereVelocity
            if abs pos.X > 0.7f then
                sphereVelocity.X <- -sphereVelocity.X
                pos.X <- pos.X + deltaTime*sphereVelocity.X
            if abs pos.Y > 0.7f then
                sphereVelocity.Y <- -sphereVelocity.Y
                pos.Y <- pos.Y + deltaTime*sphereVelocity.Y
            if abs pos.Z > 0.7f then
                sphereVelocity.Z <- -sphereVelocity.Z
                pos.Z <- pos.Z + deltaTime*sphereVelocity.Z
            sphere.Position <- pos

        // Particle computation
        GL.UseProgram particleCompShader
        GL.Uniform1(GL.GetUniformLocation(particleCompShader, "deltaTime"), deltaTime)
        GL.Uniform1(GL.GetUniformLocation(particleCompShader, "perspective"), if camera.Perspective then 1u else 0u)
        GL.Uniform4(
            GL.GetUniformLocation(particleCompShader, "attractors[0]"),
            new Vector4(
                camera.ToWorldSpace mouseX mouseY,
                if mouseLeftDown then
                    if mouseRightDown then
                        1.5f * defaultMass * 1.4f**mouseScroll
                    else
                        -defaultMass * 1.4f**mouseScroll
                else
                    0.f))
        GL.Uniform4(GL.GetUniformLocation(particleCompShader, "attractors[1]"), blackHole)
        GL.Uniform4(GL.GetUniformLocation(particleCompShader, "bigBoomer"), whiteHole)
        GL.Uniform4(GL.GetUniformLocation(particleCompShader, "curlAttractor"), curlAttractor)
        GL.Uniform4(
            GL.GetUniformLocation(particleCompShader, "musicalSphere"),
            new Vector4(
                sphere.Position,
                (sphere.Scale.X + sphere.Scale.Y + sphere.Scale.Z)/3.f))
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, particleVBO)
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, particleVelocityArray)
        GL.DispatchCompute(particleCount / 128, 1, 1)
        GL.MemoryBarrier (MemoryBarrierFlags.ShaderStorageBarrierBit)

        // Draw particles
        GL.BindVertexArray particleRenderVAO
        GL.UseProgram particleRenderShader
        let mutable projViewMutable =
            if autoRotate then
                camera.Yaw <- camera.Yaw + 0.08f * deltaTime
            let t = deltaTime / 0.2f
            camera.ForwardVelocity <- (1.f - t) * camera.ForwardVelocity + t * targetCameraVelocity.Z * if holdShift then 0.33f else 1.f
            camera.StrafeRight <- (1.f - t) * camera.StrafeRight + t * targetCameraVelocity.X * if holdShift then 0.33f else 1.f
            camera.Update deltaTime
            camera.ProjView ()
        GL.UniformMatrix4(GL.GetUniformLocation(particleRenderShader, "projViewMatrix"), true, &projViewMutable)
        GL.Uniform1(GL.GetUniformLocation(particleRenderShader, "perspective"), if camera.Perspective then 1u else 0u)
        GL.DrawArrays(PrimitiveType.Points, 0, particleCount)

        if sphere.Scale.X > 0.f then
            // Draw sphere
            GL.Disable EnableCap.CullFace
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line)
            sphere.Draw projViewMutable
            GL.Enable EnableCap.CullFace
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill)

        if overlay then
            billboard.Update deltaTime
            billboard.Draw ()

        this.Context.SwapBuffers ()

        // Tick management and occasional operations
        tick <- tick + 1UL
        if tick % fpsWait = 0UL then
            printfn "FPS: %f" this.RenderFrequency

        // Occasionally check if audio out stopped
        if audioResponsive && tick % 100UL = 0UL && audioOutCapture.Stopped () then
            audioOutCapture.StartCapturing ()

        base.OnRenderFrame eventArgs

[<EntryPoint>]
let main _ =
    let options = ToolkitOptions.Default
    options.Backend <- PlatformBackend.PreferNative
    let toolkit = Toolkit.Init options
    let game = new ColouredSugar()
    game.Run ()
    game.Dispose ()
    toolkit.Dispose ()
    0