using System.Globalization;
using System.Resources;

namespace MasterMind;

public class Program
{
    public static void Main(string[] args)
    {
        var game = new Game();
        game.Start();
    }
}

public class Game
{
    private readonly ResourceManager _resourceManager = new("MasterMind.Resources.Strings", typeof(Program).Assembly);
    private CultureInfo _cultureInfo = new("en-US");
    private readonly SequenceGenerator _sequenceGenerator;
    
    private List<string> _secretSequence = [];
    private int _numberOfColors;
    private int _maxAttempts;

    public Game()
    {
        _sequenceGenerator = new SequenceGenerator(_resourceManager, _cultureInfo);
    }

    public void Start()
    {
        ConfigureGame();

        var attempts = 0;
        var won = false;

        Console.WriteLine(string.Join(", ", _secretSequence));
        while (attempts < _maxAttempts)
        {
            attempts++;
            Console.WriteLine(_resourceManager.GetString("Attempt", _cultureInfo)!, attempts, _maxAttempts);

            var playerGuess = GetPlayerGuess();
            var (wellPlaced, misplaced) = FeedbackEvaluator.Evaluate(playerGuess, _secretSequence);

            Console.WriteLine(_resourceManager.GetString("Feedback", _cultureInfo)!, wellPlaced, misplaced);
            DisplayVisualFeedback(wellPlaced, misplaced, _numberOfColors);

            if (wellPlaced != _numberOfColors) continue;

            Console.WriteLine(_resourceManager.GetString("WinMessage", _cultureInfo));
            won = true;
            break;
        }

        if (won) return;

        Console.WriteLine(_resourceManager.GetString("LoseMessage", _cultureInfo));
        Console.WriteLine(string.Join(", ", _secretSequence));
    }

    private void ConfigureGame()
    {
        ChooseLanguage();
        Console.WriteLine(_resourceManager.GetString("WelcomeMessage", _cultureInfo));
        _numberOfColors = GetValidatedInput(_resourceManager.GetString("ChooseColors", _cultureInfo)!, 4, 10);
        _maxAttempts = GetValidatedInput(_resourceManager.GetString("ChooseAttempts", _cultureInfo)!, 10, 100);
        _secretSequence = _sequenceGenerator.Generate(_numberOfColors);
    }

    private void ChooseLanguage()
    {
        Console.WriteLine("Choose language / Choisissez la langue (en/fr):");
        var languageCode = Console.ReadLine()?.ToLower();

        _cultureInfo = new CultureInfo(languageCode == "fr" ? "fr-FR" : "en-US");
        _sequenceGenerator.SetCulture(_cultureInfo);
    }

    private int GetValidatedInput(string prompt, int min, int max)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            var input = Console.ReadLine();

            if (int.TryParse(input, out var value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine(_resourceManager.GetString("InvalidInput", _cultureInfo)!, min, max);
        }
    }

    private List<string> GetPlayerGuess()
    {
        while (true)
        {
            Console.WriteLine(_resourceManager.GetString("EnterGuess", _cultureInfo)!, _numberOfColors);
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine(_resourceManager.GetString("InvalidGuess", _cultureInfo)!, _numberOfColors);
                continue;
            }

            var guess = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToList();

            if (guess.Count != _numberOfColors)
            {
                Console.WriteLine(_resourceManager.GetString("InvalidGuess", _cultureInfo)!, _numberOfColors);
                continue;
            }

            var invalidColors = guess
                .Where(c => !_sequenceGenerator.GetColors().Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (invalidColors.Count <= 0) return guess;

            Console.WriteLine(_resourceManager.GetString("InvalidColors", _cultureInfo)!,
                string.Join(", ", invalidColors));
            Console.WriteLine(_resourceManager.GetString("ValidColors", _cultureInfo)!,
                string.Join(", ", _sequenceGenerator.GetColors()));
        }
    }

    private void DisplayVisualFeedback(int wellPlaced, int misplaced, int totalColors)
    {
        Console.WriteLine(_resourceManager.GetString("VisualFeedback", _cultureInfo));
        for (var i = 0; i < wellPlaced; i++)
        {
            // Console.Write("O ");
            // Console.Write("🔵 ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("■ ");
        }

        for (var i = 0; i < misplaced; i++)
        {
            // Console.Write("X ");
            // Console.Write("🟠 ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("■ ");
        }

        for (var i = 0; i < totalColors - wellPlaced - misplaced; i++)
        {
            // Console.Write(". ");
            // Console.Write("🔴 ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("■ ");
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine();
    }
}

public class SequenceGenerator(ResourceManager resourceManager, CultureInfo cultureInfo)
{
    private CultureInfo _cultureInfo = cultureInfo;

    public List<string> GetColors()
    {
        return
        [
            resourceManager.GetString("ColorRed", _cultureInfo)!,
            resourceManager.GetString("ColorBlue", _cultureInfo)!,
            resourceManager.GetString("ColorGreen", _cultureInfo)!,
            resourceManager.GetString("ColorYellow", _cultureInfo)!,
            resourceManager.GetString("ColorOrange", _cultureInfo)!,
            resourceManager.GetString("ColorPurple", _cultureInfo)!,
            resourceManager.GetString("ColorPink", _cultureInfo)!,
            resourceManager.GetString("ColorWhite", _cultureInfo)!,
            resourceManager.GetString("ColorBlack", _cultureInfo)!,
            resourceManager.GetString("ColorBrown", _cultureInfo)!
        ];
    }

    public List<string> Generate(int numberOfColors)
    {
        var colors = GetColors();
        return colors
            .OrderBy(_ => Random.Shared.Next())
            .Take(numberOfColors)
            .ToList();
    }

    public void SetCulture(CultureInfo cultureInfo)
    {
        _cultureInfo = cultureInfo;
    }
}

public static class FeedbackEvaluator
{
    public static (int wellPlaced, int misplaced) Evaluate(List<string> guess, List<string> secretSequence)
    {
        var wellPlaced = 0;
        var misplaced = 0;

        var secretCopy = secretSequence.Select(string? (c) => c.ToLower()).ToList();
        var guessCopy = guess.Select(string? (c) => c.ToLower()).ToList();

        for (var i = 0; i < secretCopy.Count; i++)
        {
            if (guessCopy[i] != secretCopy[i]) continue;

            wellPlaced++;
            secretCopy[i] = null;
            guessCopy[i] = null;
        }

        foreach (var color in guessCopy.Where(c => c != null))
        {
            if (!secretCopy.Contains(color)) continue;
            misplaced++;
            secretCopy.Remove(color);
        }

        return (wellPlaced, misplaced);
    }
}