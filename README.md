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

### Easy Installation (Windows)
- Go to the [releases page](https://github.com/ryco117/ColouredSugar/releases) and determine the latest release/version
- Download the `ColouredSugar-Installer-Win64.msi` installer file
- Run the installer (Note: Windows may complain that the installer is not certified/known. Simply pess `More info` and `Run anyway` to continue the installation)
- Read and agree to the copyleft license agreement to begin the installation
- By default, after the installation finishes ColouredSugar will be launched. But this can be disabled at the last screen of the install
- ColouredSugar is now installed alongside your other 64-bit program files and shortcuts were created on the `Desktop` and `Start Menu`

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

| Name | Default value | Description |
| ---- | ------------- | ----------- |
| enableVSync | true | | Whether to enable application level VSync |
| screenshotScale | 1.01 | Scale at which to save screenshots. If this value were 2.0 then screenshots would save at 2X current window width and height |
| cursorForceScrollIncrease | 1.4 | Factor to multiply cursor strength by on user `MOUSE-SCROLL` up |
| cursorForceInverseFactor | 1.5 | Factor to multiple cursor strength by on inverse (ie. strength of pull relative to push. if this number is negative then both forces push) |
| bouncingBallSize | 0.125 | Radius of the bouncing ball. Note that the encompassing cube is 2 units in length |
| bouncingBallVelocity | {x: -0.4, y: 0.4, z: -0.3} | Describes the starting velocity of the bouncing ball |
| particleCount | 1048576 | Number of coloured particles in the visualizer |
| bassStartFreq | 20.01 | Lowest frequency (in Hz) considered a bass note |
| bassEndFreq | 300.01 | Highest frequency (in Hz) considered a bass note |
| midsStartFreq | 300.01 | Lowest frequency (in Hz) considered a mids note |
| midsEndFreq | 2500.01 | Highest frequency (in Hz) considered a mids note |
| highStartFreq | 2500.01 | Lowest frequency (in Hz) considered a high note |
| highEndFreq | 16000.01 | Highest frequency (in Hz) considered a high note |
| minimumBass | 0.0175 | Minimum magnitude of strongest bass note required to register a whitehole response |
| minimumMids | 0.000125 | Minimum magnitude of strongest mids note required to register a curl attractor response |
| minimumHigh | 0.00009 | Minimum magnitude of strongest bass note required to register a blackhole response |
| whiteHoleStrength | 0.9 | Factor to adjust strength of whiteholes |
| curlAttractorStrength | 5.01 | Factor to adjust strength of curl attractors |
| blackHoleStrength | 4.75 | Factor to adjust strength of blackholes |
| cameraOrbitSpeed | 1.01 | Factor to adjust speed of camera rotations/orbits |
| cameraMoveSpeed | 1.01 | Factor to adjust speed of camera movement |
| autoOrbitSpeed | 0.05 | Factor to adjust speed of auto orbit |
| shiftFactorOrbit | 0.33 | Factor to multiply camera orbit speed by when holding `LEFT-SHIFT` |
| shiftFactorMove | 0.33 | Factor to multiply camera movement speed by when holding `LEFT-SHIFT` |
| cameraInertia | 0.3 | Camera's resistance to user inputs on movement |
| audioDisconnectCheckWait | 100 | Number of frames to wait before rechecking if audio has disconnected (only if audio-responsiveness is enabled) |

For an even more customized experience, one must edit the shader programs. The shader files of most interest are 
[particle_comp.glsl](shaders/particle_comp.glsl) and [particle_vert.glsl](shaders/particle_vert.glsl). 
These files are compiled on the GPU at runtime and respectively dictate the behaviour and colouring of the particles. 
While some programming literacy may be required to fully edit these files, they provide a powerful mechanism to 
customize one's experience.