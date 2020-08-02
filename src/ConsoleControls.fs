module ConsoleControls

open System.Runtime.InteropServices

[<DllImport("kernel32.dll")>]
extern nativeint GetConsoleWindow()

[<DllImport("user32.dll")>]
extern bool ShowWindow(nativeint hWnd, int nCmdShow)

[<DllImport("user32.dll")>]
extern bool IsWindowVisible(nativeint hWnd)

type Controller() =
    let SW_HIDE = 0
    let SW_SHOWMINIMIZED = 2
    let SW_MAXIMIZE = 3
    let SW_SHOW = 5
    let handle = GetConsoleWindow ()
    member _.Show show = ShowWindow(handle, if show then SW_SHOW else SW_HIDE) |> ignore
    member _.Visible () = IsWindowVisible(handle)
    member _.Maximize () = ShowWindow(handle, SW_MAXIMIZE) |> ignore