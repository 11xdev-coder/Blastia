using Blastia.Main.Blocks;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Networking;
using Blastia.Main.Sounds;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NAudio.Dsp;
using Steamworks;

namespace Blastia.Main.Entities.HumanLikeEntities;

[Entity(Id = EntityID.Player)]
public class Player : HumanLikeEntity
{
	public CSteamID SteamId;
	public string Name = "";
	
	/// <summary>
	/// If True, player will play walking animation and disable all other logic
	/// </summary>
	public bool IsPreview { get; set; }

	private bool _isBlocked;
	protected override bool FlipSpriteHorizontallyOnKeyPress => !_isBlocked;

	private const float ArmMaxAngle = 20;
	private const float LegMaxAngle = 25;
	private const float WalkingAnimationDuration = 0.4f;

	public Camera? Camera { get; set; }
	public World? World;

	protected override bool ApplyGravity => true;
	public override float Height => 1.8f;
	public override float Width => 0.9f;
	protected override float Mass => 46f;

	public override float MaxLife => 100f;
	public override SoundID HitSound => SoundID.FleshHit;
	private float _flickerTimer;
	private bool _isDrawing;

	private const float MinJumpVelocity = 200f;
	private const float MaxJumpVelocity = 320f;
	private const float MaxChargeTime = 0.35f;
	private float _jumpCharge;
	
	// pulling
	private const float PickupRadius = 30f;
	private const float PickupRadiusSquared = PickupRadius * PickupRadius;
	
	// inventory
	public Inventory PlayerInventory { get; private set; }
	public const int InventoryRows = 4;
	public const int InventoryColumns = 9;
	public const int HotbarSlotsCount = 9;
	private const int InventoryCapacity = InventoryRows * InventoryColumns;
	public int SelectedHotbarSlot = -1;

	public Player(Vector2 position, World? world, float initialScaleFactor = 1f, bool myPlayer = false) : 
		base(position, initialScaleFactor, EntityID.Player, new Vector2(0, -24), Vector2.Zero, 
			new Vector2(-13, -21), new Vector2(13, -21), new Vector2(-6, 21), 
			new Vector2(10, 21))
	{
		SteamId = CSteamID.Nil;
		World = world;
		PlayerInventory = new Inventory(InventoryCapacity, this);
		
		if (myPlayer)
		{
			LocallyControlled = true;
			
			if (NetworkManager.Instance != null && NetworkManager.Instance.IsSteamInitialized)
			{
				SteamId = NetworkManager.Instance.MySteamId;
				Name = SteamFriends.GetPersonaName();
			}
			
			Camera = new Camera(position)
			{
				DrawWidth = 240 * Block.Size,
				DrawHeight = 135 * Block.Size
			};
		}
		else 
		{
		    LocallyControlled = false;
		}
		
		MovementSpeed = 80f;
		TimeToMaxSpeed = 0.2f;
	}

	/// <summary>
	/// Returns <c>NetworkPlayer</c> containing all basic information about this player
	/// </summary>
	/// <returns></returns>
	public NetworkPlayer GetNetworkData()
	{
		return new NetworkPlayer
		{
			Id = GetId(),
			Position = Position,
			MovementVector = MovementVector,
			Life = Life,
			IsGrounded = IsGrounded,
			CanJump = CanJump,
			NetworkTimestamp = NetworkTimestamp,
			SteamId = SteamId,
			Name = Name,
			SelectedSlot = SelectedHotbarSlot
		};
	}

	protected override void OnLifeChanged()
	{
		base.OnLifeChanged();
		
		if (BlastiaGame.PlayerStatsMenu != null)
			BlastiaGame.PlayerStatsMenu.UpdateHealth(Life, MaxLife);
	}

	protected override void OnHit()
	{
		base.OnHit();
		
		// reset the flicker timer for better visuals
		_flickerTimer = 0f;
		_isDrawing = false;
	}

	protected override void OnKill()
	{
		// for player, instead of removing him, respawn
		SoundEngine.PlaySound(SoundID.PlayerDeath);
		Respawn();
	}

	private void Respawn()
	{
		if (PlayerNWorldManager.Instance.SelectedWorld == null) return;

		ImmunityTimer = 3f; // 3 seconds of immunity after respawn
		VisualFlickerTimer = 3f;
		Life = MaxLife;
		Position = PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint();
	}
	
	private bool ShouldBlockInput()
	{
		// typing in sign edit menu:
		return BlastiaGame.InGameSignEditMenu != null && BlastiaGame.InGameSignEditMenu.Active
			&& BlastiaGame.InGameSignEditMenu.SignText != null && BlastiaGame.InGameSignEditMenu.SignText.IsFocused;
	}

