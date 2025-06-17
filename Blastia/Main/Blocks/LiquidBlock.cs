using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks;

public enum LiquidFlowType
{
    Source,
    Vertical,
    Horizontal
}

public struct SourceConnectionResult
{
    public bool IsConnected;
    public int BestSourceId;
    public int DistanceFromSource;
    public HashSet<int> ConnectedSources;
}

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

    /// <summary>
    /// How long to wait before starting decay (in seconds)
    /// </summary>
    public float DecayDelay { get; protected set; }
    /// <summary>
    /// How fast liquid decays when cut off (levels per second)
    /// </summary>
    public float DecayRate { get; protected set; }
    public float DecayTimer { get; set; }
    private Vector2[] _sourceCheckDirections = [new(8, 0), new(-8, 0), new(0, 8), new(0, -8)];
    
    /// <summary>
    /// True when <c>DecayDelay</c> has passed without connection
    /// </summary>
    public bool HasStartedDecaying { get; set; }
    /// <summary>
    /// Currently active source this liquid is connected to
    /// </summary>
    public int ActiveSourceId { get; set; }
    /// <summary>
    /// Set of all source IDs this liquid can connect to
    /// </summary>
    public HashSet<int> ConnectedSourceIds { get; set; } = [];
    /// <summary>
    /// Distance to nearest active source
    /// </summary>
    public int DistanceFromSource { get; set; }
    /// <summary>
    /// Type of liquid flow
    /// </summary>
    public LiquidFlowType FlowType { get; set; }
    /// <summary>
    /// Last time this liquid was connected to any valid source
    /// </summary>
    public float LastConnectionTime { get; set; }

    private float _globalTime;
    private WorldState _currentWorldState;

    protected LiquidBlock(ushort id, string name, float flowSpeed = 2f, int maxFlowDistance = 8, float decayDelay = 2f,
        float decayRate = 0.5f) : base(id, name, 200f, 0f, false, true, 0, 0)
    {
        FlowRate = flowSpeed;
        MaxFlowDistance = maxFlowDistance;
        DecayDelay = decayDelay;
        DecayRate = decayRate;

        _currentWorldState = PlayerManager.Instance.SelectedWorld ?? new WorldState();
    }

    public override Rectangle GetRuleTileSourceRectangle(bool emptyTop, bool emptyBottom, bool emptyRight, bool emptyLeft)
    {
        return BlockRectangles.TopLeft;
    }

    public override void Update(World world, Vector2 position)
    {
        base.Update(world, position);
        if (PlayerManager.Instance.SelectedWorld == null) return;
        _currentWorldState = PlayerManager.Instance.SelectedWorld;
        _globalTime += (float) BlastiaGame.GameTimeElapsedSeconds;

        var blockX = (int) position.X;
        var blockY = (int) position.Y;

        if (IsSourceBlock)
        {
            var sourceId = GetUniqueSourceId(blockX, blockY);
            ActiveSourceId = sourceId;
            ConnectedSourceIds.Clear();
            ConnectedSourceIds.Add(sourceId);
            LastConnectionTime = _globalTime;
            FlowType = LiquidFlowType.Source;
        }
        
        if (FlowLevel <= 0)
        {
            _currentWorldState.SetTile(blockX, blockY, 0);
            return;
        }

        var connectionResult = FindAndConnectToSources(blockX, blockY);

        if (connectionResult.IsConnected)
        {
            HasStartedDecaying = false;
            DecayTimer = 0f;
            ActiveSourceId = connectionResult.BestSourceId;
            DistanceFromSource = connectionResult.DistanceFromSource;

            UpdateFlowLevelFromSource(connectionResult);
        }
        else
        {
            HandleDisconnection(blockX, blockY);
        }

        if (!HasStartedDecaying && connectionResult.IsConnected)
        {
            if (TryToFlowDown(blockX, blockY)) return;
            TryToFlowHorizontally(blockX, blockY);
        }
    }

    /// <summary>
    /// Generates unique ID for a source block based on its position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private int GetUniqueSourceId(int x, int y)
    {
        return HashCode.Combine(x, y, Id);
    }

    /// <summary>
    /// Finds all available sources and determines best connection
    /// </summary>
    /// <param name="blockX"></param>
    /// <param name="blockY"></param>
    /// <returns></returns>
    private SourceConnectionResult FindAndConnectToSources(int blockX, int blockY)
    {
        if (IsSourceBlock)
        {
            var sourceId = GetUniqueSourceId(blockX, blockY);
            return new SourceConnectionResult
            {
                IsConnected = true,
                BestSourceId = sourceId,
                ConnectedSources = [sourceId],
                DistanceFromSource = 0
            };
        }
        
        var result = new SourceConnectionResult();
        var visited = new HashSet<Vector2>();
        var queue = new Queue<(Vector2 pos, int distance, HashSet<int> sourcesFound)>();
        
        queue.Enqueue((new Vector2(blockX, blockY), 0, []));
        visited.Add(new Vector2(blockX, blockY));

        while (queue.Count > 0)
        {
            var (currentPos, distance, sourcesFound) = queue.Dequeue();
            var currentX = (int) currentPos.X;
            var currentY = (int) currentPos.Y;

            foreach (var direction in _sourceCheckDirections)
            {
                var checkX = currentX + (int) direction.X;
                var checkY = currentY + (int) direction.Y;
                var checkPos = new Vector2(checkX, checkY);

                if (visited.Contains(checkPos))
                    continue;
                visited.Add(checkPos);

                var blockInstance = _currentWorldState.GetBlockInstance(checkX, checkY);
                if (blockInstance?.Block is LiquidBlock liquidBlock && liquidBlock.Id == Id)
                {
                    var newSourcesFound = new HashSet<int>(sourcesFound);

                    if (liquidBlock.IsSourceBlock)
                    {
                        var sourceId = liquidBlock.GetUniqueSourceId(checkX, checkY);
                        newSourcesFound.Add(sourceId);
                        
                        // better connection if closer/first found
                        if (!result.IsConnected || distance < result.DistanceFromSource)
                        {
                            result.IsConnected = true;
                            result.BestSourceId = sourceId;
                            result.DistanceFromSource = distance;
                            result.ConnectedSources = [..newSourcesFound];
                        }
                    }
                    else if (liquidBlock.FlowLevel > 0 && !liquidBlock.HasStartedDecaying) // continue searching through flow
                    {
                        // inherit sources
                        foreach (var sourceId in liquidBlock.ConnectedSourceIds)
                        {
                            newSourcesFound.Add(sourceId);
                        }
                        
                        queue.Enqueue((checkPos, distance + 1, newSourcesFound));
                    }
                } 
            }
        }

        if (result.IsConnected)
        {
            ConnectedSourceIds = result.ConnectedSources;
        }
        
        return result;
    }

    /// <summary>
    /// Updates flow level based on flow connection info
    /// </summary>
    /// <param name="connectionResult"></param>
    private void UpdateFlowLevelFromSource(SourceConnectionResult connectionResult)
    {
        // dont reduce level of sources
        if (IsSourceBlock) return;

        var targetLevel = 8;

        if (FlowType == LiquidFlowType.Horizontal)
        {
            // horizontal flow reduces with distance
            targetLevel = Math.Max(1, 8 - (connectionResult.DistanceFromSource / 2));
        }
        else if (FlowType == LiquidFlowType.Vertical)
        {
            // vertical remains full
            targetLevel = 8;
        }
        
        // gradually adjust to target
        if (FlowLevel < targetLevel)
        {
            FlowLevel = Math.Min(targetLevel, FlowLevel + 1);
        }
        else if (FlowLevel > targetLevel)
        {
            FlowLevel = Math.Max(targetLevel, FlowLevel - 1);
        }
    }

    /// <summary>
    /// Handles liquid decay when it is cut off from the source
    /// </summary>
    /// <param name="blockX"></param>
    /// <param name="blockY"></param>
    private void HandleDisconnection(int blockX, int blockY)
    {
        var timeSinceLastConnection = _globalTime - LastConnectionTime;

        if (timeSinceLastConnection >= DecayDelay && !HasStartedDecaying)
        {
            HasStartedDecaying = true;
            DecayTimer = 0f;
        }

        if (HasStartedDecaying)
        {
            var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;
            DecayTimer += deltaTime;
            var decayAmount = DecayRate * deltaTime;
            FlowLevel = Math.Max(0, FlowLevel - (int) Math.Ceiling(decayAmount));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>False if we need to try flowing horizontally</returns>
    private bool TryToFlowDown(int x, int y)
    {
        var belowBlockId = _currentWorldState.GetTile(x, y+Size);
        if (belowBlockId == 0)
        {
            CreateLiquidBelow(x, y);
            return true;
        }

        var belowBlockInstance = _currentWorldState.GetBlockInstance(x, y + Size);

        // flow into same liquid with not full level
        if (belowBlockInstance?.Block is LiquidBlock liquidBelow && liquidBelow.Id == Id && !liquidBelow.IsSourceBlock && liquidBelow.FlowLevel < FlowLevel)
        {
            // transfer
            var transferAmount = Math.Min(FlowLevel - liquidBelow.FlowLevel, FlowLevel - 1);
            liquidBelow.FlowLevel += transferAmount;
            liquidBelow.ConnectedSourceIds = new HashSet<int>(ConnectedSourceIds);
            liquidBelow.ActiveSourceId = ActiveSourceId;
            liquidBelow.LastConnectionTime = LastConnectionTime;
            liquidBelow.FlowType = LiquidFlowType.Vertical;
            liquidBelow.FlowLevel = FlowLevel;
            liquidBelow.HasStartedDecaying = false;
            return true;
        }

        return false;
    }

    private void TryToFlowHorizontally(int x, int y)
    {
        var currentLevel = FlowLevel;
        if (currentLevel <= 1) return; // at least 2 levels to flow
        
        // only flow horizontally if it has a solid tile below
        var belowBlockId = _currentWorldState.GetTile(x, y+Size);
        if (belowBlockId > 0 && StuffRegistry.GetBlock(belowBlockId) is {IsCollidable: true})
        {
            TryToFlowHorizontalDirection(x - Size, y, currentLevel - 1);
            TryToFlowHorizontalDirection(x + Size, y, currentLevel - 1);
        }
        
    }

    private void TryToFlowHorizontalDirection(int x, int y, int targetLevel)
    {
        var targetBlockId = _currentWorldState.GetTile(x, y);
        
        if (targetBlockId == 0) // flow into air
        {
            CreateLiquidAt(x, y, targetLevel);
        }
        // else
        // {
        //     var targetBlockInstance = currentWorld.GetBlockInstance(x, y);
        //     if (targetBlockInstance?.Block is LiquidBlock liquidTarget && liquidTarget.Id == Id && !liquidTarget.IsSourceBlock) // flow into same liquid
        //     {
        //         if (liquidTarget.FlowLevel < targetLevel)
        //         {
        //             var transfer = Math.Min(targetLevel - liquidTarget.FlowLevel, FlowLevel - 1);
        //             liquidTarget.FlowLevel += transfer;
        //             ReduceLevel(transfer);
        //         }
        //     }
        // }
    }

    private void CreateLiquidBelow(int tileX, int tileY)
    {
        var newLiquid = CreateNewInstance();
        newLiquid.FlowLevel = 8;
        newLiquid.ConnectedSourceIds = new HashSet<int>(ConnectedSourceIds);
        newLiquid.ActiveSourceId = ActiveSourceId;
        newLiquid.LastConnectionTime = LastConnectionTime;
        newLiquid.FlowType = LiquidFlowType.Vertical;
        _currentWorldState.SetTileInstance(tileX, tileY + Size, new BlockInstance(newLiquid, 0));
    }

    private void CreateLiquidAt(int tileX, int tileY, int level)
    {
        var newLiquid = CreateNewInstance();
        newLiquid.FlowLevel = level;
        newLiquid.ConnectedSourceIds = new HashSet<int>(ConnectedSourceIds);
        newLiquid.ActiveSourceId = ActiveSourceId;
        newLiquid.LastConnectionTime = LastConnectionTime;
        newLiquid.FlowType = LiquidFlowType.Horizontal;
        newLiquid.DistanceFromSource = DistanceFromSource + 1;
        _currentWorldState.SetTileInstance(tileX, tileY, new BlockInstance(newLiquid, 0));
    }
    
    protected abstract LiquidBlock CreateNewInstance();

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

        var color = Color.White;
        if (IsSourceBlock) color = Color.Red;
        spriteBatch.Draw(texture, adjustedDestRectangle, adjustedSourceRectangle, color);
    }
}    