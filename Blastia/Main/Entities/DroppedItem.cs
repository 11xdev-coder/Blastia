using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities;

[Entity(Id = EntityID.DroppedItem)]
public class DroppedItem : Entity
{
    public Item? Item { get; private set; }
    public int Amount { get; private set; }
    private const float MaxPickupTime = 1.6f;
    /// <summary>
    /// Time before item can be picked up. If it reaches <c>MaxPickupTime</c> (1.6f by default) then item can be picked up
    /// </summary>
    public float PickupTime;
    
    // PULLING
    public bool IsBeingPulled { get; private set; }
    /// <summary>
    /// What player is pulling (vacuuming) the item
    /// </summary>
    public Player? PullTargetPlayer { get; private set; }
    private const float MaxPullSpeed = 150f;
    private const float PullAccelerationFactor = 9f;
    
    private BodyPart _itemBodyPart;
    private World _world;

    public override float Height => 0.85f;
    public override float Width => 0.85f;
    protected override bool ApplyGravity => true;
    protected override float Mass => 6;
    protected override float FrictionMultiplier => 0.3f;
    protected override float Bounciness => 0.5f;

    public DroppedItem(Vector2 position, float initialScaleFactor, World world) : base(position, initialScaleFactor)
    {
        SetId(EntityID.DroppedItem);

        _itemBodyPart = new BodyPart(BlastiaGame.InvisibleTexture, Vector2.Zero);
        _world = world;
    }

    /// <summary>
    /// Initiates pull towards <c>target</c> player
    /// </summary>
    /// <param name="target"></param>
    public void StartPull(Player target)
    {
        // start pulling only when can pickup
        if (PickupTime < MaxPickupTime) return;
        
        IsBeingPulled = true;
        PullTargetPlayer = target;
        ApplyGravity = false;
    }

    /// <summary>
    /// Stops item from being pulled
    /// </summary>
    public void StopPull()
    {
        IsBeingPulled = false;
        PullTargetPlayer = null;
        ApplyGravity = true;
    }
    
    /// <summary>
    /// Removes <c>amountToReduce</c> from item's Amount and if <c>Amount &lt;= 0</c> removes the item.
    /// </summary>
    /// <param name="amountToReduce"></param>
    public void ReduceAmount(int amountToReduce)
    {
        if (amountToReduce > 0)
        {
            Amount = Math.Max(0, Amount - amountToReduce);
        }

        if (Amount <= 0)
        {
            BlastiaGame.RequestRemoveEntity(this);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptingPlayer"></param>
    /// <returns><c>True</c> (if item is not already pulling OR <c>PullTargetPlayer</c> is <c>attemptingPlayer</c>) AND <c>PickupTime</c> is more than max pickup time</returns>
    public bool CanBePickedUp(Player attemptingPlayer)
    {
        if (Item == null || Amount <= 0) return false;
        
        return (!IsBeingPulled || PullTargetPlayer == attemptingPlayer) && PickupTime >= MaxPickupTime;
    }

    public void Launch(Item? item, int amount, int launchDirection, float horizontalSpeed = 115f, float verticalSpeed = -100f)
    {
        Item = item;
        Amount = amount;
        if (Item == null) return;

        var halfItemIconWidth = Item.Icon.Width * 0.5f;
        var halfItemIconHeight = Item.Icon.Height * 0.5f;
        
        _itemBodyPart.Image = Item.Icon;
        _itemBodyPart.RelativePosition = new Vector2(-halfItemIconWidth, -halfItemIconHeight);
        
        MovementVector = new Vector2(horizontalSpeed * launchDirection, verticalSpeed);
    }

    public override void Update()
    {
        var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;

        // pulling
        if (IsBeingPulled && PullTargetPlayer != null)
        {
            var directionToPlayer = PullTargetPlayer.Position - Position;
            var distance = directionToPlayer.Length();

            // prevent jittering when too close
            if (distance > 1f)
            {
                directionToPlayer.Normalize();
                var currentTargetSpeed = MaxPullSpeed;
                var targetVelocity = currentTargetSpeed * directionToPlayer;
                MovementVector = Vector2.Lerp(MovementVector, targetVelocity, PullAccelerationFactor * deltaTime);
            }
        }
        else
        {
            // ensure to apply gravity if not pulling
            if (!ApplyGravity) ApplyGravity = true;
        }
        
        base.Update();

        PickupTime += (float) BlastiaGame.GameTimeElapsedSeconds;
        
        // hover text
        if (_world.MyPlayer?.Camera == null || BlastiaGame.TooltipDisplay == null || Item == null) return;

        var worldCursorPos = _world.MyPlayer.Camera.ScreenToWorld(BlastiaGame.CursorPosition);
        if (GetBounds().Contains(new Point(MathUtilities.SmoothRound(worldCursorPos.X), MathUtilities.SmoothRound(worldCursorPos.Y))))
        {
            BlastiaGame.TooltipDisplay.SetHoverText($"{Item.Name} ({Amount})");
        }
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