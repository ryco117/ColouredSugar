![ColouredSugar](res/ColouredSugar.ico)
# ColouredSugar

### About the project
An experimental audio visualizer written in F# and using the library [OpenTK](https://github.com/opentk/opentk) as a wrapper for the OpenGL API.
The particles are updated each frame using OpenGL Compute Shaders for parallel work on the GPU.
I chose the open source library [NAudio](https://github.com/naudio/NAudio) for handling the audio streams, despite the fact that OpenTK comes with a wrapper for OpenAL, 
because I could not determine a way to stream the current audio out as a capture in OpenAL.
NAudio also provides the Fast Fourier Transform I used.

### Visualizer Technical Description
ColouredSugar uses the [WASAPI loopback](https://docs.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording) 
feature to capture the current audio out stream. The audio is then read in chunks as they become available 
and a fast fourier transform is applied to the sampled audio stream. The resulting frequency domain is partitioned into three unequal parts, 
`bass`, `mids`, and `highs`. For each of these sub domains, the discrete frequency bins with the largest magnitudes are determined 
(ie. the tones most present in the sound wave are determined). 
Then corresponding `whitehole`, `curl-attractor`, or `blackhole` point forces are created with parameters determined by the magnitude and frequency of the 
determined bin from the domain of `bass`, `mids`, and `highs` respectively. The `whitehole`s apply a force outward from their location, 
`curl-attractor`s apply a force determined by the cross-product of their positions relative to the origin and to the attractor, 
`blackhole`s apply a force towards their location following an inverse square law.

### Easy Installation (Windows)
- Go to the [releases page](https://github.com/ryco117/ColouredSugar/releases) and determine the latest release/version
- Download the `ColouredSugarInstaller-Win64.msi` installer file
- Run the installer (Note: Windows may complain that the installer is not certified/known. Simply pess `More info` and `Run anyway` to continue the installation)
- Read and agree to the copyleft license agreement to begin the installation
- By default, after the installation finishes ColouredSugar will be launched. But this can be disabled at the last screen of the install
- ColouredSugar is now installed alongside your other 64-bit program files and shortcuts were created on the `Desktop` and `Start Menu`

### Example Videos (Click for links)
##### Death Cab for Cutie
[![Death Cab for Cutie Demo](https://img.youtube.com/vi/p_YMFBeia4w/0.jpg)](https://www.youtube.com/watch?v=p_YMFBeia4w "Death Cab for Cutie Demo")
##### Madeon Demo
[![Madeon Demo](https://img.youtube.com/vi/f5pirPzv0Yk/0.jpg)](https://www.youtube.com/watch?v=f5pirPzv0Yk&t=2 "Madeon Demo")
##### Santana Demo
[![Santana Demo](https://img.youtube.com/vi/yuSlCbhd97U/0.jpg)](https://www.youtube.com/watch?v=yuSlCbhd97U&t=4 "Santana Demo")
##### L.Dre Demo
[![L.Dre Demo](https://img.youtube.com/vi/FVB3HRSrhi8/0.jpg)](https://www.youtube.com/watch?v=FVB3HRSrhi8&t=2 "L.Dre Demo")

### Controls
| Button | Description |
| ------ | ----------- |
| `F1`   | Toggle controls overlay |
| `ESC`  | Exit the application |
| `F5`  | Reset the visualizer |
| `F11`  | Toggle fullscreen (default: `false`) |
| `F12` | Save a screenshot to the default `Pictures` library path |
| `ALT+P`  | Toggle between `3D` and `2D` perspectives (default: `3D`) |
| `W,A,S,D` | Manually direct the camera (Note: only affects `3D` perspective) |
| `LEFT-SHIFT` | Hold to reduce manual movement speed of the camera |
| `Z` | Cycle camera auto-rotation mode between `Off`, `Classic`, and `Full` (Note: only affects `3D` perspective; default: `Full`) |
| `X` | Toggle presence of bouncing ball (default: `false`) |
| `R` | Toggle audio-output responsiveness (Note: framerate may jitter if there is no system audio-out present while set to `true`; default: `true`) |
| `J` | Toggle fixing particles to their start position with springs, ie. `Jello mode` (default: `true`) |
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
| fullscreenOnLaunch | false | | Toggles whether fullscreen mode is applied on launch |
| showHelpOnLaunch | true | | Toggles whether to show help overlay on launch |
| enableVSync | true | | Toggles whether to enable application level VSync |
| screenshotScale | 1.0001 | Scale at which to save screenshots. If this value were 2.0 then screenshots would save at 2X current window width and height |
| cursorForceScrollIncrease | 1.5 | Factor to multiply cursor strength by on user `MOUSE-SCROLL` up |
| cursorForceInverseFactor | 1.5 | Factor to multiple cursor strength by on inverse (ie. strength of pull relative to push. if this number is negative then both forces push) |
| cursorForceInitial | 7.5 | Starting strength of the cursor's point-force. |
| cursorHideAfterSeconds | 0.5 | Amount of seconds of cursor inactivity to wait before hiding the cursor. Cursor appears upon moving again |
| bouncingBallSize | 0.1 | Radius of the bouncing ball. Note that the encompassing cube is 2 units in length |
| bouncingBallVelocity | {x: -0.4, y: 0.4, z: -0.3} | Describes the starting velocity of the bouncing ball |
| particleCount | 1572864 | Number of coloured particles in the visualizer |
| bassStartFreq | 30.01 | Lowest frequency (in Hz) considered a bass note |
| bassEndFreq | 250.01 | Highest frequency (in Hz) considered a bass note |
| midsStartFreq | 250.01 | Lowest frequency (in Hz) considered a mids note |
| midsEndFreq | 3000.01 | Highest frequency (in Hz) considered a mids note |
| highStartFreq | 3000.01 | Lowest frequency (in Hz) considered a high note |
| highEndFreq | 15000.01 | Highest frequency (in Hz) considered a high note |
| minimumBass | 0.0075 | Minimum magnitude of strongest bass note required to register a whitehole response |
| minimumMids | 0.001 | Minimum magnitude of strongest mids note required to register a curl attractor response |
| minimumHigh | 0.0005 | Minimum magnitude of strongest bass note required to register a blackhole response |
| whiteHoleStrength | 27.5 | Factor to adjust strength of whiteholes |
| curlAttractorStrength | 17.15 | Factor to adjust strength of curl attractors |
| blackHoleStrength | 14.7 | Factor to adjust strength of blackholes |
| cameraOrbitSpeed | 0.75 | Factor to adjust speed of camera rotations/orbits |
| cameraMoveSpeed | 0.75 | Factor to adjust speed of camera movement |
| autoOrbitSpeed | 0.05 | Main rotation speed of camera auto-rotation |
| autoOrbitJerk | 0.1 | Factor to increase strength of camera jerks in `Full` camera auto-rotation mode |
| minimumBassForJerk | 0.05 | Minimum bass note magnitude to trigger camera jerk in `Full` camera auto-rotation mode |
| shiftFactorOrbit | 0.3 | Factor to multiply camera orbit speed by when holding `LEFT-SHIFT` |
| shiftFactorMove | 0.3 | Factor to multiply camera movement speed by when holding `LEFT-SHIFT` |
| cameraInertia | 0.4 | Camera's resistance to user inputs on movement |
| audioDisconnectCheckWait | 12 | Number of frames to wait before rechecking if audio has disconnected (only if audio-responsiveness is enabled) |
| springCoefficient | 100.01 | The spring coefficient used to fix particles to their start positions |

For an even more customized experience, one must edit the shader programs. The shader files of most interest are 
[particle_comp.glsl](shaders/particle_comp.glsl) and [particle_vert.glsl](shaders/particle_vert.glsl). 
These files are compiled on the GPU at runtime and respectively dictate the behaviour and colouring of the particles. 
While some programming literacy may be required to fully edit these files, they provide a powerful mechanism to 
customize one's experience.