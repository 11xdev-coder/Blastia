using System.Text;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main.Utilities;

public static class KeyboardHelper
{
    /// <summary>
    /// Processes the input from the current keyboard state and updates the provided StringBuilder
    /// based on the keys that were pressed.
    /// </summary>
    /// <param name="currentKeyState">The current state of the keyboard.</param>
    /// <param name="prevKeyState">The previous state of the keyboard.</param>
    /// <param name="stringBuilder">The StringBuilder to be updated based on the key inputs.</param>
    public static void ProcessInput(KeyboardState currentKeyState,
        KeyboardState prevKeyState, StringBuilder stringBuilder)
    {
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (IsKeyJustPressed(currentKeyState, prevKeyState, key))
            {
                HandleKeyPress(key, stringBuilder);
            }
        }
    }

    /// <summary>
    /// Determines if a specific key has just been pressed based on the current and previous keyboard states.
    /// </summary>
    /// <param name="currentKeyState">The current state of the keyboard.</param>
    /// <param name="prevKeyState">The previous state of the keyboard.</param>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key has just been pressed; otherwise, false.</returns>
    private static bool IsKeyJustPressed(KeyboardState currentKeyState, 
        KeyboardState prevKeyState, Keys key)
    {
        return currentKeyState.IsKeyDown(key) && prevKeyState.IsKeyUp(key);
    }

    /// <summary>
    /// Handles the key press by updating the provided StringBuilder based on the key that was pressed.
    /// Supports handling backspace, space, and character keys.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="stringBuilder">The StringBuilder to be updated based on the key press.</param>
    private static void HandleKeyPress(Keys key, StringBuilder stringBuilder)
    {
        if (key == Keys.Back && stringBuilder.Length > 0)
        {
            HandleBackSpace(stringBuilder);
        }
        else if (key == Keys.Space)
        {
           HandleSpace(stringBuilder);
        }
        else
        {
           HandleCharacter(key, stringBuilder);
        }
    }

    /// <summary>
    /// Handles the backspace key press by removing the last character from the provided StringBuilder.
    /// </summary>
    /// <param name="stringBuilder">The StringBuilder from which the last character will be removed.</param>
    private static void HandleBackSpace(StringBuilder stringBuilder)
    {
        stringBuilder.Length -= 1;
    }

    /// <summary>
    /// Handles the space key press by appending a space character to the provided StringBuilder.
    /// </summary>
    /// <param name="stringBuilder">The StringBuilder to which a space character will be appended.</param>
    private static void HandleSpace(StringBuilder stringBuilder)
    {
        stringBuilder.Append(' ');
    }

    /// <summary>
    /// Handles a character key press by appending the character to the provided StringBuilder.
    /// Only appends characters that are letters or digits.
    /// </summary>
    /// <param name="key">The key representing the character that was pressed.</param>
    /// <param name="stringBuilder">The StringBuilder to be updated with the character.</param>
    private static void HandleCharacter(Keys key, StringBuilder stringBuilder)
    {
        string keyString = key.ToString();

        if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
        {
            stringBuilder.Append(keyString);
        }
    }
}