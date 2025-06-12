using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks.Common;

public enum BlockBreakingAnimationState
{
    None,
    ScaleUp,
    ScaleDown,
    Finished
}


public class BlockBreakingAnimation
{
    private const float AnimationDuration = 0.3f;
    private const float ScaleUpDuration = 0.15f;
    private const float MaxScale = 1.15f;

    public BlockBreakingAnimationState State { get; private set; } = BlockBreakingAnimationState.None;
    public float CurrentScale { get; private set; } = 1f;
    public bool IsAnimating =>
        State != BlockBreakingAnimationState.None && State != BlockBreakingAnimationState.Finished;

    private float _animationTimer;
    private bool _soundPlayed;

    public void StartAnimation()
    {
        State = BlockBreakingAnimationState.ScaleUp;
        _animationTimer = 0f;
        _soundPlayed = false;
        CurrentScale = 1f;
    }

    public void Update()
    {
        if (!IsAnimating) return;

        var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;
        _animationTimer += deltaTime;

        switch (State)
        {
            case BlockBreakingAnimationState.ScaleUp:
                // scale up phase
                var scaleUpProgress = Math.Min(_animationTimer / ScaleUpDuration, 1f);
                CurrentScale = MathHelper.Lerp(1f, MaxScale, EaseOutQuad(scaleUpProgress));

                if (scaleUpProgress >= 1f)
                {
                    State = BlockBreakingAnimationState.ScaleDown;
                    _animationTimer = 0f;
                }
                break;
            case BlockBreakingAnimationState.ScaleDown:
                // scale down phase
                var scaleDownDuration = AnimationDuration - ScaleUpDuration;
                var scaleDownProgress = Math.Min(_animationTimer / scaleDownDuration, 1f);
                CurrentScale = MathHelper.Lerp(MaxScale, 1f, EaseInQuad(scaleDownProgress));

                if (scaleDownProgress >= 1f)
                {
                    State = BlockBreakingAnimationState.Finished;
                    CurrentScale = 1f;
                    _animationTimer = 0f;
                }
                break;
        }
    }

    private static float EaseOutQuad(float value) => 1 - (1 - value) * (1 - value);
    private static float EaseInQuad(float value) => value * value;
}