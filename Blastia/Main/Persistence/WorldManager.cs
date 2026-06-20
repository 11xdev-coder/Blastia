using System.Numerics;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.Persistence;

/// <summary>
/// Stores both tiles and actual instances of the block
/// </summary>
public class TileLayerData 
{
    public Dictionary<Vector2, BlockInstance> Instances { get; set; } = [];
}

public enum TileLayer
{
	Ground, // dirt, stone, basic blocks
	Liquid, // water, lava
	Furniture // chests, signs, chairs
}

[Serializable]
public class WorldState : State 
{
    [EssentialProperty]	public int WorldWidth { get; set; }
	[EssentialProperty]	public int WorldHeight { get; set; }
	[EssentialProperty]	public WorldDifficulty Difficulty { get; set; } = WorldDifficulty.Easy;
	
	public TileLayerData GroundLayer { get; set; } = new();
	
	public TileLayerData LiquidLayer { get; set; } = new();
	
	public TileLayerData FurnitureLayer { get; set; } = new();

	// sign position -> sign text
	public Dictionary<Vector2, string> SignTexts { get; set; } = [];

	public Vector2 Spawn { set; get; }

	public bool SetTileLogs { get; set; } = false;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="layer"></param>
	/// <returns>TileLayerData that holds instance dictionary</returns>
	private TileLayerData GetLayerData(TileLayer layer)
	{
		return layer switch
		{
			TileLayer.Ground => GroundLayer,
			TileLayer.Liquid => LiquidLayer,
			TileLayer.Furniture => FurnitureLayer,
			_ => GroundLayer
		};
	}
	
	/// <summary>
	/// Sets tile's ID at x y coords. If new ID is 0 -> removes the tile completely.
    /// Coordinates must be mapped to Block.Size grid
	/// </summary>
	/// <param name="player">Player that set the tile</param>
	public void SetTile(int x, int y, ushort value, TileLayer layer, Player? player = null)
	{
		Vector2 pos = new(x, y);
		if (SetTileLogs) 
			Console.WriteLine($"World: {Name}, Set tile at: (X: {x}, Y: {y}), ID: {value}");
		
		if (value == 0)
		{
			RemoveTile(layer, pos, player);
			return;
		}
		
        var block = StuffRegistry.GetBlock(value);
        if (block == null) return;
        
        SetTile(pos, new BlockInstance(block, 0), layer, player);
	}
    
    /// <summary>
    /// Removes tile with proper calls
    /// </summary>
	private void RemoveTile(TileLayer layer, Vector2 pos, Player? player = null)
	{
	    var layerData = GetLayerData(layer);
	    if (layerData.Instances.TryGetValue(pos, out var inst)) 
	    {
	        inst.OnBreak(player?.World, pos, player);
	        layerData.Instances.Remove(pos);
	    }
	}

	/// <summary>
	/// Sets block instance at coordinates
    /// Coordinates must be mapped to Block.Size grid
	/// </summary>
	/// <param name="block">New block instance, must not be empty</param>
	/// <param name="player">Player that set the tile</param>
	public void SetTile(Vector2 pos, BlockInstance block, TileLayer layer, Player? player = null)
	{   
        var layerData = GetLayerData(layer);
		block.OnPlace(player?.World, pos, player);
		layerData.Instances[pos] = block;
	}

	/// <summary>
	/// Returns block instance of block at x y
	/// Coordinates must be mapped to Block.Size grid
	/// </summary>
	public BlockInstance? GetBlockInstance(int x, int y, TileLayer layer) 
	{
	    var layerData = GetLayerData(layer);
	    if (layerData.Instances.TryGetValue(new Vector2(x, y), out var inst)) 
	    {
	        return inst;
	    }
	    
	    return null;
	}
	
	/// <summary>
	/// Returns ID of block at x y
	/// Coordinates must be mapped to Block.Size grid
	/// </summary>
	public ushort GetTile(int x, int y, TileLayer layer) => GetBlockInstance(x, y, layer)?.Id ?? 0;
	
	/// <summary>
	/// Maps x y coordinates to block grid to match top-left position of block
	/// </summary>
	public (int x, int y) MapToBlockGrid(int x, int y) 
	{
	    int newX = (int)Math.Floor((float)x / Block.Size) * Block.Size;
		int newY = (int)Math.Floor((float)y / Block.Size) * Block.Size;
		return (newX, newY);
	}
	
	/// <summary>
	/// Maps coordinate to block grid to match top-left position of block
	/// </summary>
	public int MapToBlockGrid(int val) => (int)Math.Floor((float)val / Block.Size) * Block.Size;
	
