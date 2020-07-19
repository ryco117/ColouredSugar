# ColouredSugar

### About
An experimental visualizer and graphics engine written in F# and using the library [OpenTK](https://github.com/opentk/opentk) as a wrapper for the OpenGL API.
The particles are updated each frame using OpenGL Compute Shaders for parallel work on the GPU.
I chose the open source library [NAudio](https://github.com/naudio/NAudio) for handling the audio streams, despite the fact that OpenTK comes with a wrapper for OpenAL, 
because I could not determine a way to stream the current audio out as a capture in OpenAL.
NAudio also provides the Fast Fourier Transform I used.

### Example Video (YouTube)
[![Coloured Sugar Demo](https://img.youtube.com/vi/pyM19LuC4Ns/0.jpg)](https://www.youtube.com/watch?v=pyM19LuC4Ns "Coloured Sugar Demo")