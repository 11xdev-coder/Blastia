using Blastia.Main.Entities.Common;
using Blastia.Main.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities;

[Entity(Id = EntityID.DroppedItem)]
public class DroppedItem : Entity
{
    public Item? Item { get; private set; }
    private BodyPart _itemBodyPart;

    public override float Height => 0.85f;
    public override float Width => 0.85f;
    protected override bool ApplyGravity => true;
    protected override float Mass => 6;
    protected override float FrictionMultiplier => 0.3f;
    protected override float Bounciness => 0.5f;

    public DroppedItem(Vector2 position, float initialScaleFactor) : base(position, initialScaleFactor)
    {
        SetId(EntityID.DroppedItem);

        _itemBodyPart = new BodyPart(BlastiaGame.InvisibleTexture, Vector2.Zero);
    }

    public void Launch(Item? item, int launchDirection, float horizontalSpeed = 150f, float verticalSpeed = -100f)
    {
        Item = item;
        if (Item == null) return;

        var halfItemIconWidth = Item.Icon.Width * 0.5f;
        var halfItemIconHeight = Item.Icon.Height * 0.5f;
        
        _itemBodyPart.Image = Item.Icon;
        _itemBodyPart.RelativePosition = new Vector2(-halfItemIconWidth, -halfItemIconHeight);
        
        MovementVector = new Vector2(horizontalSpeed * launchDirection, verticalSpeed);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Draw(spriteBatch, Position);
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1)
    {
        _itemBodyPart.Draw(spriteBatch, scaledPosition, scale * Scale);
    }
}