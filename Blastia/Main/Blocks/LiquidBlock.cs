using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
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
    /// <c>ItemId</c> to give when player right clicked on this liquid with an empty bucket
    /// </summary>
    public ushort BucketItemId { get; protected set; }
    
    /// <summary>
    /// 1-8, where 8 is full block
    /// </summary>
    public int FlowLevel { get; set; } = 8;
    /// <summary>
    /// Flow speed in blocks per second
    /// </summary>
    public float FlowRate { get; protected set; }
    /// <summary>
    /// How far liquid can flow horizontally
    /// </summary>
    public int MaxFlowDownDistance { get; protected set; }
    public int CurrentFlowDownDistance { get; set; }

    private WorldState _currentWorldState;

    protected LiquidBlock(ushort id, string name, float flowSpeed = 2f, int maxFlowDownDistance = 8, ushort bucketItemId = 0) 
        : base(id, name, 200f, 0f, false, true, 0, 0)
    {
        FlowRate = flowSpeed;
        MaxFlowDownDistance = maxFlowDownDistance;
        BucketItemId = bucketItemId;

        _currentWorldState = PlayerNWorldManager.Instance.SelectedWorld ?? new WorldState();
    }

    public override Rectangle GetRuleTileSourceRectangle(bool emptyTop, bool emptyBottom, bool emptyRight, bool emptyLeft)
    {
        return BlockRectangles.TopLeft;
    }
    
    public override void Update(World world, Vector2 position)
    {
        
        base.Update(world, position);
        if (PlayerNWorldManager.Instance.SelectedWorld == null) return;
        _currentWorldState = PlayerNWorldManager.Instance.SelectedWorld;

        var blockX = (int) position.X;
        var blockY = (int) position.Y;
        TryFlowingDown(blockX, blockY);
    }

    /// <summary>
    /// Tries to extend liquid down until it hits the ground, or <c>CurrentDistance</c> reaches <c>MaxFlowDistance</c>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void TryFlowingDown(int x, int y)
    {
        var below = _currentWorldState.GetBlockInstance(x, y + Size);

        if (below?.Block is LiquidBlock belowLiquid && belowLiquid.Id == Id)
        {
            // combine levels
            var combinedLevels = FlowLevel + belowLiquid.FlowLevel;

            if (combinedLevels <= 8)
            {
                // dont exceed max -> move all liquid down
                belowLiquid.FlowLevel = combinedLevels;
                _currentWorldState.SetTile(x, y, BlockId.Air); // remove this liquid
            }
            else
            {
                // combined levels exceed max -> max out below liquid and leave this on top
                belowLiquid.FlowLevel = 8;
                FlowLevel = combinedLevels - 8;
                
                // excess liquid -> flow horizontally
                TryFlowingHorizontally(x, y);
            }

            return;
        }
        
        if (CurrentFlowDownDistance >= MaxFlowDownDistance)
        {
            // exceed distance -> move whole thing down
            ShiftThisAndAboveLiquidDown(x, y);
            return;
        }

        // no blocks below -> continue flowing
        if (below == null || below.Block is {IsCollidable: false})
        {
            CreateLiquidAt(x, y + Size, 8, CurrentFlowDownDistance + 1);
            _currentWorldState.SetTile(x, y, BlockId.Air);
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
        var belowTile = _currentWorldState.GetBlockInstance(x, y + Size);
        bool onSolidGround = belowTile?.Block is {IsCollidable: true} || _currentWorldState.GetTile(x, y + Size) > 0;

        if (onSolidGround)
        {
            // solid ground -> flow horizontally
            TryFlowingHorizontally(x, y);
        }
        else
        {
            // can flow down -> shift down
            _currentWorldState.SetTile(x, y, BlockId.Air);
            CreateLiquidAt(x, y + Size, FlowLevel, CurrentFlowDownDistance);

            // tell block above to shift down
            var aboveTile = _currentWorldState.GetBlockInstance(x, y - Size);
            if (aboveTile?.Block is LiquidBlock aboveLiquid && aboveLiquid.Id == Id)
                aboveLiquid.ShiftThisAndAboveLiquidDown(x, y - Size);
        }
    }

    private void TryFlowingHorizontally(int x, int y)
    {
        // check left and right
        var leftBlock = _currentWorldState.GetTile(x - Size, y);
        var rightBlock = _currentWorldState.GetTile(x + Size, y);
        
        var leftHasAirBelow = _currentWorldState.GetTile(x - Size, y + Size) == 0;
        var rightHasAirBelow = _currentWorldState.GetTile(x + Size, y + Size) == 0;
        
        int flowTargets = 0;
        if (leftBlock == 0) flowTargets++;
        if (rightBlock == 0) flowTargets++;

        // no targets -> no flow
        if (flowTargets == 0) return;
        
        // total blocks to distribute to
        var totalBlocks = flowTargets + 1;

        var originalLevel = FlowLevel;
        
        // how much goes to each block
        var levelPerBlock = originalLevel / totalBlocks;
        var remainder = originalLevel % totalBlocks;

        // cant distribute at least 1 level per block
        if (levelPerBlock == 0)
        {
            // move all liquid to one side if we cant divide it 
            if (leftBlock == 0)
            {
                CreateLiquidAt(x - Size, y, originalLevel, CurrentFlowDownDistance);
                _currentWorldState.SetTile(x, y, BlockId.Air);
            }
            else if (rightBlock == 0)
            {
                CreateLiquidAt(x + Size, y, originalLevel, CurrentFlowDownDistance);
                _currentWorldState.SetTile(x, y, BlockId.Air);
            }
            return;
        }
        
        // distribute liquid evenly
        FlowLevel = levelPerBlock + remainder;
    
        // add the remainder to this block
        if (remainder > 0)
            FlowLevel += remainder;

        // flow to available spaces
        // Create new liquid blocks if needed
        if (leftBlock == 0)
        {
            // If there's air below this new liquid, reset flow distance to allow falling
            CreateLiquidAt(x - Size, y, levelPerBlock, 
                leftHasAirBelow ? 0 : CurrentFlowDownDistance);
        }

        if (rightBlock == 0)
        {
            CreateLiquidAt(x + Size, y, levelPerBlock,
                rightHasAirBelow ? 0 : CurrentFlowDownDistance);
        }
    }

    private void CreateLiquidAt(int x, int y, int targetFlowLevel, int flowDownDistance)
    {
        var liquid = CreateNewInstance();
        liquid.FlowLevel = targetFlowLevel;
        liquid.CurrentFlowDownDistance = flowDownDistance;
        _currentWorldState.SetTileInstance(x, y, new BlockInstance(liquid, 0));
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

        spriteBatch.Draw(texture, adjustedDestRectangle, adjustedSourceRectangle, Color.White);
    }
}
