module Program

open OpenTK
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL
open OpenTK.Input

open EzCamera

let random = new System.Random ()
let randF () = float32 (random.NextDouble ())
let randNormF () = (randF () - 0.5f) * 2.f
let fpsWait: uint64 = 100UL

[<EntryPoint>]
let main _ =
    let camera = new EzCamera()
    let mutable perspective = true
    let mutable wireframe = false
    let mutable tick = 0UL

    // Particle System
    let particleCount = 1024*1024
    let mutable particleRenderVAO = 0
    let mutable particleVBO = 0
    let mutable particleVelocityArray = 0
    let mutable particleCompShader = 0
    let mutable particleRenderShader = 0
    let mutable mouseX = 0.f
    let mutable mouseY = 0.f
    let defaultMass = -0.005f
    let mutable blackHoleMass = defaultMass
    let mutable mouseDown = false

    let rec audioOutCapture = new EzSound.AudioOutStreamer(fun complex ->
        if complex.Length > 0 then
            let mag (c: NAudio.Dsp.Complex) = sqrt(c.X*c.X + c.Y*c.Y)
            let phase (c: NAudio.Dsp.Complex) =
                match sign c.X, sign c.Y with
                | 0, 0 -> 0.f
                | 0, 1 -> float32 System.Math.PI / 2.f
                | 0, -1 -> float32 System.Math.PI / -2.f
                | 1, 0 -> 0.f
                | -1, 0 -> float32 System.Math.PI
                | 1, 1 -> atan (c.Y / c.X)
                | -1, 1 -> float32 System.Math.PI - atan (c.Y / abs c.X)
                | 1, -1 -> atan (c.Y / c.X)
                | -1, -1 -> (float32 -System.Math.PI) + atan (c.Y / c.X)
                | _ -> raise (new System.Exception())
            let freqResolution = audioOutCapture.SamplingRate / complex.Length

            let mutable max_mag = mag complex.[0]
            let mutable max_i = 0
            for i = 1 to complex.Length / 2 do
                let mag = mag complex.[i]
                if mag > max_mag then
                    max_mag <- mag
                    max_i <- i
            let c = complex.[max_i]
            let sumRange s e =
                let mutable r = 0.f
                for i = s to min e (complex.Length / 2) do r <- r + mag complex.[i]
                r
            let bassSum = sumRange 1 (150 / freqResolution)
            let highSum = sumRange (600 / freqResolution) (20_000 / freqResolution)
            if max_mag > 0.001f then
                if bassSum > 0.10f && bassSum > (highSum/1.5f) then
                    blackHoleMass <- defaultMass * bassSum * 5.f
                    mouseDown <- true
                else if highSum > 0.07f then
                    blackHoleMass <- -defaultMass * highSum * 9.f
                    mouseDown <- true
                else
                    mouseDown <- false
                //printfn "Max freq: %i, Max mag: %f, Max phase: %f" (max_i * freqResolution) max_mag 0.f
                //mouseX <- 2.f * (sqrt (sqrt max_mag))  - 1.f
                mouseX <- (min 2.f (log (float32 max_i) / log 2.f / 4.f)) - 1.f
                mouseY <- (phase c) / float32 System.Math.PI
    )

    let deltaClock = new System.Diagnostics.Stopwatch ()
    let fpsClock = new System.Diagnostics.Stopwatch ()
    let game = {
        new GameWindow(
            1366, 768,
            new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8, 4),
            //GraphicsMode.Default,
            "Hello World", GameWindowFlags.FixedWindow) with
        override this.OnKeyDown e =
            match e.Key, (e.Alt, e.Shift, e.Control, e.Command), e.IsRepeat with
            // Escape
            | Key.Escape, _, _ -> this.Exit ()
            // Fullscreen
            | Key.Enter, (true, false, false, false), false
            | Key.F11, (false, false, false, false), false ->
                if this.WindowState = WindowState.Fullscreen then
                    this.WindowState <- WindowState.Normal
                else
                    this.WindowState <- WindowState.Fullscreen
            // Toggle Wireframe
            | Key.T, _, _ ->
                wireframe <- not wireframe
                GL.PolygonMode(MaterialFace.FrontAndBack, if wireframe then PolygonMode.Line else PolygonMode.Fill)
            // Toggle alternative perspective
            | Key.P, (true, false, false, false), false ->
                perspective <- not perspective
                if not perspective then
                    camera.Yaw <- 0.f
            // Kill particle velocity
            | Key.K, _, false ->
                let velocities = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 0.f else randNormF () * 0.01f)
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVelocityArray)
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, velocities.Length * sizeof<float32>, velocities)
            // Movement keys
            | Key.A, _, false ->
                camera.StrafeRight <- camera.StrafeRight - 1.f
            | Key.D, _, false ->
                camera.StrafeRight <- camera.StrafeRight + 1.f
            | Key.W, _, false ->
                camera.StrafeUp <- camera.StrafeUp + 1.f
            | Key.S, _, false ->
                camera.StrafeUp <- camera.StrafeUp - 1.f
            // Default handling
            | _ -> base.OnKeyDown e
        override _.OnKeyUp e =
            match e.Key with
            // Movement keys
            | Key.A ->
                camera.StrafeRight <- camera.StrafeRight + 1.f
            | Key.D ->
                camera.StrafeRight <- camera.StrafeRight - 1.f
            | Key.W ->
                camera.StrafeUp <- camera.StrafeUp - 1.f
            | Key.S ->
                camera.StrafeUp <- camera.StrafeUp + 1.f
            | _ -> base.OnKeyUp e
        override this.OnMouseMove e =
            mouseX <- (float32 e.X / float32 this.Width) * 2.f - 1.f
            mouseY <- (float32 e.Y / float32 this.Height) * -2.f + 1.f
            base.OnMouseMove e
        override _.OnMouseWheel e =
            blackHoleMass <- blackHoleMass * 1.45f**e.DeltaPrecise
            base.OnMouseWheel e
        override _.OnMouseDown e =
            if e.Button = MouseButton.Left then
                mouseDown <- true
            if e.Button = MouseButton.Right then
                blackHoleMass <- -2.f * blackHoleMass
            base.OnMouseDown e
        override _.OnMouseUp e =
            if e.Button = MouseButton.Left then
                mouseDown <- false
            if e.Button = MouseButton.Right then
                blackHoleMass <- blackHoleMass / -2.f
            base.OnMouseUp e
        override _.OnLoad eventArgs =
            // Set default background
            GL.ClearColor (0.f, 0.f, 0.f, 1.f)
            GL.CullFace CullFaceMode.Back
            GL.DepthFunc DepthFunction.Less
            GL.Enable EnableCap.DepthTest
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)
            GL.Enable EnableCap.Blend

            // Load particle system
            particleCompShader <- EzShader.CreateComputeShader "particle_comp.glsl"
            particleRenderShader <- EzShader.CreateShaderProgram "particle_vert.glsl" "particle_frag.glsl"
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

            deltaClock.Start ()
            fpsClock.Start ()
            base.OnLoad eventArgs
        override _.OnUnload eventArgs =
            deltaClock.Stop ()
            fpsClock.Stop ()
            (audioOutCapture :> System.IDisposable).Dispose ()
            base.OnUnload eventArgs
        override this.OnResize eventArgs =
            GL.Viewport (0, 0, this.Width, this.Height)
            base.OnResize eventArgs
        override this.OnRenderFrame eventArgs =
            let deltaTime = float32 deltaClock.Elapsed.TotalSeconds
            deltaClock.Restart ()
            GL.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

            // Particle tests
            GL.UseProgram particleCompShader
            GL.Uniform1(GL.GetUniformLocation(particleCompShader, "deltaTime"), deltaTime)
            GL.Uniform1(GL.GetUniformLocation(particleCompShader, "perspective"), if perspective then 1u else 0u)
            GL.Uniform4(
                GL.GetUniformLocation(particleCompShader, "blackHole"),
                new Vector4(
                    camera.ToWorldSpace mouseX mouseY,
                    if mouseDown then blackHoleMass else 0.f))
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, particleVBO)
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, particleVelocityArray)
            GL.DispatchCompute(particleCount / 128, 1, 1)
            GL.MemoryBarrier (MemoryBarrierFlags.ShaderStorageBarrierBit)

            GL.BindVertexArray particleRenderVAO
            GL.UseProgram particleRenderShader
            let mutable test =
                camera.Update deltaTime
                camera.ProjView ()
            GL.UniformMatrix4(GL.GetUniformLocation(particleRenderShader, "projViewMatrix"), false, &test)
            GL.Uniform1(GL.GetUniformLocation(particleRenderShader, "perspective"), if perspective then 1u else 0u)
            GL.DrawArrays(PrimitiveType.Points, 0, particleCount)

            this.Context.SwapBuffers()

            // Tick management and occasional operations
            tick <- tick + 1UL
            if tick % fpsWait = 0UL then
                printfn "FPS: %f" (float fpsWait / ((float fpsClock.ElapsedMilliseconds) / 1000.))
                fpsClock.Restart ()

                // Occasionally check if audio out stopped
                if audioOutCapture.Stopped () then
                    audioOutCapture.StartCapturing ()

            base.OnRenderFrame eventArgs
    }
    game.Run ()
    0