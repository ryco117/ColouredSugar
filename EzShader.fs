module EzShader

open OpenTK.Graphics.OpenGL

let CreateShaderProgram vertPath fragPath =
    // Read shaders from file-path
    let readSrc (path: string) =
        let r = new System.IO.StreamReader(path, System.Text.Encoding.UTF8)
        r.ReadToEnd ()
    let vertSrc = readSrc vertPath
    let fragSrc = readSrc fragPath
    // Move src to GPU
    let vertHndl = GL.CreateShader ShaderType.VertexShader
    GL.ShaderSource(vertHndl, vertSrc)
    let fragHndl = GL.CreateShader ShaderType.FragmentShader
    GL.ShaderSource(fragHndl, fragSrc)
    // Compile shaders
    let checkErr hndl =
        let infoLog = GL.GetShaderInfoLog hndl
        if infoLog.Length <> 0 then System.Console.WriteLine infoLog
    GL.CompileShader vertHndl
    checkErr vertHndl
    GL.CompileShader fragHndl
    checkErr fragHndl
    // Link shaders into a shader program
    let handle = GL.CreateProgram ()
    GL.AttachShader(handle, vertHndl)
    GL.AttachShader(handle, fragHndl)
    GL.LinkProgram handle
    // Cleanup old unlink copies
    GL.DetachShader(handle, vertHndl)
    GL.DetachShader(handle, fragHndl)
    GL.DeleteShader vertHndl
    GL.DeleteShader fragHndl
    // Return
    handle

let CreateComputeShader (compPath: string) =
    // Read shaders from file-path
    let compSrc =
        let r = new System.IO.StreamReader(compPath, System.Text.Encoding.UTF8)
        r.ReadToEnd ()
    // Move src to GPU
    let compHndl = GL.CreateShader ShaderType.ComputeShader
    GL.ShaderSource(compHndl, compSrc)
    // Compile shaders
    let checkErr hndl =
        let infoLog = GL.GetShaderInfoLog hndl
        if infoLog.Length <> 0 then System.Console.WriteLine infoLog
    GL.CompileShader compHndl
    checkErr compHndl
    // Link shaders into a shader program
    let handle = GL.CreateProgram ()
    GL.AttachShader(handle, compHndl)
    GL.LinkProgram handle
    // Cleanup old unlink copies
    GL.DetachShader(handle, compHndl)
    GL.DeleteShader compHndl
    // Return
    handle