	public override void Update()
	{
		if (IsPreview) PreviewUpdate();
		else RegularUpdate();
		
		base.Update();
		
		if (!IsPreview) UpdateCamera();
	}

	/// <summary>
	/// Update when IsPreview = false
	/// </summary>
	private void RegularUpdate()
	{
		if (LocallyControlled)
		{
			if (!ShouldBlockInput())
			{
				_isBlocked = false;
				HandleMovement();
				HandleMouseClicks();
			}
			else
			{
				_isBlocked = true;
				StopAnimations();
			}
			
			HandleItemInteraction();
			
			if (KeyboardHelper.IsKeyJustPressed(Keys.Escape) && BlastiaGame.PlayerInventoryUiMenu != null)
			{
				BlastiaGame.PlayerInventoryUiMenu.ToggleFullInventoryDisplay();
			}
			UpdateHotbarSelection();

			UpdateSignHoverText();
		}
		else if (NetworkManager.Instance != null && NetworkManager.Instance.IsHost)
		{
			if (MovementVector != Vector2.Zero)
			{
				WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
			}
			else
			{
				StopAnimations();
			}
		}
	}

	private void UpdateCamera()
	{
		if (Camera == null) return;
		
		Camera.IsPlayerBlocked = _isBlocked;
		Camera.Update();
		MakeCameraFollow();
	}

