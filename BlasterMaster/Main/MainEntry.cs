namespace BlasterMaster.Main;

public static class MainEntry
{
    [STAThread]
    public static void Main()
    {
        BlasterMasterGame game = new BlasterMasterGame();
        game.Run();
    }
}