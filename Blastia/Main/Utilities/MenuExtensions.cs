using Blastia.Main.UI;

namespace Blastia.Main.Utilities;

/// <summary>
/// Extensions to <c>Menu</c> class
/// </summary>
public static class MenuExtensions 
{
    /// <summary>
    /// Helper method to redunant if checks - Sets <c>Menu.Active</c> to new value if not null.
    /// </summary>
    /// <param name="menu"></param>
    /// <param name="active"></param>
    public static void SetActive(this Menu? menu, bool active) 
    {
        if (menu != null) 
        {
            menu.Active = active;
        }
    }
}