using BlasterMaster.Main;
using BlasterMaster.Main.UI;
using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public abstract class CreationMenu : Menu 
{
	protected Input? NameInput;
	protected Text? ExistsText;
	
	public CreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		AddCommonElements();
	}
	
	private void AddCommonElements() 
	{
		Text nameLabel = new Text(Vector2.Zero, GetNameLabel(), Font) 
		{
			HAlign = 0.5f,
			VAlign = 0.4f
		};
		Elements.Add(nameLabel);
		
		NameInput = new Input(Vector2.Zero, Font, true) 
		{
			HAlign = 0.5f,
			VAlign = 0.45f
		};
		Elements.Add(NameInput);
		
		ExistsText = new Text(Vector2.Zero, GetExistsText(), Font) 
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			Alpha = 0f,
			DrawColor = BlasterMasterGame.ErrorColor
		};
		Elements.Add(ExistsText);
		
		Button createButton = new Button(Vector2.Zero, "Create", Font, Create)
		{
			HAlign = 0.5f,
			VAlign = CreateButtonVAlign
		};
		Elements.Add(createButton);
		
		Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
		{
			HAlign = 0.5f,
			VAlign = CreateButtonVAlign + 0.05f
		};
		Elements.Add(backButton);
	}
	
	protected abstract string GetNameLabel();
	protected abstract string GetExistsText();
	protected abstract void Create();
	protected abstract void Back();
	protected abstract void UpdateSpecific();
	
	protected virtual float CreateButtonVAlign { get; set; } = 0.6f;

	public override void Update()
	{
		base.Update();
		
		if (ExistsText == null) return;
		ExistsText.DrawColor = BlasterMasterGame.ErrorColor;
		
		UpdateSpecific();
	}
	
	protected void ShowExistsError() 
	{
		if (ExistsText == null) return;
		
		ExistsText.Alpha = 1f;
		ExistsText.LerpAlphaToZero = true;
	}
}