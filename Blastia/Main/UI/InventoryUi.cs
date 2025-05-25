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
    
    // hotbar
    public int HotbarSlotsCount { get; private set; }
    private int _selectedHotbarSlotIndex = -1;
    
    /// <summary>
    /// True, if the full inventory (extra rows below the hotbar) is open.
    /// <c>Menu.IsActive</c> will hide the whole inventory including the hotbar
    /// </summary>
    public bool IsFullInventoryOpen { get; private set; }

    private Texture2D _slotBackgroundTexture;
    
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
        
        _playerInventory.OnSlotChanged += OnInventorySlotUpdated;
        IsFullInventoryOpen = isFullyOpened;
        
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
                    new InventorySlot(slotPosition, Font, _slotBackgroundTexture, slotIndex: slotIndex)
                    {
                        Scale = _slotSize,
                        IconScale = new Vector2(1.9f, 1.9f)
                    };
                
                // set initial item
                inventorySlotUi.SetItem(_playerInventory.GetItemAt(slotIndex));
                
                // handle slot clicks
                var currentSlotId = slotIndex;
                inventorySlotUi.OnClick = () => HandleSlotClick(currentSlotId);
                
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
        // TODO: drag and drop
        
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
                slotToDraw.Draw(spriteBatch);
            }
            else if (IsFullInventoryOpen) // main inv slots only if inv is opened
            {
                slotToDraw.Draw(spriteBatch);
            }
        }
    }
}