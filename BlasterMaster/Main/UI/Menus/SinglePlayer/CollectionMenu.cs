using System.Collections.Specialized;
using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public abstract class CollectionMenu : Menu
{
	protected ScrollableArea? Collection;
	
	public CollectionMenu(SpriteFont font, bool isActive = false) : base(font, isActive) 
	{
		AddCommonElements();
	}
	
	protected abstract string GetCreateButtonLabel();	
	protected abstract void Create();
	protected abstract void Back();
	protected abstract IEnumerable<object> LoadItems();
	protected abstract void SelectItem(object item);
	
	private void AddCommonElements() 
	{
		Viewport collectionViewport = new Viewport(2000, 450);
		Collection = new ScrollableArea(Vector2.Zero, collectionViewport) 
		{
			HAlign = 0.5f,
			VAlign = 0.55f
		};
		Elements.Add(Collection);
		
		Button createButton = new Button(Vector2.Zero, GetCreateButtonLabel(), Font, Create)
		{
			HAlign = 0.5f,
			VAlign = 0.85f
		};
		Elements.Add(createButton);
		
		Button backButton = new Button(Vector2.Zero, "Back", Font, Back) 
		{
			HAlign = 0.5f,
			VAlign = 0.9f
		};
		Elements.Add(backButton);
	}
	
	public override void OnMenuActive()
	{
		base.OnMenuActive();
		
		if (Collection == null) return;
		Collection.ClearChildren();
		
		// load all states as buttons
		var items = LoadItems();
		foreach (var item in items) 
		{
			// PlayerState and WorldState ToString() returns their Name
			// if item.ToString() is null, assign it to ""
			string? itemString = item.ToString();
			if (itemString == null) itemString = "name error";
			
			Button itemButton = new Button(Vector2.Zero, itemString, Font, () => SelectItem(item));
			Collection.AddChild(itemButton);
		}
	}
}