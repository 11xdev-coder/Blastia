using Blastia.Main.Blocks.Common;
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
    public int MaxFlowDistance { get; protected set; }

    /// <summary>
    /// Is this block an infinite source block of this liquid
    /// </summary>
    public bool IsSourceBlock { get; set; }

    protected LiquidBlock(ushort id, string name, float flowSpeed = 2f, int maxFlowDistance = 4)
        : base(id, name, 200f, 0f, false, true, 0, 0)
    {
        FlowRate = flowSpeed;
        MaxFlowDistance = maxFlowDistance;
    }

    public override void Update(World world, Vector2 position)
    {
        base.Update(world, position);

        // source blocks dont flow away
        if (IsSourceBlock) return;

        var blockX = (int) position.X;
        var blockY = (int) position.Y;

        if (TryToFlowDown(blockX, blockY)) return;
        TryToFlowHorizontally(blockX, blockY);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>False if we need to try flowing horizontally</returns>
    private bool TryToFlowDown(int x, int y)
    {
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return false;

        var belowBlockId = currentWorld.GetTile(x, y+1);
        if (belowBlockId == 0)
        {
            CreateLiquidBelow(x, y);
            return true;
        }

        var belowBlock = StuffRegistry.GetBlock(belowBlockId);
        if (belowBlock == null) return false;

        // flow into same liquid with not full level
        if (belowBlock is LiquidBlock liquidBlock && liquidBlock.Id == Id && !liquidBlock.IsSourceBlock)
        {
            if (liquidBlock.FlowLevel < 8)
            {
                TransferLiquid(liquidBlock, x, y+1);
                return true;
            }
        }

        return false;
    }

    private void TryToFlowHorizontally(int x, int y)
    {
        var currentLevel = FlowLevel;
        if (currentLevel <= 1) return; // at least 2 levels to flow
        
        TryToFlowHorizontalDirection(x - 1, y, currentLevel - 1);
        TryToFlowHorizontalDirection(x + 1, y, currentLevel - 1);
    }

    private void TryToFlowHorizontalDirection(int x, int y, int targetLevel)
    {
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return;

        var targetBlockId = currentWorld.GetTile(x, y);
        var targetBlock = StuffRegistry.GetBlock(targetBlockId);
        if (targetBlock == null) return;
        
        if (targetBlockId == 0) // flow into air
        {
            CreateLiquidAt(x, y, targetLevel);
            ReduceLevel(1);
        }
        else if (targetBlock is LiquidBlock liquidTarget && liquidTarget.Id == Id && !liquidTarget.IsSourceBlock) // flow into same liquid
        {
            if (liquidTarget.FlowLevel < targetLevel)
            {
                var transfer = Math.Min(targetLevel - liquidTarget.FlowLevel, FlowLevel - 1);
                liquidTarget.FlowLevel += transfer;
                ReduceLevel(transfer);
            }
        }
    }

    private void CreateLiquidBelow(int x, int y)
    {
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return;

        var newLiquid = CreateNewInstance();
        newLiquid.FlowLevel = Math.Min(8, FlowLevel);
        currentWorld.SetTileInstance(x, y+1, new BlockInstance(newLiquid, 0));

        if (!IsSourceBlock)
        {
            ReduceLevel(newLiquid.FlowLevel);
        }
    }

    private void CreateLiquidAt(int x, int y, int level)
    {
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return;
        
        var newLiquid = CreateNewInstance();
        newLiquid.FlowLevel = level;
        currentWorld.SetTileInstance(x, y, new BlockInstance(newLiquid, 0));
    }
    
    protected abstract LiquidBlock CreateNewInstance();

    private void TransferLiquid(LiquidBlock target, int targetX, int targetY)
    {
        var spaceInTarget = 8 - target.FlowLevel;
        var amountToTransfer = Math.Min(spaceInTarget, FlowLevel);

        target.FlowLevel += amountToTransfer;

        if (!IsSourceBlock)
        {
            ReduceLevel(amountToTransfer);
        }
    }
    
    private void ReduceLevel(int amount)
    {
        FlowLevel -= amount;
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle)
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