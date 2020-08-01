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
    let mutable position = new Vector3(0.f, 0.f, 0.975f)
    let mutable perspective = true
    let mutable pitch = 0.f
    let mutable yaw = 0.f
    let mutable strafeRight = 0.f
    let mutable forwardVelocity = 0.f
    let proj = Matrix4.CreatePerspectiveFieldOfView (float32 (Math.PI/2.), 16.f/9.f, 0.01f, 50.f)
    let projInv = proj.Inverted ()
    let orth = Matrix4.CreateOrthographic (2.f, 2.f, -1.f, 1.f)
    let orthInv = orth.Inverted ()
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
    member _.ForwardVelocity
        with get () = forwardVelocity
        and set u = forwardVelocity <- u
    member _.Update deltaTime =
        yaw <- yaw + rotateSensitivity*deltaTime*strafeRight
        position.Z <- position.Z - moveSensitivity*deltaTime*forwardVelocity
    member _.Perspective
        with get () = perspective
        and set p = perspective <- p
    member _.ProjView () =
        if perspective then
            Matrix4.CreateRotationY(-yaw) * Matrix4.CreateTranslation(-position) * proj
        else
            orth
    member _.ToWorldSpace x y =
        if perspective then
            let p =
                let v = projInv * Vector4(x, y, -0.99f, 0.02f)
                let r = v.Xyz / v.W
                Vector4(r + position, 0.f)
            (Matrix4.CreateRotationY(-yaw) * p).Xyz
        else
            (orthInv * Vector4(x, y, -1.f, 0.f)).Xyz