	private void HandleMovement()
	{
		Vector2 directionVector = Vector2.Zero;
		KeyboardHelper.AccumulateValueFromMap(HorizontalMovementMap, ref directionVector);

		// less speed when in air
		var airMultiplier = 1f;
		if (!IsGrounded) airMultiplier = 0.4f;

		var targetHorizontalSpeed = 0f;
		if (directionVector != Vector2.Zero)
		{
			WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
			targetHorizontalSpeed = directionVector.X * MovementSpeed * airMultiplier;
		}
		else
		{
			StopAnimations();
		}
		
		// fps independent acceleration (still not perfect)
		var deltaTime = (float)BlastiaGame.GameTimeElapsedSeconds;
		var remainingTime = deltaTime;
  
		while (remainingTime > 0.0001f)
		{
			var dt = Math.Min(FixedTimeStep, remainingTime);
			remainingTime -= dt;
    
			// max possible acceleration
			var maxAccelPerSecond = MovementSpeed / TimeToMaxSpeed;
			var maxAccelThisStep = maxAccelPerSecond * dt;
    
			// by how much do we need to change velocity
			float speedDiff = targetHorizontalSpeed - MovementVector.X;
			float absSpeedDiff = Math.Abs(speedDiff);
    
			// apply
			if (absSpeedDiff <= maxAccelThisStep)
			{
				// close enough -> set directly
				MovementVector.X = targetHorizontalSpeed;
			}
			else
			{
				MovementVector.X += Math.Sign(speedDiff) * maxAccelThisStep;
			}
		}

		var jumped = false;
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.Space) && CanJump)
		{
			_jumpCharge += (float) BlastiaGame.GameTimeElapsedSeconds;
		}
		else if (BlastiaGame.KeyboardState.IsKeyUp(Keys.Space) &&
		         BlastiaGame.PreviousKeyboardState.IsKeyDown(Keys.Space) && CanJump)
		{
			_jumpCharge = Math.Min(_jumpCharge, MaxChargeTime);
			float chargeRatio = _jumpCharge / MaxChargeTime;
			var boostedJump = MathHelper.Lerp(MinJumpVelocity, MaxJumpVelocity, chargeRatio);
			
			var jumpHeight = boostedJump;
			MovementVector.Y = -jumpHeight;
			_jumpCharge = 0;
			jumped = true;
		}
	}

	private Vector2 GetCoordsForBlockPlacement()
	{
		if (Camera == null) return Vector2.Zero;
		
		var worldPos = Camera.ScreenToWorld(BlastiaGame.CursorPosition);
		var posX = (int) Math.Floor(worldPos.X / Block.Size) * Block.Size;
		var posY = (int) Math.Floor(worldPos.Y / Block.Size) * Block.Size;
		
		return new Vector2(posX, posY);
	}
	
	/// <summary>
	/// Handles item use and block breaking
	/// </summary>
	private void HandleMouseClicks()
	{
		// dont place blocks if we are hovered on ui
		if (BlastiaGame.IsHoveredOnAnyUi) return;

		var selectedItem = PlayerInventory.GetItemAt(SelectedHotbarSlot);
		var currentWorld = PlayerNWorldManager.Instance.SelectedWorld;
		if (currentWorld == null) return;
		
		if (BlastiaGame.HasClickedRight)
		{
			var pos = GetCoordsForBlockPlacement();
			// cant place anything on ground blocks
			if (currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Ground) != BlockId.Air)
				return;
			
			// can place liquid on furniture (but cant place liquid on liquid!)
			if (selectedItem is {BaseItem: PlaceableItem placeableLiquid} && 
			    StuffRegistry.GetBlock(placeableLiquid.BlockId)?.GetLayer() == TileLayer.Liquid
			    && currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Furniture) != BlockId.Air
			    && currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Liquid) == BlockId.Air)
			{
				PlaceBlock(currentWorld, pos, placeableLiquid);
			}
			// can place furniture on liquids (but cant place furniture on furniture!)
			else if (selectedItem is {BaseItem: PlaceableItem placeableFurniture} && 
			    StuffRegistry.GetBlock(placeableFurniture.BlockId)?.GetLayer() == TileLayer.Furniture
			    && currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Liquid) != BlockId.Air
			    && currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Furniture) == BlockId.Air)
			{
				PlaceBlock(currentWorld, pos, placeableFurniture);
			}
			// can place anything on air
			else if (selectedItem is {BaseItem: PlaceableItem anyPlaceable} &&
			         currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Ground) == BlockId.Air &&
			         currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Liquid) == BlockId.Air &&
			         currentWorld.GetTile((int) pos.X, (int) pos.Y, TileLayer.Furniture) == BlockId.Air)
			{
				PlaceBlock(currentWorld, pos, anyPlaceable);
			}
			else
			{
				// no item selected / no air -> interact
				InteractWithBlock(currentWorld, pos);
			}
		}
		if (BlastiaGame.IsHoldingLeft)
		{
			var pos = GetCoordsForBlockPlacement();
			// can only break ground and furniture
			var groundInst = currentWorld.GetBlockInstance((int) pos.X, (int) pos.Y, TileLayer.Ground);
			if (groundInst is {Block.IsBreakable: true})
				groundInst.DoBreaking(pos, this);
			
			var furnitureInst = currentWorld.GetBlockInstance((int) pos.X, (int) pos.Y, TileLayer.Furniture);
			if (furnitureInst is {Block.IsBreakable: true}) 
				furnitureInst.DoBreaking(pos, this);
		}
	}

	private void PlaceBlock(WorldState worldState, Vector2 position, PlaceableItem placeable)
	{
		var placeableBlock = StuffRegistry.GetBlock(placeable.BlockId);
		if (placeableBlock == null) return;
		
		var placeableLayer = placeableBlock.GetLayer();
		worldState.SetTile((int) position.X, (int) position.Y, placeable.BlockId, placeableLayer, this);
		PlayerInventory.RemoveItem(SelectedHotbarSlot);
		
		// return empty bucket if specified
		if (placeable.EmptyBucketId > 0)
		{
			var emptyBucket = StuffRegistry.GetItem(placeable.EmptyBucketId);
			if (emptyBucket != null)
				PlayerInventory.AddItem(emptyBucket);
		}
		
		// play sound if specified
		if (!string.IsNullOrEmpty(placeable.PlaceSound))
		{
			if (Enum.TryParse<SoundID>(placeable.PlaceSound, out var soundId))
				SoundEngine.PlaySound(soundId);
		}
	}

	private void InteractWithBlock(WorldState worldState, Vector2 position)
	{
		if (World == null) return;
		
		foreach (TileLayer layer in Enum.GetValues(typeof(TileLayer)))
		{
			var blockInstance = worldState.GetBlockInstance((int) position.X, (int) position.Y, layer);
			if (blockInstance == null) continue;

			bool succeeded;
			// only do right click logic for first found block
			// clicked on liquid with empty bucket selected -> scoop it
			var selectedItem = PlayerInventory.GetItemAt(SelectedHotbarSlot);
			if (selectedItem != null && selectedItem.Id == ItemId.EmptyBucket && 
			    blockInstance.Block is LiquidBlock liquid && 
			    liquid.FlowLevel >= 1 && liquid.BucketItemId > 0)
			{
				// has a bucket item to give -> remove this block and give the bucket
				var liquidBucketItem = StuffRegistry.GetItem(liquid.BucketItemId);
				if (liquidBucketItem != null)
				{
					// remove empty bucket, give bucket with liquid
					PlayerInventory.RemoveItem(SelectedHotbarSlot);
					PlayerInventory.AddItem(liquidBucketItem);
					worldState.SetTile((int) position.X, (int) position.Y, 0, TileLayer.Liquid, this);
				}

				succeeded = true;
			}
			else // else -> do normal right click
			{
				succeeded = blockInstance.OnRightClick(World, position, this); 
			}

			// if any of the right clicks succeeded, dont check other layers
			if (succeeded) break;
		}
		
	}

	private void HandleItemInteraction()
	{
		if (World == null) return;
		
		var droppedItems = new List<DroppedItem>(World.GetDroppedItems());

		foreach (var droppedItem in droppedItems)
		{
			if (droppedItem.Item == null || droppedItem.Amount <= 0) continue;
			
			// vacuum (pull)
			var distanceSquared = Vector2.DistanceSquared(Position, droppedItem.Position);
			if (distanceSquared < PickupRadiusSquared)
			{
				// not pulled
				if (!droppedItem.IsBeingPulled || droppedItem.PullTargetPlayer == null)
				{
					droppedItem.StartPull(this);
				}
				else if (droppedItem.IsBeingPulled && droppedItem.PullTargetPlayer != this) // pulled by another player
				{
					
				}
				else if (droppedItem.IsBeingPulled && droppedItem.PullTargetPlayer == this) // pulled by this player
				{
					droppedItem.StartPull(this);
				}
			}
			else
			{
				// pulled by this player but out of range
				if (droppedItem.IsBeingPulled && droppedItem.PullTargetPlayer == this)
				{
					droppedItem.StopPull();
				}
			}

			// pickup
			if (droppedItem.CanBePickedUpBy(this))
			{
				if (GetBounds().Intersects(droppedItem.GetBounds()))
				{
					var amountPickedUp = PlayerInventory.AddItem(droppedItem.Item, droppedItem.Amount);
					
					BlastiaGame.NotificationDisplay?.AddNotification($"(+) {droppedItem.Amount} {droppedItem.Item.Name}", Color.GreenYellow);
					SoundEngine.PlaySound(SoundID.Grab);
					
					if (amountPickedUp > 0)
					{
						droppedItem.ReduceAmount(amountPickedUp);
					}
				}
			}
		}
	}

	private void MakeCameraFollow()
	{
		if (Camera == null)
			return;

		float effectiveViewWidth = Camera.DrawWidth / Camera.CameraScale;
		float effectiveViewHeight = Camera.DrawHeight / Camera.CameraScale;

		Camera.Position = new Vector2(
			Position.X - effectiveViewWidth * 0.5f,
			Position.Y - effectiveViewHeight * 0.5f
		);
	}

	private void UpdateHotbarSelection()
	{
		if (BlastiaGame.PlayerInventoryUiMenu == null || _isBlocked) return;
		
		Keys[] hotbarKeys =
		[
			Keys.D1, Keys.D2, Keys.D3,
			Keys.D4, Keys.D5, Keys.D6,
			Keys.D7, Keys.D8, Keys.D9
		];

		for (int i = 0; i < hotbarKeys.Length; i++)
		{
			if (KeyboardHelper.IsKeyJustPressed(hotbarKeys[i]))
			{
				SelectedHotbarSlot = i;
				return;
			}
		}
		
		// scroll wheel
		var scrollDelta = BlastiaGame.ScrollWheelDelta;

		if (scrollDelta != 0)
		{
			var hotbarSlots = BlastiaGame.PlayerInventoryUiMenu.HotbarSlotsCount;

			if (scrollDelta < 0)
			{
				SelectedHotbarSlot += 1;
				if (SelectedHotbarSlot >= hotbarSlots)
				{
					SelectedHotbarSlot = 0;
				}
			}
			else if (scrollDelta > 0)
			{
				SelectedHotbarSlot -= 1;
				if (SelectedHotbarSlot <= -1)
				{
					SelectedHotbarSlot = hotbarSlots - 1;
				}
			}
		}
		
		BlastiaGame.PlayerInventoryUiMenu.SetSelectedHotbarSlotIndex(SelectedHotbarSlot);
	}
	
	private void UpdateSignHoverText()
	{
		// sign hover tooltip
		var worldState = PlayerNWorldManager.Instance.SelectedWorld;
		if (worldState != null && Camera != null)
		{
			var pos = GetCoordsForBlockPlacement();
			var tilePos = new Vector2((int) pos.X, (int) pos.Y);
			
			if (worldState.SignTexts.TryGetValue(tilePos, out var signText))
			{
				BlastiaGame.TooltipDisplay?.SetHoverText(signText);
			}
		}
	}

	/// <summary>
	/// Update called when IsPreview = true
	/// </summary>
	private void PreviewUpdate()
	{
		WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
	}

	public override void Draw(SpriteBatch spriteBatch, Vector2 position, float bodyPartScale = 1)
	{
		if (VisualFlickerTimer > 0)
		{
			_flickerTimer += (float) BlastiaGame.GameTimeElapsedSeconds;

			if (_flickerTimer >= 0.2f)
			{
				_flickerTimer = 0f;
				_isDrawing = !_isDrawing;
			}
			
			if (_isDrawing)
			{
				base.Draw(spriteBatch, position, bodyPartScale);
			}
		}
		else
		{
			base.Draw(spriteBatch, position, bodyPartScale);
		}
	}
}