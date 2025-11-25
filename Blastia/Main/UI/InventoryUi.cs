using Blastia.Main.Entities;
using Blastia.Main.Entities.Common;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class InventoryUi : Menu
{
    private Inventory _playerInventory;
    private List<InventorySlot> _inventorySlotsUi = [];
    private World _world;
    
    // layout
    private int _rows;
    private int _columns;
    private Vector2 _slotSize;
    private Vector2 _slotSpacing;
    private Vector2 _gridStartPosition;
    private readonly Vector2 _slotIconScale = new(1.9f, 1.9f);
    
    // hotbar
    public int HotbarSlotsCount { get; private set; }
    private int _selectedHotbarSlotIndex = 1;
    private Text _selectedItemText;
    
    /// <summary>
    /// True, if the full inventory (extra rows below the hotbar) is open.
    /// <c>Menu.IsActive</c> will hide the whole inventory including the hotbar
    /// </summary>
    public bool IsFullInventoryOpen { get; private set; }

    private Texture2D _slotBackgroundTexture;
    private Texture2D? _slotHighlightedTexture;
    
    // cursor item
    private Image _cursorItemImage;
    private Text _cursorItemAmountText;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="font"></param>
    /// <param name="playerInventory">Player's full inventory</param>
    /// <param name="world"></param>
    /// <param name="hotbarStartPosition">Position for hotbar (first row)</param>
    /// <param name="rows">Total rows</param>
    /// <param name="columns">Total columns</param>
    /// <param name="slotSize">Visual slot size</param>
    /// <param name="slotSpacing"></param>
    /// <param name="slotBackgroundTexture"></param>
    /// <param name="slotHighlightTexture"></param>
    /// <param name="isFullyOpened"></param>
    /// <param name="isActive"></param>
    public InventoryUi(SpriteFont font, Inventory playerInventory, World world, Vector2 hotbarStartPosition, int rows, int columns,
        Vector2 slotSize, Vector2 slotSpacing, Texture2D slotBackgroundTexture, Texture2D? slotHighlightTexture = null, 
        bool isFullyOpened = false, bool isActive = false) : base(font, isActive, false)
    {
        _playerInventory = playerInventory;
        _world = world;
        _gridStartPosition = hotbarStartPosition;
        _rows = rows;
        _columns = columns;
        HotbarSlotsCount = columns;
        
        _slotSize = slotSize;
        _slotSpacing = slotSpacing;
        _slotBackgroundTexture = slotBackgroundTexture;
        _slotHighlightedTexture = slotHighlightTexture;

        _selectedItemText = new Text(Vector2.Zero, "None", font);
            
        _playerInventory.OnSlotChanged += OnInventorySlotUpdated;
        _playerInventory.OnCursorItemChanged += OnCursorItemChanged;
        IsFullInventoryOpen = isFullyOpened;

        _cursorItemImage = new Image(Vector2.Zero, BlastiaGame.TextureManager.Invisible());
        _cursorItemAmountText = new Text(Vector2.Zero, "", font);
        
        InitializeSlots();
    }

    public void SetSelectedHotbarSlotIndex(int index)
    {
        _selectedHotbarSlotIndex = Math.Clamp(index, 0, HotbarSlotsCount - 1);
    }

    public void ToggleFullInventoryDisplay()
    {
        IsFullInventoryOpen = !IsFullInventoryOpen;
        // ensure whole inventory is active
        Active = true;
    }

    public void OnCursorItemChanged(ItemInstance? item)
    {
        if (item != null && item.Amount > 0)
        {
            _cursorItemImage.Texture = item.Icon;
            _cursorItemAmountText.Text = item.Amount.ToString();
        }
        else
        {
            _cursorItemImage.Texture = BlastiaGame.TextureManager.Invisible();
            _cursorItemAmountText.Text = "";
        }
    }
    
    protected override void AddElements()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        // remove old inventory slots
        _inventorySlotsUi.Clear();
        Elements.RemoveAll(element => element is InventorySlot);

        var slotIndex = 0;
        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                // dont create slots exceeding capacity
                if (slotIndex >= _playerInventory.Capacity)
                {
                    continue;
                }
                
                // use sprite width for spacing
                var slotPosition = _gridStartPosition + new Vector2(
                    column * (_slotBackgroundTexture.Width * _slotSize.X + _slotSpacing.X),
                    row * (_slotBackgroundTexture.Height * _slotSize.Y + _slotSpacing.Y)
                );
                
                // no highlight texture
                var inventorySlotUi =
                    new InventorySlot(slotPosition, Font, _slotBackgroundTexture, _slotHighlightedTexture, slotIndex)
                    {
                        Scale = _slotSize,
                        IconScale = _slotIconScale
                    };
                
                // set initial item
                inventorySlotUi.SetItem(_playerInventory.GetItemAt(slotIndex));
                
                // handle slot clicks
                var currentSlotId = slotIndex;
                inventorySlotUi.OnClick = () => HandleSlotClick(currentSlotId);
                inventorySlotUi.OnRightClick = () => HandleSlotRightClick(currentSlotId);
                
                _inventorySlotsUi.Add(inventorySlotUi);
                Elements.Add(inventorySlotUi);

                slotIndex += 1;
            }
        }
        
        Console.WriteLine($"[InventoryUi] Initialized {_inventorySlotsUi.Count} slots. Inventory capacity: {_playerInventory.Capacity}, " +
                          $"rows: {_rows}, columns: {_columns} at {_gridStartPosition}");
    }

    private void HandleSlotClick(int slotIndex)
    {
        if (!IsFullInventoryOpen) return;
        
        var itemInClickedSlot = _playerInventory.GetItemAt(slotIndex);
        if (itemInClickedSlot == null && _playerInventory.CursorItem == null) return;
        
        if (itemInClickedSlot == null && _playerInventory.CursorItem != null) // put item from cursor to an empty slot
        {
            _playerInventory.SetItemAt(slotIndex, _playerInventory.CursorItem);
            _playerInventory.SetCursorItem(null);
            
            SoundEngine.PlaySound(SoundID.Grab);
        }
        else if (itemInClickedSlot != null && _playerInventory.CursorItem == null) // put item from slot to cursor
        {
            _playerInventory.SetCursorItem(itemInClickedSlot);
            _playerInventory.SetItemAt(slotIndex, null);
            
            SoundEngine.PlaySound(SoundID.Grab);
        }
        else if (itemInClickedSlot != null && _playerInventory.CursorItem != null)
        {
            if (itemInClickedSlot.BaseItem == _playerInventory.CursorItem.BaseItem) // add items from cursor to max stack
            {
                var canAdd = itemInClickedSlot.MaxStack - itemInClickedSlot.Amount;
                if (canAdd > 0)
                {
                    var toAdd = Math.Min(canAdd, _playerInventory.CursorItem.Amount);
                    
                    // update item in slot
                    itemInClickedSlot.Amount += toAdd;
                    _playerInventory.SetItemAt(slotIndex, new ItemInstance(itemInClickedSlot.BaseItem, itemInClickedSlot.Amount));
                    // update cursor item
                    _playerInventory.CursorItem.Amount -= toAdd;
                    
                    // remove if empty
                    if (_playerInventory.CursorItem.Amount <= 0)
                    {
                        _playerInventory.SetCursorItem(null);
                    }
                }
            }
            else  // swap items
            {
                var tempCursorItem = _playerInventory.CursorItem;
                _playerInventory.SetCursorItem(itemInClickedSlot);
                _playerInventory.SetItemAt(slotIndex, tempCursorItem);
            }
            
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }

    private void HandleSlotRightClick(int slotIndex)
    {
        if (!IsFullInventoryOpen) return;
        
        // put 1 item of items in slot to CursorItem
        var itemInClickedSlot = _playerInventory.GetItemAt(slotIndex);
        if (itemInClickedSlot == null) return;

        if (_playerInventory.CursorItem == null) // if cursor item empty
        {
            // remove 1 and set item
            _playerInventory.RemoveItem(slotIndex);
            _playerInventory.SetCursorItem(new ItemInstance(itemInClickedSlot.BaseItem));
            
            SoundEngine.PlaySound(SoundID.Grab);
        }
        else if (itemInClickedSlot.BaseItem == _playerInventory.CursorItem.BaseItem) // if same item is in cursor
        {
            if (_playerInventory.CursorItem.Amount < _playerInventory.CursorItem.MaxStack)
            {
                // remove 1
                _playerInventory.RemoveItem(slotIndex);
                // add 1 to cursor slot
                _playerInventory.CursorItem.Amount += 1;
            }
            
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }

    private void HandleItemDrop()
    {
        if (_playerInventory.CursorItem == null) return;
        
        var playerHalfWidth = _playerInventory.Player.Width * 0.5f;
        var playerHalfHeight = _playerInventory.Player.Height * 0.5f;
        var direction = _playerInventory.Player.SpriteDirection;
        
        var itemIconHalfWidth = _playerInventory.CursorItem.Icon.Width * 0.5f * Entity.DroppedItemScale;
        var itemIconHalfHeight = _playerInventory.CursorItem.Icon.Height * 0.5f * Entity.DroppedItemScale;
        
        var posX = _playerInventory.Player.Position.X - playerHalfWidth - itemIconHalfWidth;
        var posY = _playerInventory.Player.Position.Y - playerHalfHeight - itemIconHalfHeight;
        
        // item launch constructor
        var droppedItem = new DroppedItem(new Vector2(posX, posY), Entity.DroppedItemScale, _world, 
            _playerInventory.CursorItem.BaseItem, _playerInventory.CursorItem.Amount, (int) direction);
        BlastiaGame.RequestAddEntity(droppedItem);
        BlastiaGame.NotificationDisplay?.AddNotification($"(-) {_playerInventory.CursorItem.Amount} {_playerInventory.CursorItem.Name}", Color.IndianRed);
        
        // clear cursor item
        _playerInventory.SetCursorItem(null);
    }

    private void OnInventorySlotUpdated(int slotIndex, ItemInstance? newItem)
    {
        if (slotIndex >= 0 && slotIndex < _inventorySlotsUi.Count)
        {
            _inventorySlotsUi[slotIndex].SetItem(newItem);
        }
    }

    protected override void OnMenuActive()
    {
        base.OnMenuActive();
        RefreshAllSlots();
    }

    public override void Update()
    {
        for (int i = 0; i < _inventorySlotsUi.Count; i++)
        {
            var isHotbarSlot = i < HotbarSlotsCount;
            var slotToUpdate = _inventorySlotsUi[i];
            
            // always update hotbar slots
            if (isHotbarSlot)
            {
                slotToUpdate.Update();
            }
            else if (IsFullInventoryOpen) // main inv slots only if inv is opened
            {
                slotToUpdate.Update();
            }
        }
        
        if (_playerInventory.CursorItem != null)
        {
            _cursorItemImage.Position = BlastiaGame.CursorPosition + new Vector2(35, 20);
            _cursorItemImage.Scale = _slotIconScale;
            _cursorItemImage.Update();
            
            _cursorItemAmountText.Position = BlastiaGame.CursorPosition + new Vector2(35, 20) + _slotIconScale + new Vector2(15, 10);
            _cursorItemAmountText.Text = _playerInventory.CursorItem.Amount.ToString();
            _cursorItemAmountText.Update();
        }

        // check if clicked NOT on a slot
        if (Active && _playerInventory.CursorItem != null && BlastiaGame.HasClickedLeft)
        {
            var clickedOnSlot = HoveredOnAnySlot();

            if (!clickedOnSlot)
            {
                HandleItemDrop();
            }
        }
        
        // update selected item text
        _selectedItemText.Text = _playerInventory.Items[_selectedHotbarSlotIndex] == null 
            ? "None" 
            : _playerInventory.Items[_selectedHotbarSlotIndex]?.Name;
        
        var totalSlotsWidth = _columns * _slotBackgroundTexture.Width * _slotSize.X;
        var totalGapsWidth = Math.Max(0, _columns - 1) * _slotSpacing.X;
        var totalHotbarWidth = totalSlotsWidth + totalGapsWidth;
        var hotbarCenterX = _gridStartPosition.X + totalHotbarWidth * 0.5f;
        
        var textSize = Font.MeasureString(_selectedItemText.Text) * _selectedItemText.Scale;
        var textPositionX = hotbarCenterX - textSize.X * 0.5f;
        var textPositionY = _gridStartPosition.Y - textSize.Y - 5;
        _selectedItemText.Position = new Vector2(textPositionX, textPositionY);
        _selectedItemText.Update();
    }

    /// <summary>
    /// Checks if we hovered on any of the inventory slots
    /// </summary>
    /// <returns></returns>
    public bool HoveredOnAnySlot()
    {
        if (!Active) return false;

        var hoveredOnAnySlot = false;
        foreach (var slot in _inventorySlotsUi)
        {
            // hovered on hotbar slot
            if (slot.IsHovered && slot.SlotIndex < HotbarSlotsCount)
            {
                hoveredOnAnySlot = true;
                break;
            }
            // hovered on any other slots while inventory is open
            if (slot.IsHovered && IsFullInventoryOpen)
            {
                hoveredOnAnySlot = true;
                break;
            }
        }
        
        return hoveredOnAnySlot;
    }
    
    private void RefreshAllSlots()
    {
        for (int i = 0; i < _inventorySlotsUi.Count; i++)
        {
            if (i < _playerInventory.Capacity)
            {
                _inventorySlotsUi[i].SetItem(_playerInventory.GetItemAt(i));
            }
            else
            {
                _inventorySlotsUi[i].ClearItem();
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Active) return;

        for (int i = 0; i < _inventorySlotsUi.Count; i++)
        {
            var isHotbarSlot = i < HotbarSlotsCount;
            var slotToDraw = _inventorySlotsUi[i];
            
            // always draw hotbar slots
            if (isHotbarSlot)
            {
                slotToDraw.IsSelected = i == _selectedHotbarSlotIndex;
                slotToDraw.Draw(spriteBatch);
            }
            else if (IsFullInventoryOpen) // main inv slots only if inv is opened
            {
                slotToDraw.Draw(spriteBatch);
            }
        }

        if (_playerInventory.CursorItem != null)
        {
            _cursorItemImage.Draw(spriteBatch);
            _cursorItemAmountText.Draw(spriteBatch);
        }
        _selectedItemText.Draw(spriteBatch);
    }
}