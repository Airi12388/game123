namespace AiriDesktop;

internal sealed class AppState
{
    public bool IsInitialized { get; set; }
    public int PresetsVersion { get; set; }
    public int Balance { get; set; }
    public List<GameModel> Games { get; set; } = [];
    public List<EventModel> Events { get; set; } = [];
    public List<QuestModel> DailyQuests { get; set; } = [];
    public Dictionary<string, int> OwnedCharacters { get; set; } = [];
    public Dictionary<string, string> CharacterImages { get; set; } = [];
    public int PityFive { get; set; }
    public int PityFour { get; set; }
    public List<string> LastPulls { get; set; } = [];
    public WeeklyState Weekly { get; set; } = new();
    public BattlerState Battler { get; set; } = new();
}

internal sealed class GameModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public override string ToString() => Name;
}

internal sealed class EventModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string GameId { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Starts { get; set; } = DateTime.Today;
    public DateTime Ends { get; set; } = DateTime.Today;
    public bool Done { get; set; }
    public bool RewardClaimed { get; set; }
    public string Summary => $"{Title} · {Starts:MMM d}–{Ends:MMM d} · {(Done ? "Completed" : "Upcoming")}";
}

internal sealed class QuestModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string GameId { get; set; } = "";
    public string Title { get; set; } = "";
    public int Reward { get; set; } = 25;
    public string? CompletedOn { get; set; }
    public string? EarnedOn { get; set; }
    public bool Weekly { get; set; }
    public string? CompletedWeek { get; set; }
    public string Summary => $"{(CompletedOn == DateTime.Today.ToString("yyyy-MM-dd") ? "✓" : "○")}  {Title}  ·  +{Reward} P";
}

internal sealed class WeeklyState
{
    public string WeekOf { get; set; } = "";
    public int DailyClears { get; set; }
    public int EventClears { get; set; }
    public int Bosses { get; set; }
    public List<string> Claimed { get; set; } = [];
}

internal sealed class BattlerState
{
    public int Wave { get; set; } = 1;
    public DateTime NextWaveAt { get; set; } = DateTime.UtcNow.AddSeconds(12);
    public bool Paused { get; set; }
    public DateTime? PausedAt { get; set; }
    public bool PrepareBoss { get; set; }
    public int? BossFailedWave { get; set; }
    public BossFight? Boss { get; set; }
    public int Alloy { get; set; }
    public List<ArmorModel> Armor { get; set; } = [];
    public Dictionary<string, string> Equipment { get; set; } = [];
    public List<string> Log { get; set; } = [];
}

internal sealed class BossFight
{
    public int Wave { get; set; }
    public double Health { get; set; }
    public double MaxHealth { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime LastAttackAt { get; set; }
}

internal sealed class ArmorModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Slot { get; set; } = "Chest";
    public string Rarity { get; set; } = "Common";
    public int Wave { get; set; }
    public int Count { get; set; } = 1;
    public bool AutoEquipped { get; set; }
    public Dictionary<string, int> Bonuses { get; set; } = [];
    public string Summary => $"{Rarity} {Name} · {Slot} · W{Wave}  ×{Count}";
}

internal sealed record CharacterModel(string Id, string Name, string Source, int Rarity, string Color);
internal sealed record FighterStats(double Hp, double Attack, double Crit, double Armor);