	public bool HasTile(int x, int y, TileLayer layer) => GetBlockInstance(x, y, layer) != null;

	
	/// <summary>
	/// Helper method to find first non-air tile below an entity
	/// </summary>
	/// <param name="entityLeftX">Entity left edge X (mapped to block grid)</param>
	/// <param name="entityBottomY">Entity bottom edge world Y (mapped to block grid)</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <param name="checkDistance">Distance to check for tile below</param>
	/// <param name="layer"></param>
	/// <returns>Tuple containing tile ID and coordinates, returns <c>(BlockId.Air, 0, 0)</c> if none found</returns>
	public (BlockInstance? inst, int x, int y) GetFirstTileBelowWithCoords(float entityLeftX, float entityBottomY,
		float entityWidthPixels, float checkDistance, TileLayer layer)
	{
		var checkWorldY = (int) (entityBottomY + checkDistance);

		var checkWorldXLeft = (int) (entityLeftX + 1);
		var checkWorldXCenter = (int) (entityLeftX + entityWidthPixels * 0.5f);
		var checkWorldXRight = (int) (entityLeftX + entityWidthPixels - 1);
		
		var gridY = MapToBlockGrid(checkWorldY);
		var gridXLeft = MapToBlockGrid(checkWorldXLeft);
		var gridXCenter = MapToBlockGrid(checkWorldXCenter);
		var gridXRight = MapToBlockGrid(checkWorldXRight);
		
		// check left, center and right
		foreach (var gridX in new[] {gridXLeft, gridXCenter, gridXRight}) 
		{
		    var inst = GetBlockInstance(gridX, gridY, layer);
		    if (inst != null)
		    	return (inst, gridX, gridY);
		}

		return (null, 0, 0);
	}

	/// <summary>
	/// Gets the drag coefficient of the tile directly beneath the entity's feet
	/// by checking multiple points along its bottom edge
	/// </summary>
	/// <param name="entityLeftX">Entity left edge world X</param>
	/// <param name="entityBottomY">Entity bottom edge world Y</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <param name="layer"></param>
	/// <returns>If tile was found returns its drag, otherwise returns <c>Block.AirDragCoefficient</c></returns>
	public float GetDragCoefficientTileBelow(float entityLeftX, float entityBottomY, float entityWidthPixels, TileLayer layer)
	{
	    var (inst, _, _) = GetFirstTileBelowWithCoords(entityLeftX, entityBottomY, entityWidthPixels, 1f, layer);
	    if (inst != null)
	        return inst.Block.DragCoefficient;

	    return Block.AirDragCoefficient;
	}
}

public class WorldManager : Singleton<WorldManager>
{
    public static string WorldsSaveFolder = Path.Combine(Paths.GetSaveGameDirectory(), "Worlds");
    public static string Extension = ".blsw";
    public WorldState? WorldState;
    
    public void NewWorld(string worldName, BigInteger seed, WorldDifficulty difficulty, 
			int worldWidth, int worldHeight) 
	{
		WorldState worldData = new WorldState 
		{ 
			Name = worldName, 
			Difficulty = difficulty,
			WorldWidth = worldWidth,
			WorldHeight = worldHeight
		};
		
		// run world gen on different thread
		Task.Run(() => 
		{
		    WorldGen.Generate(seed, worldData);		
			ManagerFileHelper.New(WorldsSaveFolder, worldName, Extension, worldData);
		});
	}
	
	public bool WorldExists(string worldName) => ManagerFileHelper.Exists(WorldsSaveFolder, worldName, Extension);
	public List<WorldState> LoadAllWorlds() => ManagerFileHelper.LoadAll<WorldState>(WorldsSaveFolder, Extension);
		
	/// <summary>
	/// Selects the world state
	/// </summary>
	/// <param name="worldState"></param>
	/// <param name="host">If true, will tell <c>NetworkManager</c> to host the game</param>
	public void SelectWorld(WorldState worldState, bool host)
	{
		WorldState = worldState;
		// BlastiaGame.RequestWorldInitialization();
		
		// // hide join game menu whenever in a world
		// BlastiaGame.GetMenu<JoinGameMenu>().SetActive(false);
		// BlastiaGame.GetMenu<JoinGameMenu>()?.ToggleStatusText(false);

		// if (host)
		// {
		// 	NetworkManager.Instance?.HostGame();
		// }
	}

	public void UnselectWorld() => WorldState = null;
}