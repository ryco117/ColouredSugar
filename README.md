![ColouredSugar](res/ColouredSugar.ico)
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
`bass`, `mids`, and `highs`. For each of these sub domains, the discrete frequency bin with the largest magnitude is determined 
(ie. the tone most present in the sound wave is determined). 
Then a corresponding `whitehole`, `curl-attractor`, or `blackhole` is created with parameters determined by the magnitude and frequency of the 
determined bin from the domain of `bass`, `mids`, and `highs` respectively. The `whitehole`s apply a force outward from their location, 
`curl-attractor`s apply a force determined by the cross-product of their positions relative to the origin and to the attractor, 
`blackhole`s apply a force towards their location following an inverse square law.

### Easy Installation (Precompiled)
- Go to the [releases page](https://github.com/ryco117/ColouredSugar/releases) and determine the latest release/version
- Download the compressed archive containing the **precompiled** executable
- Extract the folder within
- Run the application `ColouredSugar.exe`

### Example Videos (Click for links)
[![Coloured Sugar Demo 1](https://img.youtube.com/vi/p_YMFBeia4w/0.jpg)](https://www.youtube.com/watch?v=p_YMFBeia4w "Coloured Sugar Demo 1")
[![Coloured Sugar Demo 2](https://img.youtube.com/vi/tWfhWh-3q38/0.jpg)](https://www.youtube.com/watch?v=tWfhWh-3q38 "Coloured Sugar Demo 2")

### Controls
| Button | Description |
| ------ | ----------- |
| `F1`   | Toggle help overlay |
| `ESC`  | Exit the application |
| `ALT+F4`  | Exit the application (alternative to `ESC`) |
| `F5`  | Reset the visualizer |
| `F11`  | Toggle fullscreen (default: `false`) |
| `ALT+ENTER`  | Toggle fullscreen (alternative to `F11`) |
| `F12` | Save a screenshot to `screenshots/awesome.png` at current window resolution. If a file with that name already exists it will increment a counter until it finds an available file name |
| `ALT+P`  | Toggle between `3D` and `2D` perspectives (default: `3D`) |
| `W,A,S,D` | Manually direct the camera (Note: only affects `3D` perspective) |
| `LEFT-SHIFT` | Hold to reduce manual movement speed of the camera |
| `Z` | Toggle auto-rotation of camera (Note: only affects `3D` perspective; default: `true`) |
| `X` | Toggle presence of bouncing ball (default: `false`) |
| `R` | Toggle audio-output responsiveness (Note: framerate may jitter if there is no system audio-out present while set to `true`; default: `true`) |
| `MOUSE-LEFT` | Hold to manually apply a point force attraction/repulsion at the mouse cursor |
| `MOUSE-RIGHT` | Hold to inverse repulsion force to attraction at the mouse cursor. (Note: must hold both `MOUSE-RIGHT` and `MOUSE-LEFT` to apply an inverted force) |
| `MOUSE-SCROLL` | Scroll up to increase intensity of manual point force. Scroll down to decrease strength |
| `~` | Toggle visibility of the debug console (Note: console is output only) |

### Beyond Controls
*(Note: it is recommended to make a backup of any file before modifying it)*

ColouredSugar can be highly customized by an end user, without the need for re-compiling, 
through several methods. The simplest way is to edit the values in the [configuration file](config.json), 
which are loaded each time the application starts. These constants define various features of the visualizer, 
but most notably the rate, speed, and strength of various operations. 

For an even more customized experience, one must edit the shader programs. The shader files of most interest are 
[particle_comp.glsl](shaders/particle_comp.glsl) and [particle_vert.glsl](shaders/particle_vert.glsl). 
These files are compiled on the GPU at runtime and respectively dictate the behaviour and colouring of the particles. 
While some programming literacy may be required to fully edit these files, they provide a powerful mechanism to 
customize one's experience.