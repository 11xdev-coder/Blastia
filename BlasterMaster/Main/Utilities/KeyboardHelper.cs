using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main.Utilities;

public static class KeyboardHelper
{
    /// <summary>
    /// Represents the virtual key code for the Caps Lock key.
    /// </summary>
    public const int VkCapsLock = 0x14;

    /// <summary>
    /// Retrieves the status of the specified virtual key.
    /// </summary>
    /// <param name="keyCode">The virtual key code for which to retrieve the status.</param>
    /// <returns>The status of the specified virtual key. If the high-order bit is 1, the key is down; if it is 0, the key is up. The low-order bit indicates whether the key was pressed after the previous call to GetKeyState.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern short GetKeyState(int keyCode);

    /// <summary>
    /// Determines whether the Caps Lock key is currently on.
    /// </summary>
    /// <returns>true if the Caps Lock key is on; otherwise, false.</returns>
    public static bool IsCapsLockOn()
    {
        return Convert.ToBoolean(GetKeyState(VkCapsLock) & 0x0001);
    }
    
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
        bool isShiftDown = currentKeyState.IsKeyDown(Keys.LeftShift) ||
                           currentKeyState.IsKeyDown(Keys.RightShift);
        
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (IsKeyJustPressed(currentKeyState, prevKeyState, key))
            {
                HandleKeyPress(key, stringBuilder, isShiftDown);
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
    /// /// <param name="isShiftDown">Indicates whether the shift key is currently held down.</param>
    private static void HandleKeyPress(Keys key, StringBuilder stringBuilder,
        bool isShiftDown = false)
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
            HandleCharacter(key, stringBuilder, isShiftDown);
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
    /// Handles the character key press by appending the appropriate character
    /// to the provided StringBuilder, taking into account shift and caps lock states.
    /// Only handles letters or digits.
    /// </summary>
    /// <param name="key">The character key that was pressed.</param>
    /// <param name="stringBuilder">The StringBuilder to be updated with the character key press.</param>
    /// <param name="isShiftDown">Indicates whether the shift key is currently held down.</param>
    private static void HandleCharacter(Keys key, StringBuilder stringBuilder,
        bool isShiftDown = false)
    {
        string keyString = key.ToString();

        if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
        {
            char character = keyString[0];

            if (isShiftDown || IsCapsLockOn())
            {
                stringBuilder.Append(char.ToUpper(character));
            }
            else
            {
                stringBuilder.Append(char.ToLower(character));
            }
        }
    }
}