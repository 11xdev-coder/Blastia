using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Object = Blastia.Main.GameState.Object;

namespace Blastia.Main.UI;

/// <summary>
/// Interface for UI elements that stay on the same world position and scale with camera
/// </summary>
public interface IWorldPositionUi
{
    protected float CachedCameraScale { get; set; }
    protected Vector2 WorldPosition { get; set; }
    protected Vector2 ScreenPosition { get; set; }
    
    /// <summary>
    /// Updates screen position (<see cref="UpdateScreenPosition"/>) and caches <c>CameraScale</c>. 
    /// </summary>
    /// <param name="camera"></param>
    public void SetPositionBase(Camera camera)
    {
        CachedCameraScale = camera.CameraScale;
        UpdateScreenPosition(camera);
    }

    /// <summary>
    /// Should be called instead of setting <c>Position</c> directly. You can adjust the <c>worldPosition</c> here
    /// and then call the base method (<see cref="SetPositionBase"/>)
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="camera"></param>
    public void SetPosition(Vector2 worldPosition, Camera camera);
    
    /// <summary>
    /// Event that should be subscribed to another object's <c>OnPositionChanged</c>. Called when that object's position
    /// has changed.
    /// </summary>
    /// <param name="cameraObj">Object that changed its position</param>
    public void OnChangedPosition(Object cameraObj)
    {
        if (cameraObj is Camera camera)
        {
            UpdateScreenPosition(camera);
        }
    }

    /// <summary>
    /// Called from <c>OnChangedPosition</c> to update UI element's position on screen
    /// </summary>
    /// <param name="camera"></param>
    public void UpdateScreenPosition(Camera camera)
    {
        ScreenPosition = camera.WorldToScreen(WorldPosition);
    }

    /// <summary>
    /// Caches <c>newCameraScale</c> to <c>CachedCameraScale</c>.
    /// </summary>
    /// <param name="newCameraScale"></param>
    public void OnChangedZoomBase(float newCameraScale)
    {
        CachedCameraScale = newCameraScale;
    }
    
    /// <summary>
    /// Should be subscribed to <see cref="Camera.OnZoomed"/>. You can use <c>newCameraScale</c> but then
    /// call the base method (<see cref="OnChangedZoomBase"/>).
    /// </summary>
    /// <param name="newCameraScale"></param>
    public void OnChangedZoom(float newCameraScale)
    {
        OnChangedZoomBase(newCameraScale);
    }
}