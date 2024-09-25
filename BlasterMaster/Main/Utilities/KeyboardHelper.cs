using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main.Utilities;

public static class KeyboardHelper
{
    // dictionary to store each key
    private static readonly Dictionary<Keys, double> KeyHoldTimes = new();
    private const double CharacterInitialHoldDelay = 0.5f;
    private const double CharacterHoldRepeatInterval = 0.1f;
    
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
    /// <param name="stringBuilder">The StringBuilder to be updated based on the key inputs.</param>
    public static bool ProcessInput(StringBuilder stringBuilder)
    {
        bool hasChanged = false;
        
        KeyboardState currentKeyState = BlasterMasterGame.KeyboardState;
        KeyboardState prevKeyState = BlasterMasterGame.PreviousKeyboardState;
        
        bool isShiftDown = currentKeyState.IsKeyDown(Keys.LeftShift) ||
                           currentKeyState.IsKeyDown(Keys.RightShift);
        
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (key == Keys.Left || key == Keys.Right) continue; // ignore arrows
            
            if (IsKeyJustPressed(currentKeyState, prevKeyState, key))
            {
                hasChanged = HandleDeleteAndSpace(key, stringBuilder);
            }

            if (currentKeyState.IsKeyDown(key))
            {
                // set pressed key holding time to 0
                KeyHoldTimes.TryAdd(key, 0);

                // Update the dictionary value directly
                ProcessKeyHold(key, CharacterInitialHoldDelay, CharacterHoldRepeatInterval,
                    timerRef => KeyHoldTimes[key] = timerRef,
                    () =>
                    {
                        HandleCharacter(key, stringBuilder, isShiftDown);
                        hasChanged = true;
                    });
            }
            else
            {
                // if not holding remove the key
                KeyHoldTimes.Remove(key);
            }
        }
        
        return hasChanged;
    }
    
    #region Helpers for ProcessInput
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
    /// <returns>Returns true if stringBuilder has been changed</returns>
    private static bool HandleDeleteAndSpace(Keys key, StringBuilder stringBuilder)
    {
        if (key == Keys.Back && stringBuilder.Length > 0)
        {
            HandleBackSpace(stringBuilder);
            return true;
        }
        
        if (key == Keys.Space)
        {
            HandleSpace(stringBuilder);
            return true;
        }

        return false;
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

            if ((isShiftDown || IsCapsLockOn()) && !(isShiftDown && IsCapsLockOn()))
            {
                stringBuilder.Append(char.ToUpper(character));
            }
            else
            {
                stringBuilder.Append(char.ToLower(character));
            }
        }
    }
    #endregion

    /// <summary>
    /// Handles the action for a key being held down with a delay and interval and executes click action
    /// </summary>
    /// <param name="keyHeld">The held key</param>
    /// <param name="holdDelay">The delay before the action is starts repeating</param>
    /// <param name="holdInterval">The interval at which action is repeated after holdDelay</param>
    /// <param name="heldKeyTimerRef">Reference to the timer for the held key</param>
    /// <param name="oppositeKeyTimerRef">Reference to the timer for the opposite key's hold action, which will be reset if the current key is pressed</param>
    /// <param name="onPressAction">The action executed when key is single-tapped or held</param>
    public static void ProcessKeyHold(Keys keyHeld, double holdDelay, double holdInterval,
        ref double heldKeyTimerRef,
        ref double oppositeKeyTimerRef, Action onPressAction)
    {
        if (BlasterMasterGame.KeyboardState.IsKeyDown(keyHeld))
        {
            oppositeKeyTimerRef = 0;
            
            // single press
            if (BlasterMasterGame.PreviousKeyboardState.IsKeyUp(keyHeld))
            {
                heldKeyTimerRef = 0;
                onPressAction();
            }
            else
            {
                // still holding
                heldKeyTimerRef += BlasterMasterGame.GameTimeElapsedSeconds;
                if (heldKeyTimerRef >= holdDelay)
                {
                    heldKeyTimerRef -= holdInterval;
                    onPressAction();
                }
            }
        }
    }

    /// <summary>
    /// Handles the action for a key being held down with a delay and interval and executes click action
    /// </summary>
    /// <param name="keyHeld">The held key</param>
    /// <param name="holdDelay">The delay before the action is starts repeating</param>
    /// <param name="holdInterval">The interval at which action is repeated after holdDelay</param>
    /// <param name="updateHeldKeyTimerRef">Action to update key hold timer to new value</param>
    /// <param name="onPressAction">The action executed when key is single-tapped or held</param>
    public static void ProcessKeyHold(Keys keyHeld, double holdDelay, double holdInterval, 
        Action<double> updateHeldKeyTimerRef,
        Action onPressAction)
    {
        double heldKeyTimerRef = KeyHoldTimes[keyHeld];
        
        if (BlasterMasterGame.KeyboardState.IsKeyDown(keyHeld))
        {
            // single press
            if (BlasterMasterGame.PreviousKeyboardState.IsKeyUp(keyHeld))
            {
                heldKeyTimerRef = 0;
                onPressAction();
            }
            else
            {
                // still holding
                heldKeyTimerRef += BlasterMasterGame.GameTimeElapsedSeconds;
                if (heldKeyTimerRef >= holdDelay)
                {
                    heldKeyTimerRef -= holdInterval;
                    onPressAction();
                }
            }

            updateHeldKeyTimerRef(heldKeyTimerRef);
        }
    }
}