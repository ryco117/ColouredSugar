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

open OpenTK

let private far = 20.f
let private near = 0.01f

type EzCamera(rotateSensitivity, moveSensitivity, aspect) =
    let mutable position = Vector3(0.f, 0.f, 1.625f)
    let mutable perspective = true
    let mutable pitch = 0.f
    let mutable yaw = 0.f
    let mutable strafeRight = 0.f
    let mutable forwardVelocity = 0.f
    let mutable proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.Pi/2.f, aspect, near, far)
    let mutable projInv = proj.Inverted ()
    let orth = Matrix4.Identity
    let orthInv = orth
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
    member _.UsePerspective
        with get () = perspective
        and set p = perspective <- p
    member _.SetProjection aspect =
        proj <- Matrix4.CreatePerspectiveFieldOfView (MathHelper.Pi/2.f, aspect, near, far)
        projInv <- proj.Inverted ()
    member _.ProjView () =
        if perspective then
            Matrix4.CreateRotationY(-yaw) * Matrix4.CreateTranslation(-position) * proj
        else
            orth
    member _.ToWorldSpace x y =
        if perspective then
            let p =
                let d = 1.f
                let v =  (Vector4(x * d, y * d, (far + near) / (far - near) * d - (2.f * far * near) / (far - near), d) * projInv).Xyz
                Vector4(v + position, 1.f)
            (p * Matrix4.CreateRotationY(yaw)).Xyz
        else
            (Vector4(x, y, -1.f, 0.f) * orthInv).Xyz