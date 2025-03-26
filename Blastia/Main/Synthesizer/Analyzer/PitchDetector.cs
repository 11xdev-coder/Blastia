using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Blastia.Main.Synthesizer.Analyzer;

public static class PitchDetector
{
    /// <summary>
    /// Detects pitch (in Hz) using a normalized autocorrelation method with windowing.
    /// </summary>
    /// <param name="samples">Mono audio samples.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Estimated pitch in Hz, or 0 if none detected.</returns>
    public static double DetectPitchAutoCorrelationNormalized(float[] samples, int sampleRate)
    {
        int size = samples.Length;

        // Apply Hann window to reduce spectral leakage.
        float[] windowed = new float[size];
        for (int i = 0; i < size; i++)
        {
            double multiplier = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (size - 1)));
            windowed[i] = samples[i] * (float)multiplier;
        }

        // Define lag limits based on desired frequency range (50 Hz to 1000 Hz).
        int minLag = sampleRate / 1000; // ~44 samples for 44100Hz -> ~1000 Hz max
        int maxLag = sampleRate / 50;   // ~882 samples for 44100Hz -> ~50 Hz min

        // Calculate total energy for normalization (lag 0)
        double energy = 0;
        for (int i = 0; i < size; i++)
        {
            energy += windowed[i] * windowed[i];
        }
        if (energy == 0) return 0;

        double bestCorrelation = double.MinValue;
        int bestLag = 0;

        // Loop over possible lags
        for (int lag = minLag; lag <= maxLag; lag++)
        {
            double correlation = 0;
            for (int i = 0; i < size - lag; i++)
            {
                correlation += windowed[i] * windowed[i + lag];
            }
            // Normalize correlation value
            double normCorrelation = correlation / energy;
            if (normCorrelation > bestCorrelation)
            {
                bestCorrelation = normCorrelation;
                bestLag = lag;
            }
        }

        if (bestLag > 0)
            return sampleRate / (double)bestLag;
        return 0;
    }

    /// <summary>
    /// Detects pitch (in Hz) using a basic cepstrum-based method.
    /// </summary>
    /// <param name="samples">Mono audio samples.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Estimated pitch in Hz, or 0 if none detected.</returns>
    public static double DetectPitchCepstrum(float[] samples, int sampleRate)
    {
        int size = samples.Length;

        // Apply a Hann window
        Complex[] fftBuffer = new Complex[size];
        for (int i = 0; i < size; i++)
        {
            double multiplier = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (size - 1)));
            fftBuffer[i] = new Complex(samples[i] * multiplier, 0);
        }

        // Compute FFT
        Fourier.Forward(fftBuffer, FourierOptions.Matlab);

        // Compute log magnitude of the FFT results
        double[] logMag = new double[size];
        for (int i = 0; i < size; i++)
        {
            // Adding a small constant avoids log(0)
            logMag[i] = Math.Log(fftBuffer[i].Magnitude + 1e-10);
        }

        // Compute the inverse FFT of the log magnitude (the real cepstrum)
        Complex[] cepstrum = new Complex[size];
        for (int i = 0; i < size; i++)
        {
            cepstrum[i] = new Complex(logMag[i], 0);
        }
        Fourier.Inverse(cepstrum, FourierOptions.Matlab);

        // Define search range for pitch period (in samples)
        int minTau = sampleRate / 1000; // corresponds to ~1000 Hz
        int maxTau = Math.Min(sampleRate / 50, size); // corresponds to ~50 Hz

        double maxVal = double.MinValue;
        int tauEstimate = 0;
        for (int tau = minTau; tau < maxTau; tau++)
        {
            double val = cepstrum[tau].Real;
            if (val > maxVal)
            {
                maxVal = val;
                tauEstimate = tau;
            }
        }

        if (tauEstimate > 0)
            return sampleRate / (double)tauEstimate;
        return 0;
    }

    /// <summary>
    /// Combines the autocorrelation and cepstrum estimates to return a consensus pitch.
    /// </summary>
    /// <param name="samples">Mono audio samples.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Estimated pitch in Hz, or 0 if none detected.</returns>
    public static double DetectPitchConsensus(float[] samples, int sampleRate)
    {
        double pitchAuto = DetectPitchAutoCorrelationNormalized(samples, sampleRate);
        double pitchCep = DetectPitchCepstrum(samples, sampleRate);

        // If both methods return a valid value, average them.
        if (pitchAuto > 0 && pitchCep > 0)
            return (pitchAuto + pitchCep) / 2;
        else if (pitchAuto > 0)
            return pitchAuto;
        else
            return pitchCep;
    }
}