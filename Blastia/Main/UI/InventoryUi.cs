using Blastia.Main.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class InventoryUi : Menu
{
    private Inventory _playerInventory;
    private List<InventorySlot> _inventorySlotsUi = [];
    
    // layout
    private int _rows;
    private int _columns;
    private Vector2 _slotSize;
    private Vector2 _slotSpacing;
    private Vector2 _gridStartPosition;
    private readonly Vector2 _slotIconScale = new(1.9f, 1.9f);
    
    // hotbar
    public int HotbarSlotsCount { get; private set; }
    private int _selectedHotbarSlotIndex = -1;
    
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
    /// <param name="hotbarStartPosition">Position for hotbar (first row)</param>
    /// <param name="rows">Total rows</param>
    /// <param name="columns">Total columns</param>
    /// <param name="slotSize">Visual slot size</param>
    /// <param name="slotSpacing"></param>
    /// <param name="slotBackgroundTexture"></param>
    /// <param name="slotHighlightTexture"></param>
    /// <param name="isFullyOpened"></param>
    /// <param name="isActive"></param>
    public InventoryUi(SpriteFont font, Inventory playerInventory, Vector2 hotbarStartPosition, int rows, int columns,
        Vector2 slotSize, Vector2 slotSpacing, Texture2D slotBackgroundTexture, Texture2D? slotHighlightTexture = null, 
        bool isFullyOpened = false, bool isActive = false) : base(font, isActive, false)
    {
        _playerInventory = playerInventory;
        _gridStartPosition = hotbarStartPosition;
        _rows = rows;
        _columns = columns;
        HotbarSlotsCount = columns;
        
        _slotSize = slotSize;
        _slotSpacing = slotSpacing;
        _slotBackgroundTexture = slotBackgroundTexture;
        _slotHighlightedTexture = slotHighlightTexture;
        
        _playerInventory.OnSlotChanged += OnInventorySlotUpdated;
        _playerInventory.OnCursorItemChanged += OnCursorItemChanged;
        IsFullInventoryOpen = isFullyOpened;

        _cursorItemImage = new Image(Vector2.Zero, BlastiaGame.InvisibleTexture);
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
            _cursorItemImage.Texture = BlastiaGame.InvisibleTexture;
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
        _playerInventory.SetCursorItem(itemInClickedSlot);
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
        }
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
        base.Update();
        if (_playerInventory.CursorItem != null)
        {
            _cursorItemImage.Position = BlastiaGame.CursorPosition + new Vector2(35, 20);
            _cursorItemImage.Scale = _slotIconScale;
            _cursorItemImage.Update();
            
            _cursorItemAmountText.Position = BlastiaGame.CursorPosition + new Vector2(35, 20) + _slotIconScale + new Vector2(10, 5);
            _cursorItemAmountText.Text = _playerInventory.CursorItem.Amount.ToString();
            _cursorItemAmountText.Update();
        }
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
    }
}