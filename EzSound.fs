module EzSound

open NAudio.Wave
open NAudio.Dsp

let DualRealToComplex (samples: float32[]) =
    Array.init
        (samples.Length / 2)
        (fun i ->
            let mutable z = new Complex()
            z.X <- samples.[2 * i] + samples.[2 * i + 1]
            z)

type AudioOutStreamer(onDataAvail) =
    let capture = new WasapiLoopbackCapture()
    do if capture.WaveFormat.Channels <> 2 then raise (new System.Exception())
    do if capture.WaveFormat.Encoding <> WaveFormatEncoding.IeeeFloat then raise (new System.Exception())
    let bytesPerSample = capture.WaveFormat.BitsPerSample / 8
    do capture.DataAvailable.Add (
        fun (eventArgs: WaveInEventArgs) ->
            if eventArgs.BytesRecorded = 0 then
                capture.StopRecording ()
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
                onDataAvail finalForm)
    do capture.StartRecording ()
    interface System.IDisposable with
        member _.Dispose () =
            match capture.CaptureState with
            | NAudio.CoreAudioApi.CaptureState.Capturing
            | NAudio.CoreAudioApi.CaptureState.Starting ->
                capture.StopRecording()
            | _ -> ()
            capture.Dispose()
    member _.RecordingState = capture.CaptureState
    member _.SamplingRate = capture.WaveFormat.SampleRate
    member _.Stopped () = capture.CaptureState =  NAudio.CoreAudioApi.CaptureState.Stopped
    member _.StartCapturing () =
        match capture.CaptureState with
        | NAudio.CoreAudioApi.CaptureState.Stopped ->
            capture.StartRecording ()
        | NAudio.CoreAudioApi.CaptureState.Stopping ->
            raise (new System.Exception())
        | _ -> ()