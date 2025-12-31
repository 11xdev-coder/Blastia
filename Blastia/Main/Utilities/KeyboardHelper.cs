using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.Utilities;

public static class KeyboardHelper
{
    // dictionary to store each key
    private static readonly Dictionary<int, bool> PreviousProblemKeyStates = new();
    private static readonly Dictionary<Keys, double> KeyHoldTimes = new();
    private const double CharacterInitialHoldDelay = 0.5f;
    private const double CharacterHoldRepeatInterval = 0.1f;

    private const int VkOemComma = 0xBC;
    private const int VkOemPeriod = 0xBE;
    private const int VkOemSemicolon = 0xBA;
    private const int VkOemQuote = 0xDE;
    private const int VkOemOpenBrackets = 0xDB;
    private const int VkOemCloseBrackets = 0xDD;
    private const int VkOemQuestion = 0xBF;
    private const int VkOemTilde = 0xC0;

    private static readonly Dictionary<Keys, (char normal, char shifted)> EnglishKeyMapping = new()
    {
        // letters
        {Keys.A, ('a', 'A')}, {Keys.B, ('b', 'B')}, {Keys.C, ('c', 'C')}, {Keys.D, ('d', 'D')},
        {Keys.E, ('e', 'E')}, {Keys.F, ('f', 'F')}, {Keys.G, ('g', 'G')}, {Keys.H, ('h', 'H')},
        {Keys.I, ('i', 'I')}, {Keys.J, ('j', 'J')}, {Keys.K, ('k', 'K')}, {Keys.L, ('l', 'L')},
        {Keys.M, ('m', 'M')}, {Keys.N, ('n', 'N')}, {Keys.O, ('o', 'O')}, {Keys.P, ('p', 'P')},
        {Keys.Q, ('q', 'Q')}, {Keys.R, ('r', 'R')}, {Keys.S, ('s', 'S')}, {Keys.T, ('t', 'T')},
        {Keys.U, ('u', 'U')}, {Keys.V, ('v', 'V')}, {Keys.W, ('w', 'W')}, {Keys.X, ('x', 'X')},
        {Keys.Y, ('y', 'Y')}, {Keys.Z, ('z', 'Z')},

        // numbers
        {Keys.D0, ('0', ')')}, {Keys.D1, ('1', '!')}, {Keys.D2, ('2', '@')}, {Keys.D3, ('3', '#')},
        {Keys.D4, ('4', '$')}, {Keys.D5, ('5', '%')}, {Keys.D6, ('6', '^')}, {Keys.D7, ('7', '&')},
        {Keys.D8, ('8', '*')}, {Keys.D9, ('9', '(')},
        
        // symbols
        {Keys.OemTilde, ('`', '~')}, {Keys.OemMinus, ('-', '_')}, {Keys.OemPlus, ('=', '+')},
        {Keys.OemOpenBrackets, ('[', '{')}, {Keys.OemCloseBrackets, (']', '}')}, {Keys.OemPipe, ('\\', '|')},
        {Keys.OemSemicolon, (';', ':')}, {Keys.OemQuotes, ('\'', '"')}, {Keys.OemComma, (',', '<')},
        {Keys.OemPeriod, ('.', '>')}, {Keys.OemQuestion, ('/', '?')}
    };
    
    private static readonly Dictionary<Keys, (char normal, char shifted)> RussianKeyMapping = new()
    {
        // letters
        {Keys.A, ('ф', 'Ф')}, {Keys.B, ('и', 'И')}, {Keys.C, ('с', 'С')}, {Keys.D, ('в', 'В')},
        {Keys.E, ('у', 'У')}, {Keys.F, ('а', 'А')}, {Keys.G, ('п', 'П')}, {Keys.H, ('р', 'Р')},
        {Keys.I, ('ш', 'Ш')}, {Keys.J, ('о', 'О')}, {Keys.K, ('л', 'Л')}, {Keys.L, ('д', 'Д')},
        {Keys.M, ('ь', 'Ь')}, {Keys.N, ('т', 'Т')}, {Keys.O, ('щ', 'Щ')}, {Keys.P, ('з', 'З')},
        {Keys.Q, ('й', 'Й')}, {Keys.R, ('к', 'К')}, {Keys.S, ('ы', 'Ы')}, {Keys.T, ('е', 'Е')},
        {Keys.U, ('г', 'Г')}, {Keys.V, ('м', 'М')}, {Keys.W, ('ц', 'Ц')}, {Keys.X, ('ч', 'Ч')},
        {Keys.Y, ('н', 'Н')}, {Keys.Z, ('я', 'Я')},

        // numbers
        {Keys.D0, ('0', ')')}, {Keys.D1, ('1', '!')}, {Keys.D2, ('2', '"')}, {Keys.D3, ('3', '#')},
        {Keys.D4, ('4', ';')}, {Keys.D5, ('5', '%')}, {Keys.D6, ('6', ':')}, {Keys.D7, ('7', '?')},
        {Keys.D8, ('8', '*')}, {Keys.D9, ('9', '(')},
        
        // symbols
        {Keys.OemTilde, ('ё', 'Ё')}, {Keys.OemMinus, ('-', '_')}, {Keys.OemPlus, ('=', '+')},
        {Keys.OemOpenBrackets, ('х', 'Х')}, {Keys.OemCloseBrackets, ('ъ', 'Ъ')}, {Keys.OemPipe, ('\\', '/')},
        {Keys.OemSemicolon, ('ж', 'Ж')}, {Keys.OemQuotes, ('э', 'Э')}, {Keys.OemComma, ('б', 'Б')},
        {Keys.OemPeriod, ('ю', 'Ю')}, {Keys.OemQuestion, ('.', ',')}
    };
    
