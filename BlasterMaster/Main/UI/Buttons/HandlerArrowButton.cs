using BlasterMaster.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Buttons;

/// <summary>
/// Accepts handler, creates 2 arrows that will go to next/previous item.
/// <para>T -> ListHandler's class</para>
/// </summary>
public class HandlerArrowButton<T> : Button
{
	public Button? LeftButton;
	public Button? RightButton;

	private Vector2 _leftArrowPosition;
	private Vector2 _rightArrowPosition;
	private float _arrowSpacing;
	private ListHandler<T>? _handler;

	private float _leftArrowSizeY;
	private float _rightArrowSizeX;
	
	public HandlerArrowButton(Vector2 position, string text, SpriteFont font, Action onClick, 
		float arrowSpacing, ListHandler<T>? handler) : base(position, text, font, onClick)
	{
		_arrowSpacing = arrowSpacing;
		_handler = handler;
		
		LeftButton = new Button(_leftArrowPosition, "<", font, OnLeftArrowClick);
		RightButton = new Button(_rightArrowPosition, ">", font, OnRightArrowClick);
		
		_leftArrowSizeY = font.MeasureString(LeftButton.Text).Y;
		_rightArrowSizeX = font.MeasureString(RightButton.Text).Y;
		UpdateArrowPositions();
		UpdateButtonText();
	}

	public override void UpdateBounds()
	{
		base.UpdateBounds();
		UpdateArrowPositions();
	}

	/// <summary>
	/// Adds LeftArrow, this button and RightArrow to the elements list
	/// </summary>
	/// <param name="elements"></param>
	public void AddToElements(List<UIElement> elements)
	{
		if (LeftButton != null) elements.Add(LeftButton);
		elements.Add(this);
		if (RightButton != null) elements.Add(RightButton);
	}

	private void UpdateArrowPositions()
	{
		_leftArrowPosition = new Vector2(Bounds.Left - _arrowSpacing * 2, Bounds.Center.Y - _leftArrowSizeY / 2);
		if (LeftButton != null)
		{
			LeftButton.Position = _leftArrowPosition;
			LeftButton.UpdateBounds();
		}
		
		_rightArrowPosition = new Vector2(Bounds.Right + _arrowSpacing, Bounds.Center.Y - _rightArrowSizeX / 2);
		if (RightButton != null)
		{
			RightButton.Position = _rightArrowPosition;
			RightButton.UpdateBounds();
		}
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
}