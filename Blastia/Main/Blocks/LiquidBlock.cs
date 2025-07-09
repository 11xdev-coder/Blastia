using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Physics;
using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks;

/// <summary>
/// Base block class for liquids
/// </summary>
[Serializable]
public abstract class LiquidBlock : Block
{
    /// <summary>
    /// Liquid transparency, <c>0</c> invisible, <c>255</c> fully opaque
    /// </summary>
    public virtual int Alpha { get; set; } = 160;
    
    /// <summary>
    /// <c>ItemId</c> to give when player right clicked on this liquid with an empty bucket
    /// </summary>
    public ushort BucketItemId { get; protected set; }
    
    /// <summary>
    /// 1-8, where 8 is full block
    /// </summary>
    public int FlowLevel { get; set; } = 8;
    /// <summary>
    /// Time between each <c>Update()</c> call. Less interval -> faster liquid flow
    /// </summary>
    public float FlowUpdateInterval { get; protected set; }
    
    public bool HasChangedThisFrame { get; set; }

    private float _flowTimer;

    private WorldState _currentWorldState;

    protected LiquidBlock(ushort id, string name, float flowUpdateInterval = 0.15f, ushort bucketItemId = 0) 
        : base(id, name, 200f, 0f, false, false, true, 0, 0)
    {
        FlowUpdateInterval = flowUpdateInterval;
        BucketItemId = bucketItemId;

        _currentWorldState = PlayerNWorldManager.Instance.SelectedWorld ?? new WorldState();
    }

    public override Rectangle GetRuleTileSourceRectangle(bool emptyTop, bool emptyBottom, bool emptyRight, bool emptyLeft)
    {
        return BlockRectangles.TopLeft;
    }

    public override TileLayer GetLayer() => TileLayer.Liquid;

    protected virtual void OnEntityEnter(Entity entity)
    {
        
    }
    
    public override void Update(World world, Vector2 position)
    {
        base.Update(world, position);

        HasChangedThisFrame = false;

        var previousFlowLevel = FlowLevel;
        _flowTimer += (float) BlastiaGame.GameTimeElapsedSeconds;

        if (_flowTimer >= FlowUpdateInterval)
        {
            if (PlayerNWorldManager.Instance.SelectedWorld == null) return;
            _currentWorldState = PlayerNWorldManager.Instance.SelectedWorld;

            var blockX = (int) position.X;
            var blockY = (int) position.Y;
            TryFlowingDown(blockX, blockY);
            
            _flowTimer -= FlowUpdateInterval;
        }

        // flow level changed
        if (FlowLevel != previousFlowLevel)
            HasChangedThisFrame = true;
        
        var rect = new Rectangle((int)position.X, (int)position.Y, Size, Size);
        var potentialEntities = Collision.GetPotentialEntitiesInRectangle(rect);
        foreach (var entity in potentialEntities)
        {
            if (entity == null) continue;
            
            if (entity.GetBounds().Intersects(rect))
            {
                OnEntityEnter(entity);
            }
        }
    }

    /// <summary>
    /// Tries to extend liquid down until it hits the ground, or <c>CurrentDistance</c> reaches <c>MaxFlowDistance</c>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void TryFlowingDown(int x, int y)
    {
        var belowLiquidInst = _currentWorldState.GetBlockInstance(x, y + Size, TileLayer.Liquid);

        if (belowLiquidInst?.Block is LiquidBlock belowLiquid && belowLiquid.Id == Id)
        {
            // combine levels
            var combinedLevels = FlowLevel + belowLiquid.FlowLevel;

            if (combinedLevels <= 8)
            {
                // dont exceed max -> move all liquid down
                belowLiquid.FlowLevel = combinedLevels;
                belowLiquid.HasChangedThisFrame = true;

                HasChangedThisFrame = true;
                _currentWorldState.SetTile(x, y, BlockId.Air, GetLayer()); // remove this liquid
            }
            else
            {
                // combined levels exceed max -> max out below liquid and leave this on top
                belowLiquid.FlowLevel = 8;
                belowLiquid.HasChangedThisFrame = true;
                
                FlowLevel = combinedLevels - 8;
                HasChangedThisFrame = true;
                
                // excess liquid -> flow horizontally
                TryFlowingHorizontally(x, y);
            }

            return;
        }

        // no blocks below -> continue flowing
        var belowGroundInst = _currentWorldState.GetBlockInstance(x, y + Size, TileLayer.Ground);
        if (belowGroundInst == null || belowGroundInst.Block is {IsCollidable: false})
        {
            CreateLiquidAt(x, y + Size, FlowLevel);
            _currentWorldState.SetTile(x, y, BlockId.Air, GetLayer());
        }
        else
        {
            // hit ground -> flow horizontally
            TryFlowingHorizontally(x, y);
        }
    }

    /// <summary>
    /// Shifts this and block above liquid one block below. If it hits the ground, it will try to split 
    /// </summary>
    /// <param name="x">X of this block</param>
    /// <param name="y">Y of this block</param>
    private void ShiftThisAndAboveLiquidDown(int x, int y)
    {
        var belowTile = _currentWorldState.GetBlockInstance(x, y + Size, TileLayer.Ground);
        bool onSolidGround = belowTile?.Block is {IsCollidable: true} || _currentWorldState.GetTile(x, y + Size, TileLayer.Ground) > 0;

        if (onSolidGround)
        {
            // solid ground -> flow horizontally
            TryFlowingHorizontally(x, y);
        }
        else
        {
            // can flow down -> shift down
            _currentWorldState.SetTile(x, y, BlockId.Air, GetLayer());
            CreateLiquidAt(x, y + Size, FlowLevel);

            // tell block above to shift down
            var aboveTile = _currentWorldState.GetBlockInstance(x, y - Size, TileLayer.Liquid);
            if (aboveTile?.Block is LiquidBlock aboveLiquid && aboveLiquid.Id == Id)
                aboveLiquid.ShiftThisAndAboveLiquidDown(x, y - Size);
        }
    }

