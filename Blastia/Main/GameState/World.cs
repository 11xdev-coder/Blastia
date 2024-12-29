using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;

namespace Blastia.Main.GameState;

public class World(WorldState worldState)
{
	private WorldState _state = worldState;
	
	public bool RulerMode;
	private Vector2 _rulerStart;
	private Vector2 _rulerEnd;

	public static float GetBlocksAmount(int width, int height)
	{
		return width * height;
	}
	
	// volume of a block: 1 m3
	// mass of 1 m3 of stone: 1602 kg
	// mass of 1 m3 of dirt: 1300 kg
	// mass of all blocks  (36% stone 24% dirt 40% air): 
	public static double GetMass(int width, int height)
	{
		var amount = GetBlocksAmount(width, height);

		var stoneMass = amount * 0.36 * 1602;
		var dirtMass = amount * 0.24 * 1300;
		var mass = stoneMass + dirtMass;

		return mass;
	}

	public void SetRulerStart(Vector2 start) => _rulerStart = start;
	public void SetRulerEnd(Vector2 end) => _rulerEnd = end;
	
	public Vector2 GetRulerStart() => _rulerStart;
	public Vector2 GetRulerEnd() => _rulerEnd;

	public void Update()
	{
		BlastiaGame.RequestDebugPointDraw(GetRulerStart());
		BlastiaGame.RequestDebugPointDraw(GetRulerEnd(), 4);
	}
}