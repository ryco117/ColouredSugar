module ColouredSugarConfig

open FSharp.Data

[<Literal>]
let sample = """
{
  "enableVSync": true,

  "screenshotScale": 1.01,

  "cursorForceScrollIncrease": 1.4,
  "cursorForceInverseFactor": 1.5,

  "bouncingBallSize": 0.125,
  "bouncingBallVelocity": {
    "x": -0.4,
    "y": 0.4,
    "z": -0.3
  },

  "particleCount": 1048576,

  "bassStartFreq": 20.01,
  "bassEndFreq": 300.01,
  "midsStartFreq": 300.01,
  "midsEndFreq": 2500.01,
  "highStartFreq": 2500.01,
  "highEndFreq": 16000.01,

  "minimumBass": 0.0175,
  "minimumMids": 0.000125,
  "minimumHigh": 0.00009,

  "whiteHoleStrength": 1.15,
  "curlAttractorStrength": 5.01,
  "blackHoleStrength": 4.75,

  "cameraOrbitSpeed": 1.01,
  "cameraMoveSpeed": 1.01,
  "autoOrbitSpeed": 0.075,

  "shiftFactorOrbit": 0.33,
  "shiftFactorMove": 0.33,

  "cameraInertia": 0.3,

  "audioDisconnectCheckWait": 100
}
"""

type ConfigFormat = JsonProvider<sample>
type Config = JsonProvider<sample>.Root

let GetDefaultConfig () = ConfigFormat.Parse sample