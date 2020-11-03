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
open ColouredSugarConfig

// Define type for storing a note
type Note = {freq: float32; mag: float32}

// Define auto rotate types
type AutoRotate =
| Off
| Classic
| Full

// Try to use same RNG source application-wide
let random = System.Random ()
let randF () = float32 (random.NextDouble ())
let randNormF () = (randF () - 0.5f) * 2.f

// Grab handle to console window
let console = ConsoleControls.Controller ()

// Get default path for ColouredSugar screenshots
let screenshotsDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures) + """\ColouredSugar\""";

type ColouredSugar(config: Config) as world =
    inherit GameWindow(
        1280, 720,
        GraphicsMode(ColorFormat(8, 8, 8, 8), 24, 8, 1),
        "ColouredSugar", GameWindowFlags.Default)
    let mutable preFullscreenSize = System.Drawing.Size(1280, 720)
    do
        world.VSync <- if config.EnableVSync then VSyncMode.On else VSyncMode.Off
        world.Icon <- new System.Drawing.Icon "res/ColouredSugar.ico"
        if config.FullscreenOnLaunch then
            world.WindowBorder <- WindowBorder.Hidden
            world.WindowState <- WindowState.Fullscreen

    let camera = EzCamera(float32 config.CameraOrbitSpeed, float32 config.CameraMoveSpeed, 16.f/9.f)
    let screenshotScale = float config.ScreenshotScale
    let mutable tick = 0UL
    let fpsWait: uint64 = 300UL
    let mutable mouseX = 0.f
    let mutable mouseY = 0.f
    let mutable mouseScroll = float32 config.CursorForceInitial
    let mutable mouseLeftDown = false
    let mutable mouseRightDown = false
    let mutable mouseLastMove = System.DateTime.MinValue
    let mutable mouseHidden = false
    let cursorWait = float config.CursorHideAfterSeconds
    let cursorForceInverseFactor = float32 config.CursorForceInverseFactor
    let cursorForceScrollFactor = float32 config.CursorForceScrollIncrease
    let mutable targetCameraVelocity = Vector3.Zero
    let mutable holdShift = false
    let mutable autoRotate = Full
    let autoRotateSpeed = float32 config.AutoOrbitSpeed
    let audioDisconnectCheckRate = uint64 config.AudioDisconnectCheckWait
    let mutable audioResponsive = true
    let mutable fixParticles = true
    let mutable cubeRotation = Quaternion(0.f, 0.f, 0.f, 1.f)
    let mutable cubeAngularVelocity = Vector4(0.f, 1.f, 0.f, autoRotateSpeed)
    let mutable lastAngularChange = System.DateTime.UtcNow

    let sphere = new EzObjects.ColouredSphere(Vector3(0.75f, 0.75f, 0.75f), 3)
    let defaultSphereVelocity =
        Vector3(
            float32 config.BouncingBallVelocity.X,
            float32 config.BouncingBallVelocity.Y,
            float32 config.BouncingBallVelocity.Z)
    let mutable sphereVelocity = defaultSphereVelocity
    let defaultSphereScale = Vector3 (float32 config.BouncingBallSize)
    do sphere.Scale <- Vector3.Zero

    let mutable overlay = config.ShowHelpOnLaunch
    let texture = EzTexture.ReadFileToTexture "res/HelpMenu.png"
    let billboard = {
        new EzObjects.TexturedBillboard(Vector3.Zero, Vector3.One, 0.f, texture) with
        override this.Update _ =
            let textureWidth, textureHeight =
                match texture with
                | {Width = width; Height = height; Data = _} ->
                    let aspect = (float32 width * float32 world.Height) / (float32 height * float32 world.Width)
                    let f x = 2.f * min x 0.98f
                    let w = f (float32 width / float32 world.Width)
                    let h = f (float32 height / float32 world.Height)
                    if (w / h - aspect) / (aspect + w / h) > 0.01f then
                        aspect * h, h
                    else
                        w, w/aspect
            this.Position <- Vector3(-0.98f, 0.98f - textureHeight, 0.f)
            this.Scale <- Vector3(textureWidth, textureHeight, 1.f)
     }

    // Particle System
    let particleCount = config.ParticleCount
    let fixedParticlePositions =
        let hilbertCurve = Array.init particleCount (fun i -> CubeFillingCurve.curveToCube ((float i) / (float particleCount)))
        Array.init<float32> (particleCount * 4) (fun i -> match (i % 4) with
                                                          | 0 -> hilbertCurve.[i/4].X
                                                          | 1 -> hilbertCurve.[i/4].Y
                                                          | 2 -> hilbertCurve.[i/4].Z
                                                          | 3 -> 1.f
                                                          | _ -> raise (System.Exception "Mathematics is broken!"))
    let mutable particleRenderVAO = 0
    let mutable particleVBO = 0
    let mutable particlePositions = Array.empty<float32>
    let mutable particleVelocityArray = 0
    let mutable particleVelocities = Array.empty<float32>
    let mutable particleFixedPosArray = 0
    let mutable particleCompShader = 0
    let mutable particleRenderShader = 0
    let defaultMass = 0.005f
    let mutable blackHoles = Array.zeroCreate<Vector4> 6
    let mutable curlAttractors = Array.zeroCreate<Vector4> 5
    let mutable whiteHoles = Array.zeroCreate<Vector4> 2

    // Audio handler funciton
    let mutable complexZero = NAudio.Dsp.Complex ()
    do complexZero.X <- 0.f
    do complexZero.Y <- 0.f
    let mutable previousBass = Array.create 2 [|complexZero|]
    let mutable previousBassIndex = 0
    let onDataAvail samplingRate (complex: NAudio.Dsp.Complex[]) =
        if complex.Length > 0 then
            let mag (c: NAudio.Dsp.Complex) = sqrt(c.X*c.X + c.Y*c.Y)
            let toWorldSpace t =
                let s = System.Math.Pow(float t, 0.4)
                CubeFillingCurve.curveToCubeN 8 s
            let freqResolution = samplingRate / float complex.Length
            let getStrongest maxCount delta (input: NAudio.Dsp.Complex[]) =
                let fLen = float32 input.Length
                let arr = Array.init input.Length (fun i -> {freq = (float32 i) / fLen; mag = mag input.[i]})
                let cmp {freq = _; mag = a} {freq = _; mag = b} = sign (b - a)
                let sorted = Array.sortWith cmp arr
                let rec getList acc size (arr: Note[]) =
                    if arr.Length = 0  || size = maxCount then
                        acc
                    else
                        let t = arr.[0].freq
                        let remaining, friends = Array.partition (fun {freq = s; mag = _} -> abs (t - s) > delta) arr
                        let m = Array.fold (fun acc {freq = _; mag = m} -> acc + m) 0.f friends
                        getList ({freq = t; mag = m}::acc) (size + 1) remaining
                List.toArray (List.rev (getList [] 0 sorted))
            let roundToInt f = int (round f)
            let bassStart = roundToInt (float config.BassStartFreq / freqResolution)
            let bassEnd = roundToInt (float config.BassEndFreq / freqResolution)
            let midsStart = roundToInt (float config.MidsStartFreq / freqResolution)
            let midsEnd = roundToInt (float config.MidsEndFreq / freqResolution)
            let highStart = roundToInt (float config.HighStartFreq / freqResolution)
            let highEnd = roundToInt (float config.HighEndFreq / freqResolution)
            let bassArray = Array.sub complex bassStart (bassEnd - bassStart)
            let bassNotes = getStrongest whiteHoles.Length 0.15f bassArray
            let midsNotes = getStrongest curlAttractors.Length 0.075f (Array.sub complex midsStart (midsEnd - midsStart))
            let highNotes = getStrongest blackHoles.Length 0.125f (Array.sub complex highStart (highEnd - highStart))
            let avgLastBassMag x =
                let mutable s = 0.f
                for i = 0 to previousBass.Length - 1 do
                    s <- s +
                        if previousBass.[i].Length = 0 then
                            0.f
                        else
                            let j =
                                let j = int (round (x * float32 previousBass.[i].Length))
                                if j >= previousBass.[i].Length then previousBass.[i].Length - 1 else j
                            mag previousBass.[i].[j]
                s / float32 previousBass.Length
            let volume = 
                let summer a = Array.sumBy (fun n -> n.mag) a
                summer bassNotes + summer midsNotes + summer highNotes
            let mutable canJerk = autoRotate = Full
            for i = 0 to bassNotes.Length - 1 do
                if bassNotes.[i].mag > float32 config.MinimumBass && bassNotes.[i].mag > 1.25f * avgLastBassMag bassNotes.[i].freq then
                    whiteHoles.[i] <- Vector4(
                        toWorldSpace bassNotes.[i].freq,
                        defaultMass * bassNotes.[i].mag * float32 config.WhiteHoleStrength)
                else
                    whiteHoles.[i] <- Vector4()

                if canJerk &&
                    bassNotes.[i].mag > float32 config.MinimumBassForJerk &&
                    (System.DateTime.UtcNow - lastAngularChange).TotalSeconds > 2. &&
                    bassNotes.[i].mag > 10.f * avgLastBassMag bassNotes.[i].freq then
                    cubeAngularVelocity <- Vector4(
                        (toWorldSpace bassNotes.[i].freq).Normalized(),
                        (float32 (System.Math.Pow((float volume), 1.15))) * float32 config.AutoOrbitJerk)
                    lastAngularChange <- System.DateTime.UtcNow
                    canJerk <- false
            for i = 0 to midsNotes.Length - 1 do
                if midsNotes.[i].mag > float32 config.MinimumMids then
                    curlAttractors.[i] <- Vector4(
                        toWorldSpace midsNotes.[i].freq,
                        defaultMass * (sqrt midsNotes.[i].mag) * float32 config.CurlAttractorStrength)
                else
                    curlAttractors.[i] <- Vector4()
            for i = 0 to highNotes.Length - 1 do
                if highNotes.[i].mag > float32 config.MinimumHigh then
                    blackHoles.[i] <- Vector4(
                        toWorldSpace highNotes.[i].freq,
                        defaultMass * (sqrt highNotes.[i].mag) * float32 config.BlackHoleStrength)
                else
                    blackHoles.[i] <- Vector4()

            previousBass.[previousBassIndex] <- bassArray
            previousBassIndex <- (previousBassIndex + 1) % previousBass.Length
    let onClose () =
        blackHoles <- Array.zeroCreate<Vector4> 6
        curlAttractors <- Array.zeroCreate<Vector4> 5
        whiteHoles <- Array.zeroCreate<Vector4> 2
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
            if this.WindowBorder = WindowBorder.Hidden then
                this.WindowState <- WindowState.Normal
                this.WindowBorder <- WindowBorder.Resizable
                this.ClientSize <- preFullscreenSize
            else
                preFullscreenSize <- this.ClientSize
                this.WindowBorder <- WindowBorder.Hidden
                this.WindowState <- WindowState.Fullscreen
        // Alternate perspectives
        | Key.P, (true, false, false, false), false ->
            camera.UsePerspective <- not camera.UsePerspective
            if not camera.UsePerspective then
                camera.Yaw <- 0.f
        // Toggle sphere
        | Key.X, _, false ->
            if sphere.Scale.X > 0.f then
                sphere.Scale <- Vector3.Zero
            else
                sphere.Scale <- defaultSphereScale
        // Reset states of objects
        | Key.F5, _, false ->
            particleVelocities <- Array.zeroCreate<float32> (particleCount * 4)
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVelocityArray)
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, particleVelocities.Length * sizeof<float32>, particleVelocities)
            particlePositions <- Array.copy fixedParticlePositions
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVBO)
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, particlePositions.Length * sizeof<float32>, particlePositions)
            sphereVelocity <- defaultSphereVelocity
            sphere.Position <- Vector3.Zero
            camera.Position <- Vector3(0.f, 0.f, 1.625f)
            camera.Yaw <- 0.f
            cubeRotation <- Quaternion(0.f, 0.f, 0.f, 1.f)
            cubeAngularVelocity <- Vector4(0.f, 1.f, 0.f, autoRotateSpeed)
            mouseScroll <- float32 config.CursorForceInitial
        // Toggle auto rotate
        | Key.Z, _, false ->
            match autoRotate with
            | Off -> autoRotate <- Classic
            | Classic -> autoRotate <- Full
            | Full -> autoRotate <- Off
        // Toggle responsive to audio-out
        | Key.R, _, false ->
            if audioResponsive then
                audioResponsive <- false
                if audioOutCapture.Capturing () then
                    audioOutCapture.StopCapturing ()
            else
                audioResponsive <- true
                audioOutCapture.Reset ()
        // Toggle Jello mode
        | Key.J, _, false -> fixParticles <- not fixParticles
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
            GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear)

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
                use managedImage = new Image<PixelFormats.Rgb24>(width, height)
                for j = 0 to height - 1 do
                    for i = 0 to width - 1 do
                        managedImage.[i, j] <-
                            PixelFormats.Rgb24(
                                rawImage.[bytesPerRow * (height - j - 1) + 3 * i],
                                rawImage.[bytesPerRow * (height - j - 1) + 3 * i + 1],
                                rawImage.[bytesPerRow * (height - j - 1) + 3 * i + 2])
                let file = System.IO.File.OpenWrite filePath
                managedImage.SaveAsPng file
                printfn "Screenshot saved to '%s'" filePath
                file.Close ()
            })
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
        mouseLastMove <- System.DateTime.UtcNow
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
        // Set default values
        GL.Enable EnableCap.Multisample
        GL.ClearColor (0.f, 0.f, 0.f, 1.f)
        GL.CullFace CullFaceMode.Back
        GL.Enable EnableCap.CullFace
        GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill)
        GL.DepthFunc DepthFunction.Lequal
        GL.Enable EnableCap.DepthTest
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)
        GL.Enable EnableCap.Blend
        GL.PointSize 1.f

        // Load particle system
        particleCompShader <- EzShader.CreateComputeShader "shaders/particle_comp.glsl"
        particleRenderShader <- EzShader.CreateShaderProgram "shaders/particle_vert.glsl" "shaders/particle_frag.glsl"
        particlePositions <- Array.copy fixedParticlePositions
        particleVBO <- GL.GenBuffer ()
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVBO)
        GL.BufferData(BufferTarget.ShaderStorageBuffer, particlePositions.Length * sizeof<float32>, particlePositions, BufferUsageHint.StreamDraw)
        particleVelocities <- Array.zeroCreate<float32> (particleCount * 4)
        particleVelocityArray <- GL.GenBuffer ()
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVelocityArray)
        GL.BufferData(BufferTarget.ShaderStorageBuffer, particleVelocities.Length * sizeof<float32>, particleVelocities, BufferUsageHint.StreamDraw)
        particleFixedPosArray <- GL.GenBuffer ()
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleFixedPosArray)
        GL.BufferData(BufferTarget.ShaderStorageBuffer, fixedParticlePositions.Length * sizeof<float32>, fixedParticlePositions, BufferUsageHint.StaticRead)
        particleRenderVAO <- GL.GenVertexArray ()
        GL.BindVertexArray particleRenderVAO
        GL.BindBuffer(BufferTarget.ArrayBuffer, particleVBO)
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0)
        GL.EnableVertexAttribArray 0
        GL.BindBuffer(BufferTarget.ArrayBuffer, particleVelocityArray)
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0)
        GL.EnableVertexAttribArray 1

        // Set shader values
        GL.UseProgram particleCompShader
        GL.Uniform1(GL.GetUniformLocation(particleCompShader, "springCoefficient"), float32 config.SpringCoefficient)
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, particleVBO)
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, particleVelocityArray)
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, particleFixedPosArray)

        base.OnLoad eventArgs
    override _.OnUnload eventArgs =
        (audioOutCapture :> System.IDisposable).Dispose ()
        base.OnUnload eventArgs
    override this.OnResize eventArgs =
        camera.SetProjection this.Aspect
        GL.Viewport (0, 0, this.Width, this.Height)
        base.OnResize eventArgs
    override this.OnRenderFrame eventArgs =
        let deltaTime = float32 eventArgs.Time
        GL.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

        if sphere.Scale.X > 0.f then
            // Update sphere
            sphereVelocity.Y <- sphereVelocity.Y - deltaTime
            let mutable pos = sphere.Position + deltaTime*sphereVelocity
            let bound = 1.f - float32 config.BouncingBallSize
            if abs pos.X > bound then
                sphereVelocity.X <- -sphereVelocity.X
                pos.X <- pos.X + deltaTime*sphereVelocity.X
            if abs pos.Y > bound then
                sphereVelocity.Y <- -sphereVelocity.Y
                pos.Y <- pos.Y + deltaTime*sphereVelocity.Y
            if abs pos.Z > bound then
                sphereVelocity.Z <- -sphereVelocity.Z
                pos.Z <- pos.Z + deltaTime*sphereVelocity.Z
            sphere.Position <- pos

        // Particle computation
        GL.UseProgram particleCompShader
        GL.Uniform1(GL.GetUniformLocation(particleCompShader, "deltaTime"), deltaTime)
        GL.Uniform1(GL.GetUniformLocation(particleCompShader, "perspective"), if camera.UsePerspective then 1u else 0u)
        GL.Uniform1(GL.GetUniformLocation(particleCompShader, "fixParticles"), if fixParticles then 1u else 0u)
        GL.Uniform4(
            GL.GetUniformLocation(particleCompShader, "attractors[0]"),
            if mouseLeftDown then
                let p =
                    if camera.UsePerspective then
                        (Vector4(camera.ToWorldSpace mouseX mouseY, 1.f) * Matrix4.CreateFromQuaternion (cubeRotation.Inverted())).Xyz
                    else
                        camera.ToWorldSpace mouseX mouseY
                Vector4(
                    p,
                    if mouseRightDown then
                        cursorForceInverseFactor * defaultMass * cursorForceScrollFactor**mouseScroll
                    else
                        -defaultMass * cursorForceScrollFactor**mouseScroll)
            else
                Vector4(0.f))
        // TODO: Use interpolated strings here when .NET 5 releases!
        for i = 0 to blackHoles.Length - 1 do
            GL.Uniform4(GL.GetUniformLocation(particleCompShader, sprintf "attractors[%i]" (i + 1)), blackHoles.[i])
        for i = 0 to whiteHoles.Length - 1 do
            GL.Uniform4(GL.GetUniformLocation(particleCompShader, sprintf "bigBoomers[%i]" i), whiteHoles.[i])
        for i = 0 to curlAttractors.Length - 1 do
            GL.Uniform4(GL.GetUniformLocation(particleCompShader, sprintf "curlAttractors[%i]" i), curlAttractors.[i])
        GL.Uniform4(
            GL.GetUniformLocation(particleCompShader, "musicalSphere"),
            Vector4(sphere.Position, sphere.Scale.X))
        GL.DispatchCompute(particleCount / 128, 1, 1)

        // Draw particles
        GL.BindVertexArray particleRenderVAO
        GL.UseProgram particleRenderShader
        let mutable projViewMutable =
            if autoRotate = Classic then
                camera.Yaw <- camera.Yaw + autoRotateSpeed * deltaTime
            elif autoRotate = Full then
                let w = cubeAngularVelocity.W
                let theta = w * deltaTime
                let r = Quaternion(sin theta * cubeAngularVelocity.Xyz, cos theta)
                cubeRotation <- (cubeRotation * r).Normalized()
                cubeAngularVelocity <- Vector4(cubeAngularVelocity.Xyz, w + (autoRotateSpeed - w) * (1.f - exp -deltaTime))
            let t = exp (-deltaTime / float32 config.CameraInertia)
            camera.ForwardVelocity <- camera.ForwardVelocity + 
                ((targetCameraVelocity.Z * if holdShift then float32 config.ShiftFactorMove else 1.f) - camera.ForwardVelocity) * (1.f - t)
            camera.StrafeRight <- camera.StrafeRight +
                ((targetCameraVelocity.X * if holdShift then float32 config.ShiftFactorOrbit else 1.f) - camera.StrafeRight) * (1.f - t)
            camera.Update deltaTime
            if camera.UsePerspective then
                Matrix4.CreateFromQuaternion cubeRotation * camera.ProjView ()
            else
                camera.ProjView ()
        GL.UniformMatrix4(GL.GetUniformLocation(particleRenderShader, "projViewMatrix"), true, &projViewMutable)
        GL.Uniform1(GL.GetUniformLocation(particleRenderShader, "perspective"), if camera.UsePerspective then 1u else 0u)
        GL.DrawArrays(PrimitiveType.Points, 0, particleCount)

        if sphere.Scale.X > 0.f then
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
        if audioResponsive && tick % audioDisconnectCheckRate = 0UL && audioOutCapture.Stopped () then
            audioOutCapture.StartCapturing ()

        if (System.DateTime.UtcNow.Subtract mouseLastMove).TotalSeconds > cursorWait then
            if not mouseHidden then
                world.Cursor <- MouseCursor.Empty
                mouseHidden <- true
        else
            if mouseHidden then
                world.Cursor <- MouseCursor.Default
                mouseHidden <- false

        base.OnRenderFrame eventArgs
    member this.Aspect =
        float32 this.Width / float32 this.Height

[<EntryPoint>]
let main args =
    // Allow passing args to disable hiding the console on launch (useful for debugging)
    if args.Length = 0 then
        console.Show false

    // Set OpenTK options
    let options = ToolkitOptions.Default
    options.Backend <- PlatformBackend.PreferNative
    use toolkit = Toolkit.Init options

    // Create screenshots directory (if non-existant)
    System.IO.Directory.CreateDirectory screenshotsDir |> ignore

    // Load JSON configuration
    let config =
        let defaultPath = "config.json"
        if  System.IO.File.Exists defaultPath then
            try
                ConfigFormat.Load defaultPath
            with
            | e ->
                console.Maximize ()
                printfn "ERRROR: could not parse '%s': %s" defaultPath e.Message
                printf "Press ENTER to use the exe-baked '%s' and continue anyway: " defaultPath
                System.Console.ReadLine () |> ignore
                GetDefaultConfig ()
        else
            GetDefaultConfig ()

    // Run
    try
        use game = new ColouredSugar(config)
        game.Run ()
    with
    | e ->
        console.Maximize ()
        printfn "ColouredSugar failed with the following error:\n %s" e.Message
        printf "Press ENTER to safely close..."
        System.Console.ReadLine () |> ignore
    0