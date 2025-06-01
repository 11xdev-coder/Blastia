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

    public override int Height => 1;
    public override int Width => 1;
    protected override bool ApplyGravity => true;
    protected override float Mass => 10;

    public DroppedItem(Vector2 position, float initialScaleFactor) : base(position, initialScaleFactor)
    {
        SetId(EntityID.DroppedItem);

        _itemBodyPart = new BodyPart(BlastiaGame.InvisibleTexture, Vector2.Zero);
    }

    public void Launch(Item? item, int launchDirection, float horizontalSpeed = 250f, float verticalSpeed = -150f)
    {
        Item = item;
        if (Item == null) return;
        
        _itemBodyPart.Image = Item.Icon;
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