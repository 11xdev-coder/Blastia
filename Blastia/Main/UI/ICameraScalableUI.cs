using Blastia.Main.GameState;
using Object = Blastia.Main.GameState.Object;

namespace Blastia.Main.UI;

public interface ICameraScalableUI
{
    public abstract void OnChangedPosition(Object camera);
    public abstract void OnChangedZoom(float newCameraScale);
}