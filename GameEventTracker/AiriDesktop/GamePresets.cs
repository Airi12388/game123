namespace AiriDesktop;

/// <summary>
/// Built-in games with their usual daily and weekly routines. All three reset dailies at 4:00 AM server time
/// and weeklies on Monday at 4:00 AM. Add a game here to make it available in "Add a built-in game".
/// </summary>
internal static class GamePresets
{
    internal sealed record PresetTask(string Title, int Reward, bool Weekly);
    internal sealed record Preset(string Name, string[] Aliases, PresetTask[] Tasks);

    private static PresetTask D(string title, int reward) => new(title, reward, false);
    private static PresetTask W(string title, int reward) => new(title, reward, true);

    public static readonly Preset[] All =
    [
        new("Zenless Zone Zero", ["ZZZ", "Zenless"],
        [
            D("Reach 400 Engagement (daily errands)", 30),
            D("Spend Battery Charge", 25),
            D("Grab a coffee at Coff Cafe", 10),
            D("Scratch a card at the newsstand", 10),
            D("Run the Random Play video store", 10),
            D("Claim the HoYoLAB check-in", 5),
            W("Notorious Hunt ×3 (weekly bosses)", 80),
            W("Ridu Weekly: reach the point cap", 80),
            W("Hollow Zero / Lost Void weekly bounty", 80),
            W("City Fund weekly missions", 60),
        ]),
        new("Honkai: Star Rail", ["HSR", "Star Rail", "Honkai Star Rail"],
        [
            D("Daily Training: reach 500 activity", 30),
            D("Spend Trailblaze Power", 25),
            D("Collect and resend Assignments", 10),
            D("Nameless Honor daily missions", 10),
            D("Claim the HoYoLAB check-in", 5),
            W("Echo of War ×3 (weekly bosses)", 80),
            W("Simulated / Divergent Universe weekly points", 80),
            W("Nameless Honor weekly missions", 60),
            W("Check Memory of Chaos, Pure Fiction & Apocalyptic Shadow", 60),
        ]),
        new("Genshin Impact", ["Genshin", "GI"],
        [
            D("Complete 4 Daily Commissions", 30),
            D("Claim the commission bonus from Katheryne", 10),
            D("Spend Original Resin", 25),
            D("Collect and resend Expeditions", 10),
            D("Battle Pass daily missions", 10),
            D("Claim the HoYoLAB check-in", 5),
            W("Weekly bosses ×3 (discounted Resin)", 80),
            W("Battle Pass weekly missions", 60),
            W("Serenitea Pot: realm currency & companionship", 40),
            W("Use the Parametric Transformer", 40),
            W("Check Spiral Abyss, Imaginarium Theater & Stygian Onslaught", 60),
        ]),
    ];

    /// <summary>Quests the very first version of the app seeded; replaced by the presets when upgrading a save.</summary>
    public static readonly string[] LegacySeedTitles = ["Spend some Original Resin", "Complete a commission", "Complete a Calyx", "Use daily training"];

    public static Preset? Find(string gameName) => All.FirstOrDefault(p =>
        p.Name.Equals(gameName.Trim(), StringComparison.OrdinalIgnoreCase) ||
        p.Aliases.Any(a => a.Equals(gameName.Trim(), StringComparison.OrdinalIgnoreCase)));
}
