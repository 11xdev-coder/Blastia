namespace Blastia.Main.Synthesizer;

public class EnvelopeGenerator
{
    public float AttackTime { get; set; } = 0.01f; // seconds
    public float DecayTime { get; set; } = 0.1f; // seconds
    public float SustainLevel { get; set; } = 0.7f; // 0-7
    public float ReleaseTime { get; set; } = 0.3f; // seconds

    public enum EnvelopeStage
    {
        Idle, Attack, Decay, Sustain, Release
    }
    
    private float _sampleRate;
    private float _currentLevel;
    private EnvelopeStage _currentStage = EnvelopeStage.Idle;
    private float _stageProgress;

    public EnvelopeGenerator(float sampleRate = 44100)
    {
        _sampleRate = sampleRate;
    }

    public void NoteOn()
    {
        _currentStage = EnvelopeStage.Attack;
        _stageProgress = 0;
    }

    public void NoteOff()
    {
        if (_currentStage != EnvelopeStage.Idle)
        {
            _currentStage = EnvelopeStage.Release;
            _stageProgress = 0;
        }
    }

    public float Process()
    {
        switch (_currentStage)
        {
            case EnvelopeStage.Attack:
                _stageProgress += 1f / (_sampleRate * AttackTime);
                _currentLevel = _stageProgress;
                if (_stageProgress >= 1f)
                {
                    _currentStage = EnvelopeStage.Decay;
                    _stageProgress = 0;
                }
                break;
            case EnvelopeStage.Decay:
                _stageProgress += 1f / (_sampleRate * DecayTime);
                _currentLevel = 1f - (_stageProgress * (1f - SustainLevel));
                if (_stageProgress >= 1f)
                {
                    _currentStage = EnvelopeStage.Sustain;
                    _currentLevel = SustainLevel;
                }
                break;
            case EnvelopeStage.Sustain:
                _currentLevel = SustainLevel;
                break;
            case EnvelopeStage.Release:
                _stageProgress += 1f / (_sampleRate * ReleaseTime);
                _currentLevel = SustainLevel * (1f - _stageProgress);
                if (_stageProgress >= 1f)
                {
                    _currentStage = EnvelopeStage.Idle;
                    _currentLevel = 0f;
                }
                break;
            
            default:
                _currentLevel = 0f;
                break;
        }
        
        return _currentLevel;
    }
}