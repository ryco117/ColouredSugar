# ColouredSugar

### About the project
An experimental audio visualizer and graphics engine written in F# and using the library [OpenTK](https://github.com/opentk/opentk) as a wrapper for the OpenGL API.
The particles are updated each frame using OpenGL Compute Shaders for parallel work on the GPU.
I chose the open source library [NAudio](https://github.com/naudio/NAudio) for handling the audio streams, despite the fact that OpenTK comes with a wrapper for OpenAL, 
because I could not determine a way to stream the current audio out as a capture in OpenAL.
NAudio also provides the Fast Fourier Transform I used.

### Visualizer Technical Description
ColouredSugar uses the [WASAPI loopback](https://docs.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording) 
feature to capture the current audio out stream. The audio is then read in chunks as they become available 
and a fast fourier transform is applied to the sampled audio stream. The resulting frequency domain is partitioned into three unequal parts, 
`bass`, `mids`, and `highs`. For each of these sub domains, the discrete frequency bin with the largest magnitude is determined \
(ie. the tone most present in the sound wave is determined). 
Then a corresponding `whitehole`, `curl-attractor`, or `blackhole` is created with parameters determined by the magnitude and frequency of the 
determined bin from the domain of `bass`, `mids`, and `highs` respectively. The `whitehole`s apply a force outward from their location, 
`curl-attractor`s apply a force determined by the cross-product of their positions relative to the origin and to the attractor, 
`blackhole`s apply a force towards their location following an inverse square law.

### Example Videos (Click for links)
[![Coloured Sugar Demo 1](https://img.youtube.com/vi/MySuzdh4YaA/0.jpg)](https://www.youtube.com/watch?v=MySuzdh4YaA "Coloured Sugar Demo 1")
[![Coloured Sugar Demo 2](https://img.youtube.com/vi/tWfhWh-3q38/0.jpg)](https://www.youtube.com/watch?v=tWfhWh-3q38 "Coloured Sugar Demo 2")

### Controls
| Button | Description |
| ------ | ----------- |
| `ESC`  | Exit the application |
| `F11`  | Toggle fullscreen (default: `false`) |
| `ALT-ENTER`  | Toggle fullscreen (alternative to `F11`) |
| `F12` | Save a screenshot to 'awesome.png' at 1.5X current window resolution. If a file with that name already exists it will increment a counter until it finds an available file name |
| `ALT-P`  | Toggle between `3D` and `2`D perspectives (default: `3D`) |
| `K`  | Kill particle velocities and reset positions (reset) |
| `W,A,S,D` | Manually rotate the cube (if in `3D` perspective) |
| `Z` | Toggle auto-rotation of cube (if in `3D` perspective, default: `true`) |
| `R` | Toggle audio-output responsiveness (Note: framerate may jitter if there is not system audio-out present while set to `true`, default: `true`) |
| `MOUSE-LEFT` | Hold to manually apply a point force attraction/repulsion at position guided by the mouse cursor |
| `MOUSE-RIGHT` | Hold to inverse repulsion force to attraction at position guided by the mouse cursor. (Note: must hold both `MOUSE-LEFT` and `MOUSE-RIGHT` to apply and inverse the force respectively) |
| `MOUSE-SCROLL` | Scroll up to increase intensity of manual point force. Scroll down to decrease strength |