using System.Windows;
using System.Windows.Media;

namespace AiriDesktop;

/// <summary>Shared colors for the dark theme, mirrored from the XAML resources.</summary>
public static class Palette
{
    public static readonly SolidColorBrush Muted = Hex("#8E8E99");
    public static readonly SolidColorBrush Red = Hex("#EE4B2B");
    public static readonly SolidColorBrush Orange = Hex("#F0812F");
    public static readonly SolidColorBrush Yellow = Hex("#F5C518");
    public static readonly SolidColorBrush Purple = Hex("#A15CF6");
    public static readonly SolidColorBrush Blue = Hex("#4C8DFF");
    public static readonly SolidColorBrush Green = Hex("#22B573");
    public static readonly SolidColorBrush Cyan = Hex("#4CC9FF");

    private static readonly SolidColorBrush[] GameBrushes = [Orange, Blue, Purple, Green, Yellow, Red];
    private static readonly SolidColorBrush LegendarySurface = Hex("#2A220C");
    private static readonly SolidColorBrush EpicSurface = Hex("#221735");
    private static readonly SolidColorBrush RareSurface = Hex("#13203A");
    private static readonly Dictionary<string, Brush> ArtCache = [];

    public static SolidColorBrush Hex(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    public static Brush Game(int index) => index < 0 ? Muted : GameBrushes[index % GameBrushes.Length];

    public static (string Label, string Letter, Brush Accent, Brush Surface) Rarity(int stars) => stars switch
    {
        5 => ("Legendary", "L", Yellow, LegendarySurface),
        4 => ("Epic", "E", Purple, EpicSurface),
        _ => ("Rare", "R", Blue, RareSurface)
    };

    public static Brush Armor(string rarity) => rarity switch
    {
        "Uncommon" => Green,
        "Rare" => Blue,
        "Epic" => Purple,
        "Unique" => Yellow,
        _ => Muted
    };

    /// <summary>Placeholder art: a diagonal wash of the character's color into the card's dark base.</summary>
    public static Brush Art(string hex)
    {
        if (ArtCache.TryGetValue(hex, out var cached)) return cached;
        var color = (Color)ColorConverter.ConvertFromString(hex)!;
        var brush = new LinearGradientBrush(
            [new GradientStop(color, 0), new GradientStop(Color.FromArgb(255, (byte)(color.R / 3), (byte)(color.G / 3), (byte)(color.B / 3)), .65), new GradientStop(Color.FromRgb(16, 16, 20), 1)],
            new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        ArtCache[hex] = brush;
        return brush;
    }
}

public sealed record CharacterCardModel(
    string Id, string Name, string Source, int Rarity, string RarityLabel, string RarityLetter,
    Brush Accent, Brush Surface, Brush ArtBackground, string Initials, string Number,
    int Pow, int Aura, int Copies, ImageSource? Portrait)
{
    public bool IsOwned => Copies > 0;
    public double CardOpacity => IsOwned ? 1 : .42;
    public string OwnedLabel => Copies switch { 0 => "Not pulled", 1 => "Owned", _ => $"Owned ×{Copies}" };
    public Brush OwnedBrush => IsOwned ? Palette.Green : Palette.Muted;
    public Visibility InitialsVisibility => Portrait is null ? Visibility.Visible : Visibility.Collapsed;
    public string CopiesText => Copies > 1 ? $"×{Copies}" : Copies == 1 ? "OWNED" : "STARTER";
}

public sealed record GameRowModel(string Id, string Name, Brush Dot, string Count, bool CanRemove)
{
    public Visibility RemoveVisibility => CanRemove ? Visibility.Visible : Visibility.Collapsed;
}

public sealed record EventRowModel(string Id, string Title, string GameName, Brush GameBrush, string Dates, string Status, Brush StatusBrush, string Countdown, bool Done)
{
    public Visibility CompleteVisibility => Done ? Visibility.Collapsed : Visibility.Visible;
}

public sealed record QuestRowModel(string Id, string Title, int Reward, bool Done)
{
    public bool CanComplete => !Done;
    public string CheckGlyph => Done ? "✓" : "";
    public string RewardText => Done ? "CLAIMED" : $"+{Reward} ◆";
    public Brush RewardBrush => Done ? Palette.Muted : Palette.Cyan;
    public double TitleOpacity => Done ? .5 : 1;
}

public sealed record WeeklyGoalRow(string Id, string Title, int Progress, int Target, int Reward, bool Claimed)
{
    public int Shown => Math.Min(Progress, Target);
    public bool Ready => !Claimed && Progress >= Target;
    public string ProgressText => $"{Shown}/{Target}";
    public string ButtonText => Claimed ? "Claimed" : Ready ? "Claim" : "In progress";
}

public sealed record ArmorRowModel(string Name, string Detail, string Bonuses, string Count, Brush Accent, bool Equipped)
{
    public Visibility EquippedVisibility => Equipped ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BonusVisibility => Bonuses.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
}
