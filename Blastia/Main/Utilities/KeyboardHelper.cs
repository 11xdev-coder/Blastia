﻿using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.Utilities;

public static class KeyboardHelper
{
    // dictionary to store each key
    private static readonly Dictionary<Keys, double> KeyHoldTimes = new();
    private const double CharacterInitialHoldDelay = 0.5f;
    private const double CharacterHoldRepeatInterval = 0.1f;

    private static readonly Dictionary<Keys, (char normal, char shifted)> SymbolKeyMap = new()
    {
        { Keys.OemOpenBrackets, ('[', '{') },
        { Keys.OemCloseBrackets, (']', '}') },
        { Keys.OemSemicolon, (';', ':') },
        { Keys.OemQuotes, ('\'', '"') },
        { Keys.OemComma, (',', '<') },
        { Keys.OemPeriod, ('.', '>') },
        { Keys.OemQuestion, ('/', '?') },
        { Keys.OemPipe, ('\\', '|') },
        { Keys.OemMinus, ('-', '_') },
        { Keys.OemPlus, ('=', '+') },
        { Keys.OemTilde, ('`', '~') }
    };
    
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
    
    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff,
        uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    private static char GetCharFromKey(Keys key, bool isShiftDown)
    {
        var keyboardState = new byte[256];
        GetKeyboardState(keyboardState);

        if (isShiftDown)
            keyboardState[(int) Keys.LeftShift] = 0x80;

        uint vk = (uint) key;
        uint scan = MapVirtualKey(vk, 0);
        var buff = new StringBuilder(2);
        IntPtr hkl = GetKeyboardLayout(0);

        int ret = ToUnicodeEx(vk, scan, keyboardState, buff, buff.Capacity, 0, hkl);
        if (ret > 0)
            return buff[0];

        return '\0';
    }


    /// <summary>
    /// Determines whether the Caps Lock key is currently on.
    /// </summary>
    /// <returns>true if the Caps Lock key is on; otherwise, false.</returns>
    public static bool IsCapsLockOn()
    {
        return Convert.ToBoolean(GetKeyState(VkCapsLock) & 0x0001);
    }

    public static bool IsKeyJustPressed(Keys key)
    {
        // currently -> down, previously -> up
        return BlastiaGame.KeyboardState.IsKeyDown(key) && BlastiaGame.PreviousKeyboardState.IsKeyUp(key);
    }

    /// <summary>
    /// Processes the input from the current keyboard state and updates the provided StringBuilder
    /// based on the keys that were pressed
    /// </summary>
    /// <param name="index">Index where to write at + will be changed to new index</param>
    /// <param name="stringBuilder">StringBuilder to be updated</param>
    /// <returns>If StringBuilder was updated -> true; otherwise -> false</returns>
    public static bool ProcessInput(ref int index, StringBuilder stringBuilder)
    {
        bool hasChanged = false;

        KeyboardState currentKeyState = BlastiaGame.KeyboardState;

        bool isShiftDown = currentKeyState.IsKeyDown(Keys.LeftShift) ||
                           currentKeyState.IsKeyDown(Keys.RightShift);
        
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (key == Keys.Left || key == Keys.Right) continue; // ignore arrows
            
            if (currentKeyState.IsKeyDown(key))
            {
                // set pressed key holding time to 0
                KeyHoldTimes.TryAdd(key, 0);
                
                // temp index to change later
                var tempIndex = index;
                
                Action<double> updateTimerRef = timerRef => KeyHoldTimes[key] = timerRef;

                Action handleAction = key switch
                {
                    Keys.Back => () => hasChanged = HandleBackSpace(ref tempIndex, key, stringBuilder),
                    Keys.Space => () => hasChanged = HandleSpace(ref tempIndex, key, stringBuilder),
                    _ => () => hasChanged = HandleCharacter(ref tempIndex, key, stringBuilder, isShiftDown)
                };
                
                ProcessKeyHold(key, CharacterInitialHoldDelay, CharacterHoldRepeatInterval,
                    updateTimerRef, handleAction);
                
                // update index
                index = tempIndex;
            }
            else
            {
                // if not holding remove the key
                KeyHoldTimes.Remove(key);
            }
        }
        
