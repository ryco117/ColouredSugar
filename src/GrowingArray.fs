module GrowingArray

type GrowingArray<'T>(arr: 'T[]) =
    let mutable size = arr.Length
    let mutable array = Array.zeroCreate (max (2 * size) 64)
    do Array.iteri (fun i x -> array.[i] <- x) arr
    member _.Push x =
        if size = array.Length then
            let newArr = Array.zeroCreate (2 * size)
            Array.iteri (fun i x -> newArr.[i] <- x) array
            array <- newArr
        array.[size] <- x
        size <- size + 1
    member _.At i =
        array.[i]
    member _.Size () = size
    member _.CopyArray () = Array.sub array 0 size