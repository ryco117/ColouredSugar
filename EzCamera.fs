(*
This file is part of ColouredSugar

ColouredSugar is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

ColouredSugar is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with ColouredSugar. If not, see <https://www.gnu.org/licenses/>.
*)

module EzCamera

open System
open OpenTK

type EzCamera() =
    let rotateSensitivity = 1.f
    let moveSensitivity = 1.0f
    let mutable position = Vector3(0.f, 0.f, 0.95f)
    let mutable perspective = true
    let mutable pitch = 0.f
    let mutable yaw = 0.f
    let mutable strafeRight = 0.f
    let mutable strafeUp = 0.f
    let proj = Matrix4.CreatePerspectiveFieldOfView (float32 (Math.PI/2.), 16.f/9.f, 0.05f, 1000.f)
    member _.Pitch
        with get () = pitch
        and set p = pitch <- p
    member _.Yaw
        with get () = yaw
        and set y = yaw <- y
    member _.Position
        with get () = position
        and set p = position <- p
    member _.StrafeRight
        with get() = strafeRight
        and set r = strafeRight <- r
    member _.StrafeUp
        with get () = strafeUp
        and set u = strafeUp <- u
    member _.Update deltaTime =
        yaw <- yaw - rotateSensitivity*deltaTime*strafeRight
        position.Z <- position.Z - moveSensitivity*deltaTime*strafeUp
    member _.Perspective
        with get () = perspective
        and set p = perspective <- p
    member _.ProjView () =
        Matrix4.CreateRotationY(yaw) * Matrix4.CreateTranslation(-position) * proj
    member _.ToWorldSpace x y =
        if perspective then
            new Vector3(
                x * float32(System.Math.Cos (float yaw)),
                y,
                x * float32(System.Math.Sin(float yaw)))
        else
            new Vector3(x, y, 0.f)