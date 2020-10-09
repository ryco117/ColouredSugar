module CubeFillingCurve

type private Vertex =
| Vertex0
| Vertex1
| Vertex2
| Vertex3
| Vertex4
| Vertex5
| Vertex6
| Vertex7

let private v0 = (1., 1., -1.)
let private v1 = (1., -1., -1.)
let private v2 = (-1., -1., -1.)
let private v3 = (-1., 1., -1.)
let private v4 = (-1., 1., 1.)
let private v5 = (-1., -1., 1.)
let private v6 = (1., -1., 1.)
let private v7 = (1., 1., 1.)

let private nearestVertex x =
    if x < 0.5 then
        if x < 0.25 then
            if x < 0.125 then
                Vertex0, x * 8.
            else
                Vertex1, (x - 0.125) * 8.
        else
            if x < 0.375 then
                Vertex2, (x - 0.25) * 8.
            else
                Vertex3, (x - 0.375) * 8.
    else
        if x < 0.75 then
            if x < 0.625 then
                Vertex4, (x - 0.5) * 8.
            else
                Vertex5, (x - 0.625) * 8.
        else
            if x < 0.875 then
                Vertex6, (x - 0.75) * 8.
            else
                Vertex7, (x - 0.875) * 8.

let private addPoints (x:float, y:float, z:float) (x':float, y':float, z':float) =
    (x + x', y + y', z + z')

let private vertexPos x = function
| Vertex0 -> addPoints v0 (0., -x, 0.)
| Vertex1 -> addPoints v1 (if x < 0.5 then (0., 1. - (x * 2.), 0.) else (-2. * (x - 0.5), 0., 0.))
| Vertex2 -> addPoints v2 (if x < 0.5 then (1. - (x * 2.), 0., 0.) else (0., 2. * (x - 0.5), 0.))
| Vertex3 -> addPoints v3 (if x < 0.5 then (0., (x * 2.) - 1., 0.) else (0., 0., 2. * (x - 0.5)))
| Vertex4 -> addPoints v4 (if x < 0.5 then (0., 0., (x * 2.) - 1.) else (0., -2. * (x - 0.5), 0.))
| Vertex5 -> addPoints v5 (if x < 0.5 then (0., 1. - (x * 2.), 0.) else (2. * (x - 0.5), 0., 0.))
| Vertex6 -> addPoints v6 (if x < 0.5 then ((x * 2.) - 1., 0., 0.) else (0., 2. * (x - 0.5), 0.))
| Vertex7 -> addPoints v7 (0., x - 1., 0.)

let private scale = function
| x, y, z -> x/2., y/2., z/2.

let private r0 = function
| x, y, z -> y, -z, -x
let private r1 = function
| x, y, z -> -z, x, -y
let private r2 = r1
let private r3 = function
| x, y, z -> -x, -y, z
let private r4 = r3
let private r5 = function
| x, y, z -> z, x, y
let private r6 = r5
let private r7 = function
| x, y, z -> y, z, x

let private cellTransform p = function
| Vertex0 -> addPoints (0.5, 0.5, -0.5) (scale (r0 p))
| Vertex1 -> addPoints (0.5, -0.5, -0.5) (scale (r1 p))
| Vertex2 -> addPoints (-0.5, -0.5, -0.5) (scale (r2 p))
| Vertex3 -> addPoints (-0.5, 0.5, -0.5) (scale (r3 p))
| Vertex4 -> addPoints (-0.5, 0.5, 0.5) (scale (r4 p))
| Vertex5 -> addPoints (-0.5, -0.5, 0.5) (scale (r5 p))
| Vertex6 -> addPoints (0.5, -0.5, 0.5) (scale (r6 p))
| Vertex7 -> addPoints (0.5, 0.5, 0.5) (scale (r7 p))

let curveToCubeN n x =
    let rec f n x =
        let v, x' = nearestVertex x
        if n = 0 then
            vertexPos x' v
        else
            let p' = f (n-1) x'
            cellTransform p' v
    match f n x with
    | x, y, z -> OpenTK.Vector3(float32 x, float32 y, float32 z)

let curveToCube = curveToCubeN 4