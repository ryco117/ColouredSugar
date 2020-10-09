module ColouredSugarConfig

open FSharp.Data

[<Literal>]
let sample = """
{
  "enableVSync": true,

  "screenshotScale": 1.0001,

  "cursorForceScrollIncrease": 1.4,
  "cursorForceInverseFactor": 1.5,
  "cursorForceInitial": 4.2,

  "bouncingBallSize": 0.125,
  "bouncingBallVelocity": {
    "x": -0.4,
    "y": 0.4,
    "z": -0.3
  },

  "particleCount": 1572864,

  "bassStartFreq": 50.01,
  "bassEndFreq": 250.01,
  "midsStartFreq": 250.01,
  "midsEndFreq": 2000.01,
  "highStartFreq": 2000.01,
  "highEndFreq": 15000.01,

  "minimumBass": 0.006,
  "minimumMids": 0.0005,
  "minimumHigh": 0.0004,

  "whiteHoleStrength": 27.5,
  "curlAttractorStrength": 17.25,
  "blackHoleStrength": 14.5,

  "cameraOrbitSpeed": 0.75,
  "cameraMoveSpeed": 0.75,
  "autoOrbitSpeed": 0.085,

  "shiftFactorOrbit": 0.3,
  "shiftFactorMove": 0.3,

  "cameraInertia": 0.4,

  "audioDisconnectCheckWait": 12
}
"""

type ConfigFormat = JsonProvider<sample>
type Config = JsonProvider<sample>.Root

let GetDefaultConfig () = ConfigFormat.Parse sample