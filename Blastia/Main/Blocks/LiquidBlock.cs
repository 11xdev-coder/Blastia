using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
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

    public override void OnPlace(World? world, Vector2 position, Player? player)
    {
        base.OnPlace(world, position, player);
        
        // register source
        if (IsSourceBlock)
        {
            var sourceId = GetUniqueSourceId((int)position.X, (int)position.Y);
            LiquidSourceRegistry.RegisterSource(sourceId, position, Id);
        }
    }

    public override void OnBreak(World? world, Vector2 position, Player? player)
    {
        base.OnBreak(world, position, player);
        
        // unregister source
        if (IsSourceBlock)
        {
            var sourceId = GetUniqueSourceId((int)position.X, (int)position.Y);
            LiquidSourceRegistry.UnregisterSource(sourceId);
        }
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
            LiquidSourceRegistry.RegisterSource(sourceId, position, Id);
            
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

        CleanupStaleConnections();
        
        var connectionResult = FindAndConnectToSources(blockX, blockY);

        if (connectionResult.IsConnected)
        {
            HasStartedDecaying = false;
            DecayTimer = 0f;
            ActiveSourceId = connectionResult.BestSourceId;
            DistanceFromSource = connectionResult.DistanceFromSource;
            LastConnectionTime = _globalTime;

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
    /// Clean up connections to destroyed sources
    /// </summary>
    private void CleanupStaleConnections()
    {
        if (IsSourceBlock) return;
    
        var validSources = new HashSet<int>();
    
        foreach (var sourceId in ConnectedSourceIds.ToArray()) 
        {
            // if souce is still active
            if (LiquidSourceRegistry.IsSourceActive(sourceId))
            {
                if (LiquidSourceRegistry.TryGetSourceType(sourceId, out var liquidType) && liquidType == Id)
                {
                    validSources.Add(sourceId);
                }
            }
        }
    
        ConnectedSourceIds = validSources;
    
        if (!validSources.Contains(ActiveSourceId))
        {
            ActiveSourceId = validSources.FirstOrDefault();
            if (ActiveSourceId == 0)
            {
                LastConnectionTime = 0f;
            }
        }
    }
    
    private bool FindSpecificSource(int sourceId)
    {
        // Use the source registry to check if the source is active
        return LiquidSourceRegistry.IsSourceActive(sourceId) && 
               LiquidSourceRegistry.TryGetSourceType(sourceId, out var liquidType) && 
               liquidType == Id;
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
        
        var position = new Vector2(blockX, blockY);
        var result = new SourceConnectionResult();
        var validSources = new HashSet<int>();
        
        var sourcesInRange = LiquidSourceRegistry.GetSourcesInRange(position, MaxFlowDistance * Size, Id);
        
        if (sourcesInRange.Count > 0)
        {
            var closestDistanceSquared = float.MaxValue;
            
            foreach (var sourceEntry in sourcesInRange)
            {
                int sourceId = sourceEntry.Key;
                Vector2 sourcePosition = sourceEntry.Value;
                
                if (HasValidFlowPath(blockX, blockY, (int)sourcePosition.X, (int)sourcePosition.Y))
                {
                    validSources.Add(sourceId);
                    
                    // find closest source
                    float distanceSquared = Vector2.DistanceSquared(position, sourcePosition);
                    if (distanceSquared < closestDistanceSquared)
                    {
                        closestDistanceSquared = distanceSquared;
                        result.IsConnected = true;
                        result.BestSourceId = sourceId;
                        // to block distance
                        result.DistanceFromSource = (int)Math.Round(Math.Sqrt(distanceSquared) / Size);
                    }
                }
            }
        }

        // no sources found -> fall back to pathfinding
        if (!result.IsConnected)
        {
            return FindAndConnectToSourcesUsingPathfinding(blockX, blockY);
        }

        if (result.IsConnected)
        {
            result.ConnectedSources = validSources;
            ConnectedSourceIds = validSources;
            LastConnectionTime = _globalTime;
        }
        else
        {
            // clear stale connections
            ConnectedSourceIds.Clear();
            ActiveSourceId = 0;
        }
        
        return result;
    }

    /// <summary>
    /// Pathfinding method for connecting to sources through other liquid blocks
    /// </summary>
    private SourceConnectionResult FindAndConnectToSourcesUsingPathfinding(int blockX, int blockY)
    {
        var result = new SourceConnectionResult();
        var visited = new HashSet<Vector2>();
        var queue = new Queue<(Vector2 pos, int distance)>();
        var validSources = new HashSet<int>();
        
        queue.Enqueue((new Vector2(blockX, blockY), 0));
        visited.Add(new Vector2(blockX, blockY));

        // respect gravity
        Vector2[] directions = [
            new(0, -8),  // up
            new(8, 0),   // right
            new(-8, 0)   // left
        ];

        while (queue.Count > 0)
        {
            var (currentPos, distance) = queue.Dequeue();
            var currentX = (int) currentPos.X;
            var currentY = (int) currentPos.Y;

            foreach (var direction in directions)
            {
                var checkX = currentX + (int) direction.X;
                var checkY = currentY + (int) direction.Y;
                var checkPos = new Vector2(checkX, checkY);

                if (!visited.Add(checkPos))
                    continue;

                var blockInstance = _currentWorldState.GetBlockInstance(checkX, checkY);
                if (blockInstance?.Block is LiquidBlock liquidBlock && liquidBlock.Id == Id)
                {
                    if (liquidBlock.IsSourceBlock)
                    {
                        // verify source exists using the registry
                        var sourceId = liquidBlock.GetUniqueSourceId(checkX, checkY);
                        if (LiquidSourceRegistry.IsSourceActive(sourceId))
                        {
                            validSources.Add(sourceId);
                            
                            // best connection (closest)
                            if (!result.IsConnected || distance < result.DistanceFromSource)
                            {
                                result.IsConnected = true;
                                result.BestSourceId = sourceId;
                                result.DistanceFromSource = distance;
                            }
                        }
                    }
                    else if (liquidBlock.FlowLevel > 0 && !liquidBlock.HasStartedDecaying) // continue searching through flow
                    {
                        queue.Enqueue((checkPos, distance + 1));
                    }
                } 
            }
        }

        if (result.IsConnected)
        {
            result.ConnectedSources = validSources;
            ConnectedSourceIds = validSources;
            LastConnectionTime = _globalTime;
        }
        else
        {
            // clear stale connections
            ConnectedSourceIds.Clear();
            ActiveSourceId = 0;
        }
        
        return result;
    }
    
    private bool HasValidFlowPath(int sourceX, int sourceY, int targetX, int targetY)
    {
        // can connect to adjacent blocks
        if (Math.Abs(targetX - sourceX) + Math.Abs(targetY - sourceY) <= Size)
        {
            return true;
        }
        
        return CanFlowDownwardPath(sourceX, sourceY, targetX, targetY, []);
    }

    private bool CanFlowDownwardPath(int sourceX, int sourceY, int targetX, int targetY, HashSet<Vector2> visited)
    {
        var currentPos = new Vector2(sourceX, sourceY);
        if (!visited.Add(currentPos))
            return false;
        
        // reached target
        if (sourceX == targetX && sourceY == targetY)
            return true;
        
        // try flowing down first
        var belowY = sourceY + Size;
        if (belowY <= targetY) // only check downward if target is below the source
        {
            var belowBlockId = _currentWorldState.GetTile(sourceX, belowY);
            if (belowBlockId == BlockId.Air) // flow down to empty space
            {
                if (CanFlowDownwardPath(sourceX, belowY, targetX, targetY, visited))
                    return true;
            }
            else
            {
                // flowing into same water
                var belowBlock = _currentWorldState.GetBlockInstance(sourceX, belowY);
                if (belowBlock?.Block is LiquidBlock belowLiquid && belowLiquid.Id == Id)
                {
                    if (CanFlowDownwardPath(sourceX, belowY, targetX, targetY, visited))
                        return true;
                }
                else if (belowBlock?.Block is {IsCollidable: true}) // hit solid ground
                {
                    // flow horizontally
                    var leftX = sourceX - Size;
                    var rightX = sourceX + Size;

                    foreach (var nextX in new[] {leftX, rightX})
                    {
                        var nextBlock = _currentWorldState.GetBlockInstance(nextX, sourceY);
                        if (nextBlock == null || (nextBlock.Block is LiquidBlock nextLiquid && nextLiquid.Id == Id && nextLiquid.FlowLevel > 0))
                        {
                            // air or same liquid
                            if (CanFlowDownwardPath(nextX, sourceY, targetX, targetY, visited))
                                return true;
                        }
                    }        
                }
            }
        }
        
        // target at the same level flow horizontally
        if (sourceY == targetY)
        {
            var belowBlock = _currentWorldState.GetBlockInstance(sourceX, sourceY + Size);
            
            // solid ground below
            if (belowBlock?.Block is {IsCollidable: true})
            {
                var direction = targetX > sourceX ? Size : -Size;
                var nextX = sourceX + direction;
                
                var nextBlock = _currentWorldState.GetBlockInstance(nextX, sourceY);
                if (nextBlock == null || (nextBlock.Block is LiquidBlock nextLiquid && nextLiquid.Id == Id && nextLiquid.FlowLevel > 0))
                {
                    // air or same liquid
                    if (CanFlowDownwardPath(nextX, sourceY, targetX, targetY, visited))
                        return true;
                }
            }
        }

        return false;
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

    /// <summary>
    /// Tries to flow horizontally in both directions
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void TryToFlowHorizontally(int x, int y)
    {
        var currentLevel = FlowLevel;
        if (currentLevel <= 1) return; // at least 2 levels to flow
        
        // only flow horizontally if it has a solid tile below
        var belowBlockId = _currentWorldState.GetTile(x, y+Size);
        if (belowBlockId > 0 && StuffRegistry.GetBlock(belowBlockId) is {IsCollidable: true})
        {
            var maxFlowDistance = Math.Max(1, MaxFlowDistance - DistanceFromSource);
            
            TryToFlowHorizontalCascade(x - Size, y, currentLevel - 1, -Size, 1, maxFlowDistance);
            TryToFlowHorizontalCascade(x + Size, y, currentLevel - 1, Size, 1, maxFlowDistance);
        }
    }

    /// <summary>
    /// Tries to continue flowing horizontally in one direction as far as possible
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="targetLevel"></param>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <param name="maxDistance"></param>
    private void TryToFlowHorizontalCascade(int x, int y, int targetLevel, int direction, int distance, int maxDistance)
    {
        if (distance > maxDistance || targetLevel <= 0) return;

        var targetBlockId = _currentWorldState.GetTile(x, y);
        
        // if we can flow here
        if (targetBlockId == BlockId.Air)
        {
            // solid ground below
            var belowBlock = _currentWorldState.GetBlockInstance(x, y + Size);
            if (belowBlock?.Block is {IsCollidable: true})
            {
                CreateLiquidAt(x, y, targetLevel);
                
                // still have level to flow -> continue cascading
                if (targetLevel > 1 && distance < maxDistance)
                {
                    TryToFlowHorizontalCascade(x + direction, y, targetLevel - 1, direction, distance + 1, maxDistance);
                }
            }
            else if (belowBlock == null || belowBlock.Block is {IsCollidable: false}) // can flow down
            {
                CreateLiquidAt(x, y, targetLevel);
            }
        }
        else // flow into existing liquid
        {
            var targetBlockInstance = _currentWorldState.GetBlockInstance(x, y);
            if (targetBlockInstance?.Block is LiquidBlock liquidTarget && liquidTarget.Id == Id &&
                !liquidTarget.IsSourceBlock
                && liquidTarget.FlowLevel < targetLevel)
            {
                // transfer
                var transfer = Math.Min(targetLevel - liquidTarget.FlowLevel, targetLevel);
                liquidTarget.FlowLevel = Math.Min(8, liquidTarget.FlowLevel + transfer);
                
                liquidTarget.FlowType = LiquidFlowType.Horizontal;
                liquidTarget.HasStartedDecaying = false;
                liquidTarget.DecayTimer = 0f;
            }
        }
    }

    private void CreateLiquidBelow(int tileX, int tileY)
    {
        var newLiquid = CreateNewInstance();
        newLiquid.FlowLevel = 8;
        
        // vertical flow -> directly below
        // still verify the connection exists
        if (ConnectedSourceIds.Count > 0 && ActiveSourceId != 0)
        {
            newLiquid.ConnectedSourceIds = [..ConnectedSourceIds];
            newLiquid.ActiveSourceId = ActiveSourceId;
            newLiquid.LastConnectionTime = _globalTime;
        }
        else
        {
            newLiquid.ConnectedSourceIds = [];
            newLiquid.ActiveSourceId = 0;
            newLiquid.LastConnectionTime = 0f;
        }
        
        newLiquid.FlowType = LiquidFlowType.Vertical;
        newLiquid.DistanceFromSource = DistanceFromSource;
        newLiquid.HasStartedDecaying = false;
        newLiquid.DecayTimer = 0f;
        
        _currentWorldState.SetTileInstance(tileX, tileY + Size, new BlockInstance(newLiquid, 0));
    }

    private void CreateLiquidAt(int tileX, int tileY, int level)
    {
        var newLiquid = CreateNewInstance();
        newLiquid.FlowLevel = level;
        
        // dont inherit sources, prevent phantom connections
        newLiquid.ConnectedSourceIds = [];
        newLiquid.ActiveSourceId = 0;
        newLiquid.LastConnectionTime = 0f;
        newLiquid.FlowType = LiquidFlowType.Horizontal;
        newLiquid.DistanceFromSource = DistanceFromSource + 1;
        newLiquid.HasStartedDecaying = false;
        newLiquid.DecayTimer = 0f;
        
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
