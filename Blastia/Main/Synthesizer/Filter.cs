namespace Blastia.Main.Synthesizer;

public enum FilterType
{
    LowPass,
    HighPass,
    BandPass,
    Notch
}


public class Filter
{
    public static string[] FilterTypes =
    [
        "LowPass", "HighPass", "BandPass", "Notch"
    ];
    
    private float _cutoff = 1000f; // cutoff freq
    public float Cutoff
    {
        get => _cutoff;
        set
        {
            _cutoff = Math.Clamp(value, 20f, 20000f);
            CalculateCoefficients();
        }
    }
    
    private float _resonance = 0.5f; // resonance 0-1
    public float Resonance
    {
        get => _resonance;
        set
        {
            _resonance = Math.Clamp(value, 0.01f, 0.99f);
            CalculateCoefficients();
        }
    }
    
    private FilterType _filterType = FilterType.LowPass;
    public FilterType Type
    {
        get => _filterType;
        set
        {
            _filterType = value;
            Reset();
            CalculateCoefficients();
        }
    }

    private float _sampleRate;
    private float _a1, _a2, _b0, _b1, _b2;
    private float _x1, _x2, _y1, _y2;

    public Filter(float sampleRate = 44100)
    {
        _sampleRate = sampleRate;
        CalculateCoefficients();
    }
    

    private void CalculateCoefficients()
    {
        float omega = 2 * (float)Math.PI * _cutoff / _sampleRate;
        float sinOmega = (float) Math.Sin(omega);
        float cosOmega = (float) Math.Cos(omega);
        
        // convert resonance (0 - 1) to Q (0.5 - 25)
        float q = 0.5f + _resonance * 24.5f;
        float alpha = sinOmega / (2 * q);

        float a0 = 1f;

        switch (_filterType)
        {
            // compute biquad coefficients
            case FilterType.LowPass:
                _b0 = (1 - cosOmega) / 2;
                _b1 = 1 - cosOmega;
                _b2 = _b0;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                a0 = 1 + alpha;
                break;
            
            case FilterType.HighPass:
                _b0 = (1 + cosOmega) / 2;
                _b1 = -(1 + cosOmega);
                _b2 = _b0;
                _a1 = -2f * cosOmega;
                _a2 = 1 - alpha;
                a0 = 1 + alpha;
                break;
            
            case FilterType.BandPass:
                _b0 = alpha;
                _b1 = 0f;
                _b2 = -alpha;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                a0 = 1 + alpha;
                break;
            
            case FilterType.Notch:
                _b0 = 1;
                _b1 = -2 * cosOmega;
                _b2 = 1;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                a0 = 1 + alpha;
                break;
        }
        
        // normalize
        _b0 /= a0;
        _b1 /= a0;
        _b2 /= a0;
        _a1 /= a0;
        _a2 /= a0;
    }
    

    public float Process(float input)
    {
        float output = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;

        if (float.IsNaN(output) || float.IsInfinity(output))
        {
            Reset();
            return input;
        }
        
        _x2 = _x1;
        _x1 = input;
        _y2 = _y1;
        _y1 = output;

        return output;
    }

    public void Reset()
    {
        _x1 = _x2 = _y1 = _y2 = 0;
    }
}