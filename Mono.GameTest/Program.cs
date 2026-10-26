namespace Mono.GameTest;

class Program
{
    static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        var game = new GameBase();
        game.Run();
    }
}