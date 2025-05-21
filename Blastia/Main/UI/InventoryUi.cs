using Blastia.Main.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class InventoryUi : Menu
{
    private Inventory _playerInventory;
    private List<InventorySlot> _inventorySlotsUi;
    
    // layout
    private int _rows;
    private int _columns;
    private Vector2 _slotSize;
    private Vector2 _slotSpacing;
    private Vector2 _gridStartPosition;
    
    public bool IsHotbar { get; private set; }

    private Texture2D _slotBackgroundTexture;
    
    protected InventoryUi(SpriteFont font, Inventory playerInventory, Vector2 gridStartPosition, int rows, int columns,
        Vector2 slotSize, Vector2 slotSpacing, Texture2D slotBackgroundTexture, bool isHotbar = false, bool isActive = false) : base(font, isActive)
    {
        _playerInventory = playerInventory;
        _gridStartPosition = gridStartPosition;
        _rows = rows;
        _columns = columns;
        _slotSize = slotSize;
        _slotSpacing = slotSpacing;
        _slotBackgroundTexture = slotBackgroundTexture;
        IsHotbar = isHotbar;
        
        _inventorySlotsUi = [];
        _playerInventory.OnSlotChanged += OnInventorySlotUpdated;
        
        InitializeSlots();
    }

    protected override void AddElements()
    {
        foreach (var slotUi in _inventorySlotsUi)
        {
            Elements.Add(slotUi);
        }
    }

    private void InitializeSlots()
    {
        // remove old inventory slots
        _inventorySlotsUi.Clear();
        Elements.RemoveAll(element => element is InventorySlot);

        int slotIndex = 0;
        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                // for main inventory dont create slots exceeding capacity
                if (slotIndex >= _playerInventory.Capacity && !IsHotbar)
                {
                    // fixed slots for hotbar
                    if (IsHotbar && slotIndex >= _columns) break;

                    if (!IsHotbar) break;
                }
                
                // use sprite width for spacing
                Vector2 slotPosition = _gridStartPosition + new Vector2(
                    column * (_slotBackgroundTexture.Width * _slotSize.X + _slotSpacing.X),
                    row * (_slotBackgroundTexture.Height * _slotSize.Y + _slotSpacing.Y)
                );
                
                // no highlight texture
                var inventorySlotUi =
                    new InventorySlot(slotPosition, Font, _slotBackgroundTexture, slotIndex: slotIndex)
                    {
                        Scale = _slotSize,
                        IconScale = Vector2.One
                    };
                
                // set initial item
                inventorySlotUi.SetItem(_playerInventory.GetItemAt(slotIndex));
                
                // handle slot clicks
                var currentSlotId = slotIndex;
                inventorySlotUi.OnClick = () => HandleSlotClick(currentSlotId);
                
                _inventorySlotsUi.Add(inventorySlotUi);
                Elements.Add(inventorySlotUi);
                
                slotIndex++;
                if (IsHotbar && slotIndex >= _columns) break; // only one row if hotbar
            }
            
            if (IsHotbar && slotIndex >= _columns) break;
            if (!IsHotbar && slotIndex >= _playerInventory.Capacity) break;
        }
    }

    private void HandleSlotClick(int slotIndex)
    {
        // TODO
    }

    private void OnInventorySlotUpdated(int slotIndex, ItemInstance? newItem)
    {
        // TODO
    }
}