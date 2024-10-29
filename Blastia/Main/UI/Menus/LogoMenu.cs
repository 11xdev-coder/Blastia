using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus;

public class LogoMenu : Menu
{
    private Image? _rotatingImageTest;
    
    public LogoMenu(SpriteFont font, bool isActive = true) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        LogoImageElement logoText = new LogoImageElement(new Vector2(0, 100),
            BlastiaGame.LogoTexture)
        {
            HAlign = 0.5f
        };
        Elements.Add(logoText);
    }

    public override void Update()
    {
        base.Update();
        
        UpdateRotationTangent();
    }

    private void UpdateRotationTangent()
    {
        if (_rotatingImageTest == null) return;
        
        Vector2 start = _rotatingImageTest.Position;
        Vector2 end = BlastiaGame.CursorPosition;
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;

        float theta = (float) Math.Atan(dy / dx) + MathHelper.ToRadians(135);
        if (dx < 0) theta += MathHelper.Pi;
        
        _rotatingImageTest.Rotation = theta;
    }

    private void UpdateRotationDotProduct()
    {
        if (_rotatingImageTest == null) return;
        
        Vector2 start = _rotatingImageTest.Position;
        Vector2 end = BlastiaGame.CursorPosition;

        // dot product with direction and right vector (1; 0)
        Vector2 direction = end - start;

        double dotProduct = direction.X; // dir.X * 1 + dir.Y * 0
        double directionMagnitude = direction.Magnitude();

        float theta = (float) Math.Acos(dotProduct / directionMagnitude); // right vec magnitude = 1
        if (direction.Y < 0) // flip
        {
            theta = MathHelper.TwoPi - theta;
        }
    
        // offset
        _rotatingImageTest.Rotation = theta + MathHelper.ToRadians(135);
    }
}