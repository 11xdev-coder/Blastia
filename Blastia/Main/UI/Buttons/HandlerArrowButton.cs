using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

// TODO: refactor whatever bullshit thisi s
/// <summary>
/// Accepts handler, creates 2 arrows that will go to next/previous item.
/// <para>T -> ListHandler's class</para>
/// </summary>
public class HandlerArrowButton<T> : Button, IValueStorageUi<ListHandler<T>>
{
	public Func<ListHandler<T>>? GetValue { get; set; }
	public Action<ListHandler<T>>? SetValue { get; set; }
	
	public Button? LeftButton;
	public Button? RightButton;
	private float _arrowSpacing;
	private ListHandler<T>? _handler;

	private float _leftArrowSizeY;
	private float _rightArrowSizeX;
	
	public HandlerArrowButton(Vector2 position, string text, SpriteFont font, Action onClick, 
		float arrowSpacing, ListHandler<T>? handler, Action<Action>? subscribeToEvent = null) : base(position, text, font, onClick)
	{
		_arrowSpacing = arrowSpacing;
		_handler = handler;

		LeftButton = new Button(Vector2.Zero, "<", font, OnLeftArrowClick)
		{
			AffectedByAlignOffset = false
		};
		RightButton = new Button(Vector2.Zero, ">", font, OnRightArrowClick)
		{
			AffectedByAlignOffset = false
		};
		_leftArrowSizeY = font.MeasureString(LeftButton.Text).Y;
		_rightArrowSizeX = font.MeasureString(RightButton.Text).Y;
		UpdateArrowPositions();
		UpdateButtonText();

		if (subscribeToEvent != null) subscribeToEvent(UpdateLabel);
	}

    public override void Update()
    {
        base.Update();
        LeftButton?.Update();
        RightButton?.Update();
    }

	public override void UpdateBounds()
	{
		base.UpdateBounds();
		UpdateArrowPositions();
	}

	private void UpdateArrowPositions()
	{
		if (LeftButton == null || RightButton == null) return;
		LeftButton.Position = new Vector2(Bounds.Left - _arrowSpacing * 2, Bounds.Center.Y - _leftArrowSizeY / 2);		
		RightButton.Position = new Vector2(Bounds.Right + _arrowSpacing, Bounds.Center.Y - _rightArrowSizeX / 2);
	}

	private void UpdateButtonText()
	{
		if (_handler == null) return;
		
		Text = $"{InitialText}: {_handler.GetString()}";
	}

	private void OnLeftArrowClick()
	{
		if (_handler == null) return;
		
		_handler.Previous();
		UpdateButtonText();
		OnClick?.Invoke();
	}

	private void OnRightArrowClick()
	{
		if (_handler == null) return;
		
		_handler.Next();
		UpdateButtonText();
		OnClick?.Invoke();
	}

	public void UpdateLabel()
	{
		UpdateButtonText();
	}

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        LeftButton?.Draw(spriteBatch);
        RightButton?.Draw(spriteBatch);
    }
}