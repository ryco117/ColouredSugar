module EzObjects

open OpenTK
open OpenTK.Graphics.OpenGL

open GrowingArray
open EzShader

type Object3D(objectMesh: float32[]*uint32[], vertPath, fragPath) =
    let mutable indexLength = 0
    let mutable position = Vector3(0.f)
    let mutable rotation = Matrix4.Identity
    let mutable scale = Vector3(1.f)
    // Create VAO
    let VAO = GL.GenVertexArray ()
    do GL.BindVertexArray VAO
    // Create vertex and element buffer objects
    let VBO = GL.GenBuffer ()
    let EBO = GL.GenBuffer ()
    do match objectMesh with
        | vertices, indices ->
            // Fill VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO)
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof<float32>, vertices, BufferUsageHint.StaticDraw)
            // Fill EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO)
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof<uint32>, indices, BufferUsageHint.StaticDraw)
            indexLength <- indices.Length
    // Create shader program
    let shaderProgram = CreateShaderProgram vertPath fragPath
    do GL.UseProgram shaderProgram
    //Transformations
    member _.Position
        with get () = position
        and set pos = position <- pos
    member _.Scale
        with get () = scale
        and set scale' = scale <- scale'
    // Cleanup
    member this.Dispose () = (this :> System.IDisposable).Dispose ()
    interface System.IDisposable with
        member _.Dispose () =
            GL.BindVertexArray VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0)
            GL.DeleteBuffer VBO
            GL.DeleteBuffer EBO
            GL.BindVertexArray 0
            GL.DeleteVertexArray VAO
            GL.DeleteProgram shaderProgram
    member _.GetRenderShader () = shaderProgram
    member _.Draw projView =
        GL.UseProgram shaderProgram
        GL.BindVertexArray VAO
        let mutable (projViewModel: Matrix4) =
            rotation * Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(position) * projView
        GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "projViewModel"), false, &projViewModel)
        GL.DrawElements(PrimitiveType.Triangles, indexLength, DrawElementsType.UnsignedInt, 0)

let GenSphere n =
    let top = 0.f, 1.f, 0.f         // top 0
    let bot = 0.f, -1.f, 0.f        // bot 1
    let left = -1.f, 0.f, 0.f       // left 2
    let right = 1.f, 0.f, 0.f       // right 3
    let front = 0.f, 0.f, 1.f       // front 4
    let back = 0.f, 0.f, -1.f       // back 5
    let pointArray = new GrowingArray<float32*float32*float32> [|top; bot; left; right; front; back|]
    let rec apply n (triList: (int*int*int) list) =
        if n < 1 then
            triList 
        else
            let rec f (acc: (int*int*int) list) = function
            | [] -> acc
            | (p0, p1, p2)::tl ->
                let avg p0 p1 =
                    match (pointArray.At p0), (pointArray.At p1) with
                    | (x0, y0, z0), (x1, y1, z1) ->
                        let cx = (x0 + x1) / 2.f
                        let cy = (y0 + y1) / 2.f
                        let cz = (z0 + z1) / 2.f
                        let r = sqrt (cx**2.f + cy**2.f + cz**2.f)
                        let index = pointArray.Size ()
                        pointArray.Push (cx/r, cy/r, cz/r)
                        index
                let c0 = avg p0 p1
                let c1 = avg p1 p2
                let c2 = avg p2 p0
                f ([(p0, c0, c2); (c0, c1, c2); (p1, c1, c0); (p2, c2, c1)] @ acc) tl
            apply (n - 1) (f [] triList)
    let triIndexArray =
        apply n [(0, 4, 3); (0, 3, 5); (0, 5, 2); (0, 2, 4); 
        (1, 4, 2); (1, 2, 5); (1, 5, 3); (1, 3, 4)]
        |> List.toArray
    let vertices =
        let a = Array.zeroCreate (3 * pointArray.Size ())
        for i = 0 to (pointArray.Size ()) - 1 do
            match pointArray.At i with
            | (x, y, z) ->
                a.[3 * i] <- x
                a.[3 * i + 1] <- y
                a.[3 * i + 2] <- z
        a
    let indices =
        let a = Array.zeroCreate (3 * triIndexArray.Length)
        for i = 0 to triIndexArray.Length - 1 do
            match triIndexArray.[i] with
            | (p0, p1, p2) ->
                a.[3 * i] <- uint32 p0
                a.[3 * i + 1] <- uint32 p1
                a.[3 * i + 2] <- uint32 p2
        a
    vertices, indices

type ColouredSphere(color: Vector3) as this =
    inherit Object3D(GenSphere 6, "solidcolor_vert.glsl", "solidcolor_frag.glsl")
    do GL.Uniform3(GL.GetUniformLocation(this.GetRenderShader (), "triColor"), color)
    do GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0)
    do GL.EnableVertexAttribArray 0