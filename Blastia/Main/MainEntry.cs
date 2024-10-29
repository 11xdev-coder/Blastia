namespace Blastia.Main;

public static class MainEntry
{
    [STAThread]
    public static void Main()
    {
        BlastiaGame game = new BlastiaGame();
        game.Run();
    }
}