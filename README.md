# ColouredSugar

### About
An experimental visualizer and graphics engine written in F# and using the library [OpenTK](https://github.com/opentk/opentk) as a wrapper for the OpenGL API.
The particles are updated each frame using OpenGL Compute Shaders for parallel work on the GPU.
I chose the open source library [NAudio](https://github.com/naudio/NAudio) for handling the audio streams, despite the fact that OpenTK comes with a wrapper for OpenAL, 
because I could not determine a way to stream the current audio out as a capture in OpenAL.
NAudio also provides the Fast Fourier Transform I used.

### Example Video (YouTube)
[![Coloured Sugar Demo](https://img.youtube.com/vi/MySuzdh4YaA/0.jpg)](https://www.youtube.com/watch?v=MySuzdh4YaA "Coloured Sugar Demo")

### Controls
| Button | Description |
| ------ | ----------- |
| `ESC`  | Exit the application |
| `ALT-ENTER`  | Toggle fullscreen |
| `F11`  | Toggle fullscreen (default: false) |
| `ALT-P`  | Toggle between 3D and 2D perspectives (default: 3D) |
| `K`  | Kill particle velocities and reset positions (reset) |
| `W,A,S,D` | Manually rotate the cube (if in 3D perspective) |
| `Z` | Toggle auto-rotation of cube (if in 3D perspective, default: true) |
| `R` | Toggle audio-output responsiveness (Note: framerate may jitter if there is not system audio-out present while set to true, default: true) |
| `MOUSE-LEFT` | Hold to manually apply a point force attraction/repulsion at position guided by the mouse cursor |
| `MOUSE-RIGHT` | Hold to inverse repulsion force to attraction at position guided by the mouse cursor. (Note: must hold both `MOUSE-LEFT` and `MOUSE-RIGHT` to apply and inverse the force respectively) |
| `MOUSE-SCROLL` | Scroll up to increase intensity of manual point force. Scroll down to decrease strength |
| `F12` | Save a screenshot to 'awesome.png'. If a file with that name already exists it will increment a counter until it finds a free file name |