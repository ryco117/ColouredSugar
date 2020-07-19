module EzCamera

open System
open OpenTK

let up = new Vector3(0.f, 1.f, 0.f)

type EzCamera() =
    let rotateSensitivity = 0.06f
    let moveSensitivity = 1.0f
    let mutable position = Vector3(0.f, 0.f, 2.f)
    let mutable pitch = 0.f
    let mutable yaw = 0.f
    let mutable strafeRight = 1.f
    let mutable strafeUp = 0.f
    let proj = Matrix4.CreatePerspectiveFieldOfView (float32 (Math.PI/2.), 16.f/9.f, 0.05f, 1000.f)
    member _.Pitch
        with get() = pitch
        and set p = pitch <- p
    member _.Yaw
        with get() = yaw
        and set y = yaw <- y
    member _.Position
        with get() = position
        and set p = position <- p
    member _.StrafeRight
        with get() = strafeRight
        and set r = strafeRight <- r
    member _.StrafeUp
        with get() = strafeUp
        and set u = strafeUp <- u
    member _.Update deltaTime =
        yaw <- yaw - rotateSensitivity*deltaTime*strafeRight
        position.Z <- position.Z - moveSensitivity*deltaTime*strafeUp
    member _.ProjView () =
        Matrix4.CreateRotationY(yaw) * Matrix4.CreateTranslation(-position) * proj
    member _.ToWorldSpace x y =
        new Vector3(
            x * float32(System.Math.Cos (float yaw)),
            y,
            x * float32(System.Math.Sin(float yaw)))