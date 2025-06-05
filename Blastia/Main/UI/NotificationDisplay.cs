using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

/// <summary>
/// Manages notification text at right-bottom of the screen
/// </summary>
public class NotificationDisplay
{
    private class NotificationEntry
    {
        public Text Text { get; }
        public float LifetimeRemaining { get; set; }
        public float InitialLifetime { get; }
        public float FadeOutStartTime { get; }

        public NotificationEntry(Text text, float lifetime)
        {
            Text = text;
            InitialLifetime = lifetime;
            LifetimeRemaining = lifetime;
            // 25% of lifetime or 0.5s
            FadeOutStartTime = Math.Min(lifetime * 0.25f, 0.5f);
        }
    }
    
    private readonly List<NotificationEntry> _notifications;
    private readonly SpriteFont _font;

    private const int MaxNotifications = 5;
    private const float VerticalSpacing = 5f;
    private const float RightMargin = 15f;
    private const float BottomMargin = 15f;
    private const float DefaultNotificationLifetime = 3f;

    public NotificationDisplay(SpriteFont font)
    {
        _font = font;
        _notifications = [];
    }

    public void AddNotification(string text, float lifetime = DefaultNotificationLifetime)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var notificationTextUi = new Text(Vector2.Zero, text, _font);
        notificationTextUi.Update();
        
        var notificationEntry = new NotificationEntry(notificationTextUi, lifetime);
        _notifications.Add(notificationEntry);

        if (_notifications.Count > MaxNotifications)
        {
            // remove oldest one at the start
            _notifications.RemoveAt(0);
        }

        RepositionNotifications();
    }

    private void RepositionNotifications()
    {
        var currentY = VideoManager.Instance.TargetResolution.Y - BottomMargin;

        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            var entry = _notifications[i];
            entry.Text.Update();

            var textVisualSize = _font.MeasureString(entry.Text.Text);
            currentY -= textVisualSize.Y;

            var posX = VideoManager.Instance.TargetResolution.X - RightMargin - textVisualSize.X;

            entry.Text.Position = new Vector2(posX, currentY);
            entry.Text.UpdateBounds();

            if (i > 0)
            {
                currentY -= VerticalSpacing;
            }
        }
    }

    public void Update()
    {
        var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;
        var needsReposition = false;

        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            var entry = _notifications[i];
            entry.LifetimeRemaining -= deltaTime;

            if (entry.LifetimeRemaining <= 0)
            {
                _notifications.RemoveAt(i);
                needsReposition = true;
            }
            else
            {
                if (entry.LifetimeRemaining <= entry.FadeOutStartTime)
                {
                    var fadeProgress = entry.LifetimeRemaining / entry.FadeOutStartTime;
                    entry.Text.Alpha = MathHelper.Clamp(fadeProgress, 0.1f, 1f); 
                }
                else
                {
                    entry.Text.Alpha = 1f;
                }
            }
        }

        if (needsReposition)
        {
            RepositionNotifications();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var entry in _notifications)
        {
            entry.Text.Draw(spriteBatch);
        }
    }
}