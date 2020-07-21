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

open EzCamera

// Try to use same RNG source application-wide
let random = new System.Random ()
let randF () = float32 (random.NextDouble ())
let randNormF () = (randF () - 0.5f) * 2.f

[<EntryPoint>]
let main _ =
    let camera = new EzCamera()
    let screenshotScale = 2
    let mutable perspective = true
    let mutable tick = 0UL
    let fpsWait: uint64 = 300UL
    let mutable mouseX = 0.f
    let mutable mouseY = 0.f
    let mutable mouseScroll = 0.f
    let mutable mouseLeftDown = false
    let mutable mouseRightDown = false
    let mutable autoRotate = true
    let mutable audioResponsive = true

    // Particle System
    let particleCount = 1024*1024
    let mutable particleRenderVAO = 0
    let mutable particleVBO = 0
    let mutable particleVelocityArray = 0
    let mutable particleCompShader = 0
    let mutable particleRenderShader = 0
    let defaultMass = 0.005f
    let mutable blackHole = new Vector4()
    let mutable blackHoleMids = new Vector4()
    let mutable whiteHole = new Vector4()

    // Audio handler funciton
    let onDataAvail samplingRate (complex: NAudio.Dsp.Complex[]) =
        blackHole.W <- 0.f
        blackHoleMids.W <- 0.f
        whiteHole.W <- 0.f
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
            let detailedFreq i len =
                let flog =
                    if i < 2 then
                        log (1.1f)
                    else
                        log (float32 i)
                let ffrac = flog - float32 (int flog)
                floor flog / log (float32 len), ffrac
            let freqResolution = float samplingRate / float complex.Length
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
            let bassEnd = roundToInt (275. / freqResolution)
            let midsStart = roundToInt (275. / freqResolution)
            let midsEnd = roundToInt (3_500. / freqResolution)
            let highStart = roundToInt (3_500. / freqResolution)
            let highEnd = roundToInt (17_500. / freqResolution)
            let bassMaxI, bassMaxMag, bassSum, bassFreqLog, _ = analyze (Array.sub complex 0 bassEnd)
            let _, midsMaxMag, midsSum, midsFreqLog, midsFreqFrac = analyze (Array.sub complex midsStart (midsEnd - midsStart))
            let _, highMaxMag, highSum, highFreqLog, highFreqFrac = analyze (Array.sub complex highStart (highEnd - highStart))
            if (bassMaxMag * float32 complex.Length) > 0.33f then
                let X = 2.f * bassFreqLog - 1.f
                let Y = phase complex.[bassMaxI] / float32 System.Math.PI
                whiteHole.W <- -defaultMass * bassSum * 16.f
                whiteHole.Xyz <- camera.ToWorldSpace perspective X Y
            if (midsMaxMag * float32 complex.Length) > 0.1f then
                let X = 2.f * midsFreqLog - 1.f
                let Y = 2.f * midsFreqFrac - 1.f
                blackHoleMids.W <- defaultMass * midsSum * 10.f
                blackHoleMids.Xyz <- camera.ToWorldSpace perspective X Y
            if (highMaxMag * float32 complex.Length) > 0.025f then
                let X = 2.f * highFreqLog - 1.f
                let Y = 2.f * highFreqFrac - 1.f
                blackHole.W <- defaultMass * highSum * 8.25f
                blackHole.Xyz <- camera.ToWorldSpace perspective X Y
    let onClose () =
        blackHole.W <- 0.f
        whiteHole.W <- 0.f
    let audioOutCapture = new EzSound.AudioOutStreamer(onDataAvail, onClose)

    let deltaClock = new System.Diagnostics.Stopwatch ()
    let fpsClock = new System.Diagnostics.Stopwatch ()
    let game = {
        new GameWindow(
            1366, 768,
            new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8, 4),
            "Coloured Sugar", GameWindowFlags.Default) with
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
            // Alternate perspectives
            | Key.P, (true, false, false, false), false ->
                perspective <- not perspective
                if not perspective then
                    camera.Yaw <- 0.f
            // Kill particle velocity
            | Key.K, _, false ->
                let velocities = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 0.f else randNormF () * 0.01f)
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVelocityArray)
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, velocities.Length * sizeof<float32>, velocities)
                let positions = Array.init (particleCount * 4) (fun i -> if i % 4 = 3 then 1.f else randNormF ())
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particleVBO)
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, nativeint 0, positions.Length * sizeof<float32>, positions)
            // Movement keys
            | Key.A, _, false ->
                camera.StrafeRight <- camera.StrafeRight - 1.f
            | Key.D, _, false ->
                camera.StrafeRight <- camera.StrafeRight + 1.f
            | Key.W, _, false ->
                camera.StrafeUp <- camera.StrafeUp + 1.f
            | Key.S, _, false ->
                camera.StrafeUp <- camera.StrafeUp - 1.f
            // Toggle auto rotate
            | Key.Z, _, _ -> autoRotate <- not autoRotate
            // Toggle responsive to audio-out
            | Key.R, _, false ->
                if audioResponsive then
                    audioResponsive <- false
                    if audioOutCapture.Capturing () then
                        audioOutCapture.StopCapturing ()
                else
                    audioResponsive <- true
                    if audioOutCapture.Stopped () then
                        audioOutCapture.StartCapturing ()
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
                GL.GetUniformLocation(particleCompShader, "attractors[0]"),
                new Vector4(
                    camera.ToWorldSpace perspective mouseX mouseY,
                    if mouseLeftDown then
                        if mouseRightDown then
                            1.5f * defaultMass * 1.4f**mouseScroll
                        else
                            -defaultMass * 1.4f**mouseScroll
                    else
                        0.f))
            GL.Uniform4(GL.GetUniformLocation(particleCompShader, "attractors[1]"), whiteHole)
            GL.Uniform4(GL.GetUniformLocation(particleCompShader, "attractors[2]"), blackHole)
            GL.Uniform4(GL.GetUniformLocation(particleCompShader, "experimental"), blackHoleMids)
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, particleVBO)
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, particleVelocityArray)
            GL.DispatchCompute(particleCount / 128, 1, 1)
            GL.MemoryBarrier (MemoryBarrierFlags.ShaderStorageBarrierBit)

            GL.BindVertexArray particleRenderVAO
            GL.UseProgram particleRenderShader
            let mutable projViewMutable =
                if autoRotate then
                    camera.Yaw <- camera.Yaw - 0.08f * deltaTime
                camera.Update deltaTime
                camera.ProjView ()
            GL.UniformMatrix4(GL.GetUniformLocation(particleRenderShader, "projViewMatrix"), false, &projViewMutable)
            GL.Uniform1(GL.GetUniformLocation(particleRenderShader, "perspective"), if perspective then 1u else 0u)
            GL.DrawArrays(PrimitiveType.Points, 0, particleCount)

            this.Context.SwapBuffers()

            // Tick management and occasional operations
            tick <- tick + 1UL
            if tick % fpsWait = 0UL then
                printfn "FPS: %f" (float fpsWait / ((float fpsClock.ElapsedMilliseconds) / 1000.))
                fpsClock.Restart ()

                // Ccheck if audio out stopped
                if audioResponsive && audioOutCapture.Stopped () then
                    audioOutCapture.StartCapturing ()

            base.OnRenderFrame eventArgs
    }
    game.Run ()
    0