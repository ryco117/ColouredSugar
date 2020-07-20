(*
This file is part of ColouredSugar

ColouredSugar is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

ColouredSugar is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with ColouredSugar. If not, see <https://www.gnu.org/licenses/>.
*)

module EzSound

open NAudio.Wave
open NAudio.Dsp

let DualRealToComplex (samples: float32[]) =
    Array.init
        (samples.Length / 2)
        (fun i ->
            let mutable z = new Complex()
            // Mix stereo to mono
            z.X <- (samples.[2 * i] + samples.[2 * i + 1]) / 2.f
            // Only left
            //z.X <- samples.[2 * i]
            // Only right
            //z.X <- samples.[2 * i + 1]
            z)

type AudioOutStreamer(onDataAvail, onClose) =
    let capture = new WasapiLoopbackCapture()
    do if capture.WaveFormat.Channels <> 2 then raise (new System.Exception())
    do if capture.WaveFormat.Encoding <> WaveFormatEncoding.IeeeFloat then raise (new System.Exception())
    let bytesPerSample = capture.WaveFormat.BitsPerSample / 8
    do capture.DataAvailable.Add (
        fun (eventArgs: WaveInEventArgs) ->
            if eventArgs.BytesRecorded < 1 then
                capture.StopRecording ()
                onClose ()
            else
                let samplesReal = Array.zeroCreate<float32> (eventArgs.BytesRecorded / bytesPerSample)
                System.Buffer.BlockCopy(eventArgs.Buffer, 0, samplesReal, 0, eventArgs.BytesRecorded)
                let complex = DualRealToComplex samplesReal
                let logSize =
                    let rec log2 acc = function
                    | 1 -> acc
                    | n -> log2 (acc+1) (n/2)
                    log2 0 complex.Length
                let finalForm = Array.sub complex 0 (1 <<< logSize)
                FastFourierTransform.FFT(true, logSize, finalForm)
                onDataAvail capture.WaveFormat.SampleRate finalForm)
    do capture.StartRecording ()
    interface System.IDisposable with
        member _.Dispose () =
            match capture.CaptureState with
            | NAudio.CoreAudioApi.CaptureState.Capturing
            | NAudio.CoreAudioApi.CaptureState.Starting ->
                capture.StopRecording()
            | _ -> ()
            capture.Dispose()
            onClose ()
    member _.RecordingState = capture.CaptureState
    member _.SamplingRate = capture.WaveFormat.SampleRate
    member _.Stopped () = capture.CaptureState =  NAudio.CoreAudioApi.CaptureState.Stopped
    member _.Capturing () = capture.CaptureState =  NAudio.CoreAudioApi.CaptureState.Capturing
    member _.StartCapturing () =
        match capture.CaptureState with
        | NAudio.CoreAudioApi.CaptureState.Stopped
        | NAudio.CoreAudioApi.CaptureState.Stopping ->
            capture.StartRecording ()
        | _ -> ()
    member _.StopCapturing () =
        match capture.CaptureState with
        | NAudio.CoreAudioApi.CaptureState.Starting
        | NAudio.CoreAudioApi.CaptureState.Capturing ->
            capture.StopRecording ()
        | _ -> ()