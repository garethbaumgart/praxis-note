/**
 * AudioWorklet processor that captures raw PCM samples from all input channels
 * and posts them to the main thread for transcription.
 *
 * Runs in the audio rendering thread — must stay lightweight.
 * Each process() call receives ~128 samples (one AudioWorklet quantum).
 */
class PcmProcessor extends AudioWorkletProcessor {
  process(inputs, _outputs, _parameters) {
    const input = inputs[0];
    if (input && input.length > 0 && input[0].length > 0) {
      // Clone channel data (Float32) and post to main thread.
      // We must clone because the backing buffers are reused by the audio engine.
      const channels = [];
      for (let ch = 0; ch < input.length; ch++) {
        channels.push(new Float32Array(input[ch]));
      }
      this.port.postMessage({ channels });
    }
    return true;
  }
}

registerProcessor('pcm-processor', PcmProcessor);