    private void TryFlowingHorizontally(int x, int y)
    {
        // flow with enough levels
        if (FlowLevel < 2) return;

        var previousFlowLevel = FlowLevel;

        var leftGroundInst = _currentWorldState.GetBlockInstance(x - Size, y, TileLayer.Ground);
        var leftLiquidInst = _currentWorldState.GetBlockInstance(x - Size, y, TileLayer.Liquid);
        var rightGroundInst = _currentWorldState.GetBlockInstance(x + Size, y, TileLayer.Ground);
        var rightLiquidInst = _currentWorldState.GetBlockInstance(x + Size, y, TileLayer.Liquid);

        // flow levels of near liquids
        var leftLevel = (leftLiquidInst?.Block is LiquidBlock leftLiquid && leftLiquid.Id == Id) 
            ? leftLiquid.FlowLevel : 0;
        var rightLevel = (rightLiquidInst?.Block is LiquidBlock rightLiquid && rightLiquid.Id == Id) 
            ? rightLiquid.FlowLevel : 0;
        
        // level differences (-1 ensuring flow goes downhill)
        var leftDiff = FlowLevel - leftLevel - 1;
        var rightDiff = FlowLevel - rightLevel - 1;

        // flow only if there's a difference (flow downhill)
        var flowLeft = leftDiff > 0 && (leftGroundInst == null || leftLiquidInst?.Block is LiquidBlock);
        var flowRight = rightDiff > 0 && (rightGroundInst == null || rightLiquidInst?.Block is LiquidBlock);

        // no flow needed
        if (!flowLeft && !flowRight) return;

        // calculate equalized level
        var totalLiquid = FlowLevel;
        var blockCount = 1;
        
        if (flowLeft) 
        {
            totalLiquid += leftLevel;
            blockCount++;
        }
        
        if (flowRight) 
        {
            totalLiquid += rightLevel;
            blockCount++;
        }
        
        // target levels
        var targetLevel = totalLiquid / blockCount;
        var remainder = totalLiquid % blockCount;
        
        // apply changes
        var newSourceLevel = targetLevel;
        if (remainder > 0) 
        {
            newSourceLevel++;
            remainder--;
        }
        
        // update source
        FlowLevel = newSourceLevel;
        
        // update near blocks
        if (flowLeft) 
        {
            var newLeftLevel = targetLevel;
            if (remainder > 0) 
            {
                newLeftLevel++;
                remainder--;
            }
            
            if (leftLiquidInst?.Block is LiquidBlock existingLeft && existingLeft.Id == Id) 
            {
                existingLeft.FlowLevel = newLeftLevel;
                existingLeft.HasChangedThisFrame = true;
            } 
            else 
            {
                CreateLiquidAt(x - Size, y, newLeftLevel);
            }
        }
        
        if (flowRight) 
        {
            var newRightLevel = targetLevel + remainder; // any remaining liquid
            
            if (rightLiquidInst?.Block is LiquidBlock existingRight && existingRight.Id == Id) 
            {
                existingRight.FlowLevel = newRightLevel;
                existingRight.HasChangedThisFrame = true;
            } 
            else 
            {
                CreateLiquidAt(x + Size, y, newRightLevel);
            }
        }
        
        // remove if empty
        if (FlowLevel <= 0) 
        {
            HasChangedThisFrame = true;
            _currentWorldState.SetTile(x, y, BlockId.Air, GetLayer());
        }

        if (FlowLevel != previousFlowLevel)
            HasChangedThisFrame = true;
    }

    private void CreateLiquidAt(int x, int y, int targetFlowLevel)
    {
        var liquid = CreateNewInstance();
        liquid.FlowLevel = targetFlowLevel;
        liquid.HasChangedThisFrame = true;
        _currentWorldState.SetTile(x, y, new BlockInstance(liquid, 0), GetLayer());

        HasChangedThisFrame = true;
    }
    
    public abstract LiquidBlock CreateNewInstance();

    public override void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle, Vector2 worldPosition)
    {
        var texture = StuffRegistry.GetBlockTexture(Id);

        var liquidHeight = (destRectangle.Height * FlowLevel) / 8;
        var adjustedDestRectangle = new Rectangle(
            destRectangle.X,
            destRectangle.Y + (destRectangle.Height - liquidHeight),
            destRectangle.Width,
            liquidHeight);
        
        var sourceHeight = (sourceRectangle.Height * FlowLevel) / 8;
        var adjustedSourceRectangle = new Rectangle(
            sourceRectangle.X,
            sourceRectangle.Y + (sourceRectangle.Height - sourceHeight),
            sourceRectangle.Width,
            sourceHeight);

        var color = new Color(255, 255, 255, Alpha);
        spriteBatch.Draw(texture, adjustedDestRectangle, adjustedSourceRectangle, color);
    }
}
