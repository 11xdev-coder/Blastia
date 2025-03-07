namespace Blastia.Main;

public static class MainEntry
{
    [STAThread]
    public static void Main()
    {
        Synthesizer.Synthesizer.Launch([]);
        //BlastiaGame game = new BlastiaGame();
        //game.Run();
    }
}