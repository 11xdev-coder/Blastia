namespace Blastia.Main;

public static class MainEntry
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("-synth"))
        {
            Synthesizer.Synthesizer.Launch([]);
        }
        
        BlastiaGame game = new BlastiaGame();
        game.Run();
    }
}