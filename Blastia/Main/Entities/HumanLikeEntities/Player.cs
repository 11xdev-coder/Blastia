using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.Entities.HumanLikeEntities;

[Entity(Id = EntityID.Player)]
public class Player : HumanLikeEntity
{
	/// <summary>
	/// If True, player will play walking animation and disable all other logic
	/// </summary>
	public bool IsPreview { get; set; }
	public bool LocallyControlled { get; private set; }

	protected override bool FlipSpriteHorizontallyOnKeyPress => true;

	private const float ArmMaxAngle = 20;
	private const float LegMaxAngle = 25;
	private const float WalkingAnimationDuration = 0.4f;

	public Camera? Camera { get; set; }
	private World? _world;

	protected override bool ApplyGravity => true;
	public override float Height => 1.8f;
	public override float Width => 0.9f;
	protected override float Mass => 46f;
	
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
	private int _selectedHotbarSlot = -1;

	public Player(Vector2 position, World? world, float initialScaleFactor = 1f, bool myPlayer = false) : 
		base(position, initialScaleFactor, EntityID.Player, new Vector2(0, -24), Vector2.Zero, 
			new Vector2(-13, -21), new Vector2(13, -21), new Vector2(-6, 21), 
			new Vector2(10, 21))
	{
		_world = world;
		PlayerInventory = new Inventory(InventoryCapacity, this);
		LocallyControlled = myPlayer;
		
		if (myPlayer)
		{
			Camera = new Camera(position)
			{
				DrawWidth = 240 * Block.Size,
				DrawHeight = 135 * Block.Size
			};
		}
		MovementSpeed = 80f;
		AccelerationFactor = 10f;
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
			HandleMovement();
			HandleMouseClicks();
			HandleItemInteraction();
			
			if (KeyboardHelper.IsKeyJustPressed(Keys.Escape) && BlastiaGame.PlayerInventoryUiMenu != null)
			{
				BlastiaGame.PlayerInventoryUiMenu.ToggleFullInventoryDisplay();
			}
			UpdateHotbarSelection();
		}
	}

	private void UpdateCamera()
	{
		Camera?.Update();
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
		
		MovementVector.X = MathHelper.Lerp(MovementVector.X, targetHorizontalSpeed, 
			AccelerationFactor * (float) BlastiaGame.GameTimeElapsedSeconds);
		
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
	
	private void HandleMouseClicks()
	{
		var currentWorld = PlayerManager.Instance.SelectedWorld;
		if (currentWorld == null) return;
			
		if (BlastiaGame.HasClickedRight)
		{
			var pos = GetCoordsForBlockPlacement();
			currentWorld.SetTile((int) pos.X, (int) pos.Y, 1);
		}
		if (BlastiaGame.HasClickedLeft)
		{
			var pos = GetCoordsForBlockPlacement();
			currentWorld.SetTile((int) pos.X, (int) pos.Y, 0);
		}
	}

	private void HandleItemInteraction()
	{
		if (_world == null) return;
		
		var droppedItems = new List<DroppedItem>(_world.GetDroppedItems());

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
			if (droppedItem.CanBePickedUp(this))
			{
				if (GetBounds().Intersects(droppedItem.GetBounds()))
				{
					var amountPickedUp = PlayerInventory.AddItem(droppedItem.Item, droppedItem.Amount);
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
		if (BlastiaGame.PlayerInventoryUiMenu == null) return;
		
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
				_selectedHotbarSlot = i;
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
				_selectedHotbarSlot += 1;
				if (_selectedHotbarSlot >= hotbarSlots)
				{
					_selectedHotbarSlot = 0;
				}
			}
			else if (scrollDelta > 0)
			{
				_selectedHotbarSlot -= 1;
				if (_selectedHotbarSlot <= -1)
				{
					_selectedHotbarSlot = hotbarSlots - 1;
				}
			}
		}
		
		BlastiaGame.PlayerInventoryUiMenu.SetSelectedHotbarSlotIndex(_selectedHotbarSlot);
	}

	/// <summary>
	/// Update called when IsPreview = true
	/// </summary>
	private void PreviewUpdate()
	{
		WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
	}
}