        return hasChanged;
    }
    
    #region Short key handlings
    private static bool HandleSpace(ref int index, Keys key, StringBuilder stringBuilder)
    {
        if (key == Keys.Space)
        {
            stringBuilder.Insert(index, ' ');
            index++;
            return true;
        }

        return false;
    }

    private static bool HandleBackSpace(ref int index, Keys key, StringBuilder stringBuilder)
    {
        if (key == Keys.Back && stringBuilder.Length > 0 && index > 0)
        {
            int positiveIndex = Math.Max(0, index - 1);
            stringBuilder.Remove(positiveIndex, 1);
            index = positiveIndex;
            
            return true;
        }

        return false;
    }

    private static bool HandleCharacter(ref int index, Keys key, StringBuilder stringBuilder,
        bool isShiftDown = false)
    {
        // string keyString = key.ToString();
        //
        // // handle digits (1 = 'D1', 2 = 'D2' etc.)
        // if (keyString.Length == 2 && keyString[0] == 'D' && char.IsDigit(keyString[1]))
        // {
        //     char digit = keyString[1];
        //     if (isShiftDown)
        //     {
        //         // map shifted digis to symbols
        //         var shiftedDigits = ")!@#$%^&*(";
        //         digit = shiftedDigits[keyString[1] - '0'];
        //     }
        //     
        //     stringBuilder.Insert(index, digit);
        //     index++;
        //     return true;
        // }
        //
        // // handle normal letters (like 'A', 'B', 'C')
        // if (keyString.Length == 1 && char.IsLetter(keyString[0]))
        // {
        //     char character = keyString[0];
        //
        //     if ((isShiftDown || IsCapsLockOn()) && !(isShiftDown && IsCapsLockOn()))
        //     {
        //         stringBuilder.Insert(index, char.ToUpper(character));
        //         index++;
        //         return true;
        //     }
        //
        //     // if no shift/caps -> lower
        //     stringBuilder.Insert(index, char.ToLower(character));
        //     index++;
        //     return true;
        // }
        //
        // // handle symbols
        // if (SymbolKeyMap.TryGetValue(key, out var symbolPair))
        // {
        //     var charToInsert = isShiftDown ? symbolPair.shifted : symbolPair.normal;
        //     stringBuilder.Insert(index, charToInsert);
        //     index++;
        //     return true;
        // }
        
        var ch = GetCharFromKey(key, isShiftDown);
        if (ch == '\0')
            return false;

        stringBuilder.Insert(index, ch);
        index++;

        return true;
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
        if (BlastiaGame.KeyboardState.IsKeyDown(keyHeld))
        {
            oppositeKeyTimerRef = 0;
            
            // single press
            if (BlastiaGame.PreviousKeyboardState.IsKeyUp(keyHeld))
            {
                heldKeyTimerRef = 0;
                onPressAction();
            }
            else
            {
                // still holding
                heldKeyTimerRef += BlastiaGame.GameTimeElapsedSeconds;
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
        
        if (BlastiaGame.KeyboardState.IsKeyDown(keyHeld))
        {
            // single press
            if (BlastiaGame.PreviousKeyboardState.IsKeyUp(keyHeld))
            {
                heldKeyTimerRef = 0;
                onPressAction();
            }
            else
            {
                // still holding
                heldKeyTimerRef += BlastiaGame.GameTimeElapsedSeconds;
                if (heldKeyTimerRef >= holdDelay)
                {
                    heldKeyTimerRef -= holdInterval;
                    onPressAction();
                }
            }

            updateHeldKeyTimerRef(heldKeyTimerRef);
        }
    }

    /// <summary>
    /// Goes through each pressed key and tries to map it. Adds result to initialValue
    /// </summary>
    /// <param name="map"></param>
    /// <param name="initialValue"></param>
    public static void AccumulateValueFromMap(Dictionary<Keys, Vector2> map, ref Vector2 initialValue) 
    {
        Keys[] pressedKeys = BlastiaGame.KeyboardState.GetPressedKeys();
        foreach (var key in pressedKeys)
        {
            map.TryGetValue(key, out var newValue);
            initialValue += newValue;
        }
    }
    
    /// <summary>
    /// See <see cref="AccumulateValueFromMap(System.Collections.Generic.Dictionary{Microsoft.Xna.Framework.Input.Keys,Vector2},ref Vector2)"/>
    /// </summary>
    /// <param name="map"></param>
    /// <param name="initialValue"></param>
    public static void AccumulateValueFromMap(Dictionary<Keys, float> map, ref float initialValue) 
    {
        Keys[] pressedKeys = BlastiaGame.KeyboardState.GetPressedKeys();
        foreach (var key in pressedKeys)
        {
            map.TryGetValue(key, out var newValue);
            initialValue += newValue;
        }
    }
}