    // virtual code -> character mapping
    private static readonly Dictionary<int, (char normal, char shifted)> RussianProblemKeyMapping = new()
    {
        { VkOemComma, ('б', 'Б') },
        { VkOemPeriod, ('ю', 'Ю') },
        { VkOemSemicolon, ('ж', 'Ж') },
        { VkOemQuote, ('э', 'Э') },
        { VkOemOpenBrackets, ('х', 'Х') },
        { VkOemCloseBrackets, ('ъ', 'Ъ') },
        { VkOemQuestion, ('.', ',') },
        { VkOemTilde, ('ё', 'Ё') }
    };

    private static Keys GetCorrespondingKey(int vkCode)
    {
        return vkCode switch
        {
            VkOemComma => Keys.OemComma,
            VkOemPeriod => Keys.OemPeriod,
            VkOemSemicolon => Keys.OemSemicolon,
            VkOemQuote => Keys.OemQuotes,
            VkOemOpenBrackets => Keys.OemOpenBrackets,
            VkOemCloseBrackets => Keys.OemCloseBrackets,
            VkOemQuestion => Keys.OemQuestion,
            VkOemTilde => Keys.OemTilde,
            _ => Keys.None
        };
    }
    
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
    private static extern IntPtr GetKeyboardLayout(uint idThread);
    
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

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

    private static void ProcessProblemKey(int vkCode, (char normal, char shifted) characters,
        bool isShiftDown, ref int index, ref bool hasChanged, StringBuilder stringBuilder)
    {
        Keys correspondingKey = GetCorrespondingKey(vkCode);
        bool isKeyDown = (GetAsyncKeyState(vkCode) & 0x8000) != 0;

        // Manage key hold state
        if (isKeyDown)
        {
            KeyHoldTimes.TryAdd(correspondingKey, 0);
            var tempIndex = index;

            var characterAction = () =>
            {
                stringBuilder.Insert(tempIndex, isShiftDown ? characters.shifted : characters.normal);
                tempIndex++;
                return true; // true -> indicates a change
            };

            // action to update the timer
            Action<double> updateTimerRef = timerRef => KeyHoldTimes[correspondingKey] = timerRef;

            // get previous state
            PreviousProblemKeyStates.TryGetValue(vkCode, out bool wasKeyDown);
            double heldKeyTimerRef = KeyHoldTimes[correspondingKey];

            // initial press logic
            var keyActionPerformed = false;
            if (!wasKeyDown)
            {
                heldKeyTimerRef = 0;
                keyActionPerformed = characterAction();
            }
            else
            {
                // key hold
                heldKeyTimerRef += BlastiaGame.GameTimeElapsedSeconds;
                if (heldKeyTimerRef >= CharacterInitialHoldDelay)
                {
                    heldKeyTimerRef -= CharacterHoldRepeatInterval;
                    keyActionPerformed = characterAction();
                }
            }

            if (keyActionPerformed)
            {
                hasChanged = true;
            }
            
            updateTimerRef(heldKeyTimerRef);
            index = tempIndex;
        }
        else
        {
            KeyHoldTimes.Remove(correspondingKey);
        }

        // store current state for next time
        PreviousProblemKeyStates[vkCode] = isKeyDown;
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
        bool isRussian = IsRussianKeyboardLayout();

        // Handle special Russian keyboard keys directly using Windows API
        if (isRussian)
        {
            foreach (var kvp in RussianProblemKeyMapping)
            {
                ProcessProblemKey(kvp.Key, kvp.Value, isShiftDown, ref index, ref hasChanged, stringBuilder);
            }
        }

        // Continue with regular key processing
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            // Skip keys we handled directly above
            if (isRussian && RussianProblemKeyMapping.Any(k => GetCorrespondingKey(k.Key) == key))
                continue;

            if (currentKeyState.IsKeyDown(key))
            {
                KeyHoldTimes.TryAdd(key, 0);
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

                index = tempIndex;
            }
            else
            {
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

    private static bool IsRussianKeyboardLayout()
    {
        try
        {
            IntPtr hkl = GetKeyboardLayout(0);
            // russian keyboard layout identifier (0x0419 = 1049 decimal)
            // the low word contains the language identifier
            int langId = (int)hkl & 0xFFFF;
            return langId == 0x0419;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool HandleCharacter(ref int index, Keys key, StringBuilder stringBuilder,
        bool isShiftDown = false)
    {
        // dont process when control is held
        if (BlastiaGame.KeyboardState.IsKeyDown(Keys.LeftControl) || BlastiaGame.KeyboardState.IsKeyDown(Keys.RightControl)) return false;
        
        (char, char) pair;
        if (IsRussianKeyboardLayout()) 
            RussianKeyMapping.TryGetValue(key, out pair);
        else
            EnglishKeyMapping.TryGetValue(key, out pair);
        
        if (pair == default)
        {
            // if key is not mapped, return false
            return false;
        }
        var ch = isShiftDown ? pair.Item2 : pair.Item1;

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