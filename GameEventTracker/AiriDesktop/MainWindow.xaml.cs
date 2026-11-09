using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Ellipse = System.Windows.Shapes.Ellipse;
using Polygon = System.Windows.Shapes.Polygon;

namespace AiriDesktop;

public partial class MainWindow : Window
{
    private const int EventReward = 100;
    private const int BossReward = 250;
    private const int SinglePullCost = 100;
    private const int TenPullCost = 900;
    private readonly string _statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiriDesktop", "state.json");
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;
    private AppState _state;
    private bool _binding;
    private string? _selectedGameId;
    private string? _selectedCharacterId;
    private int _lastArenaWave = -1;

    private static readonly string[] Slots = ["Chest", "Legs", "Helm", "Boots", "Hands", "Weapon", "Cloak", "Rings1", "Ring2", "Trinket1", "Trinket2"];
    private static readonly string[] Rarities = ["Common", "Uncommon", "Rare", "Epic", "Unique"];
    private static readonly string[] BonusNames = ["Might", "Vitality", "Haste", "Aegis", "Focus"];
    private static readonly string[] RarityOrder = Rarities;
    private static readonly (double Hp, double Attack, double Crit, double Armor)[] RarityStats = [(120, 20, 5, 8), (165, 32, 9, 14), (220, 45, 14, 20)];
    private static readonly Dictionary<string, (double Hp, double Attack, double Crit, double Armor)> CharacterBonuses = new()
    {
        ["rift-runner"] = (0, 8, 0, 4), ["naruto"] = (0, 9, 2, 0), ["sailor-moon"] = (30, 0, 0, 4),
        ["kafka"] = (0, 4, 7, 0), ["miyabi"] = (0, 10, 4, 0), ["ellen"] = (0, 7, 6, 0),
        ["raiden"] = (0, 6, 5, 0), ["silver-wolf"] = (0, 0, 8, 3), ["furina"] = (25, 0, 3, 0), ["arabella"] = (0, 6, 0, 5)
    };
    private static readonly (string Name, string Sky, string Ground, string Glow)[] Biomes =
    [
        ("Mosslight Ruins", "#20372F", "#263B30", "#A5CF74"), ("Cinderfall Basin", "#442B28", "#382824", "#ED9368"),
        ("Glassfrost Shelf", "#263B48", "#263640", "#94D9E3"), ("Sable Observatory", "#302744", "#2C2639", "#C2A0E3")
    ];
    private static readonly string[] MonsterAdjectives = ["Shardbound", "Cinder-veiled", "Gloam-marked", "Frost-bitten", "Hollowglass"];
    private static readonly string[] MonsterForms = ["Riftling", "Gloomstalker", "Obelisk brute", "Veil sentinel", "Shard fiend"];
    private static readonly CharacterModel[] CharacterCatalog =
    [
        new("naruto", "Naruto Uzumaki", "Naruto", 5, "#D98545"), new("sailor-moon", "Sailor Moon", "Sailor Moon", 5, "#D881A8"),
        new("tanjiro", "Tanjiro Kamado", "Demon Slayer", 4, "#538C74"), new("anya", "Anya Forger", "Spy x Family", 3, "#C56D78"),
        new("anby", "Anby Demara", "Zenless Zone Zero", 4, "#7594AF"), new("nicole", "Nicole Demara", "Zenless Zone Zero", 4, "#D17A98"),
        new("ellen", "Ellen Joe", "Zenless Zone Zero", 5, "#5C8CA2"), new("miyabi", "Hoshimi Miyabi", "Zenless Zone Zero", 5, "#B77986"),
        new("amber", "Amber", "Genshin Impact", 3, "#DB8657"), new("xiangling", "Xiangling", "Genshin Impact", 4, "#CE6850"),
        new("furina", "Furina", "Genshin Impact", 5, "#6194B5"), new("march-7", "March 7th", "Honkai: Star Rail", 3, "#DA8B9B"),
        new("dan-heng", "Dan Heng", "Honkai: Star Rail", 4, "#578C89"), new("kafka", "Kafka", "Honkai: Star Rail", 5, "#9E6D91"),
        new("adelheid", "Adelheid", "Chaos Zero Nightmare", 4, "#937EAF"), new("amir", "Amir", "Chaos Zero Nightmare", 4, "#6687A1"),
        new("cassius", "Cassius", "Chaos Zero Nightmare", 3, "#B58A54"), new("arabella", "Arabella", "Chaos Zero Nightmare", 5, "#B36971"),
        new("levi", "Levi Ackerman", "Attack on Titan", 5, "#657B8C"), new("luffy", "Monkey D. Luffy", "One Piece", 5, "#CF6548"),
        new("nezuko", "Nezuko Kamado", "Demon Slayer", 4, "#C97B94"), new("killua", "Killua Zoldyck", "Hunter x Hunter", 4, "#62A9BD"),
        new("koleda", "Koleda Belobog", "Zenless Zone Zero", 4, "#D5805A"), new("zhu-yuan", "Zhu Yuan", "Zenless Zone Zero", 5, "#577AAB"),
        new("raiden", "Raiden Shogun", "Genshin Impact", 5, "#8164AA"), new("kazuha", "Kaedehara Kazuha", "Genshin Impact", 5, "#BC7C62"),
        new("himeko", "Himeko", "Honkai: Star Rail", 5, "#B36A53"), new("silver-wolf", "Silver Wolf", "Honkai: Star Rail", 5, "#588EC5"),
        new("beryl", "Beryl", "Chaos Zero Nightmare", 4, "#64A078"), new("anika", "Anika", "Chaos Zero Nightmare", 3, "#BA8056"),
        new("lisa", "Lisa", "Genshin Impact", 3, "#896DA9"), new("kaeya", "Kaeya", "Genshin Impact", 4, "#5879A7"),
        new("navia", "Navia", "Genshin Impact", 5, "#C5A15E"), new("acheron", "Acheron", "Honkai: Star Rail", 5, "#7D668B"),
        new("firefly", "Firefly", "Honkai: Star Rail", 5, "#6AA880"), new("pela", "Pela", "Honkai: Star Rail", 4, "#7594AF"),
        new("jane-doe", "Jane Doe", "Zenless Zone Zero", 5, "#9D7165"), new("qingyi", "Qingyi", "Zenless Zone Zero", 5, "#69A397"),
        new("burnice", "Burnice White", "Zenless Zone Zero", 5, "#DD774E"), new("frieren", "Frieren", "Frieren: Beyond Journey's End", 5, "#79A8A2"),
        new("gojo", "Satoru Gojo", "Jujutsu Kaisen", 5, "#7299D1"), new("goku", "Son Goku", "Dragon Ball", 5, "#DF854B"),
        new("rukia", "Rukia Kuchiki", "Bleach", 4, "#8293BD"), new("rengoku", "Kyojuro Rengoku", "Demon Slayer", 5, "#D57848"),
        new("saitama", "Saitama", "One-Punch Man", 5, "#CC5C4C")
    ];
    private static readonly Dictionary<string, FighterStats> BaseStats = new()
    {
        ["3"] = new(120, 20, 5, 8), ["4"] = new(165, 32, 9, 14), ["5"] = new(220, 45, 14, 20)
    };

    public MainWindow()
    {
        InitializeComponent();
        _state = LoadState();
        SeedFirstRun();
        EnsureWeekly();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        ArenaCanvas.SizeChanged += (_, _) => RenderArena();
        Loaded += (_, _) => RenderArena();
        Closing += (_, _) => { _timer.Stop(); SaveState(); };
        RefreshAll();
        _timer.Start();
    }

    private AppState LoadState()
    {
        try { if (File.Exists(_statePath)) return JsonSerializer.Deserialize<AppState>(File.ReadAllText(_statePath)) ?? new AppState(); }
        catch (Exception ex) { MessageBox.Show($"Couldn't read local progress.\n\n{ex.Message}", "Airi's", MessageBoxButton.OK, MessageBoxImage.Warning); }
        return new AppState();
    }

    private void SaveState()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
            File.WriteAllText(_statePath, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { MessageBox.Show($"Couldn't save local progress.\n\n{ex.Message}", "Airi's", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void SeedFirstRun()
    {
        if (_state.IsInitialized) return;
        var genshin = new GameModel { Name = "Genshin Impact" };
        var starRail = new GameModel { Name = "Honkai: Star Rail" };
        _state.Games.AddRange([genshin, starRail]);
        _state.DailyQuests.AddRange([
            new QuestModel { GameId = genshin.Id, Title = "Spend some Original Resin", Reward = 25 },
            new QuestModel { GameId = genshin.Id, Title = "Complete a commission", Reward = 20 },
            new QuestModel { GameId = starRail.Id, Title = "Complete a Calyx", Reward = 25 },
            new QuestModel { GameId = starRail.Id, Title = "Use daily training", Reward = 20 }
        ]);
        _state.Events.AddRange([
            new EventModel { GameId = genshin.Id, Title = "Weekend co-op session", Starts = DateTime.Today.AddDays(2), Ends = DateTime.Today.AddDays(4) },
            new EventModel { GameId = starRail.Id, Title = "Community game night", Starts = DateTime.Today.AddDays(5), Ends = DateTime.Today.AddDays(5) }
        ]);
        _state.Weekly.WeekOf = GetWeekKey();
        _state.Battler.Log.Add("Your Rift Runner is ready.");
        _state.IsInitialized = true;
        SaveState();
    }

    private static string GetWeekKey()
    {
        var today = DateTime.Today;
        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd");
    }

    private void EnsureWeekly()
    {
        if (_state.Weekly.WeekOf == GetWeekKey()) return;
        _state.Weekly = new WeeklyState { WeekOf = GetWeekKey() };
    }

    private void Tick()
    {
        EnsureWeekly();
        if (ProcessBattle()) SaveState();
        RefreshBattle();
        RefreshSummary();
        WeeklyGoalsList.ItemsSource = BuildWeeklyGoals();
    }

    private void RefreshAll()
    {
        _binding = true;
        EnsureWeekly();
        var selectedGame = _selectedGameId ?? _state.Games.FirstOrDefault()?.Id;
        GamesList.ItemsSource = _state.Games.ToList();
        GamesList.SelectedValuePath = nameof(GameModel.Id);
        GamesList.SelectedValue = selectedGame;
        _selectedGameId = selectedGame;
        EventGameInput.ItemsSource = _state.Games.ToList();
        EventGameInput.DisplayMemberPath = nameof(GameModel.Name);
        EventGameInput.SelectedValuePath = nameof(GameModel.Id);
        EventGameInput.SelectedValue = selectedGame;
        QuestGameInput.ItemsSource = _state.Games.ToList();
        QuestGameInput.DisplayMemberPath = nameof(GameModel.Name);
        QuestGameInput.SelectedValuePath = nameof(GameModel.Id);
        QuestGameInput.SelectedValue = selectedGame;
        RefreshTrackerLists();
        RefreshCharacters();
        RefreshBattle();
        RefreshSummary();
        _binding = false;
    }

    private void RefreshTrackerLists()
    {
        var gameId = _selectedGameId;
        EventsList.ItemsSource = _state.Events.Where(e => gameId is null || e.GameId == gameId).OrderBy(e => e.Starts)
            .Select(e => new EventRow(e, _state.Games.FirstOrDefault(g => g.Id == e.GameId)?.Name ?? "Unknown game")).ToList();
        var questGame = QuestGameInput.SelectedValue as string ?? gameId;
        DailyQuestsList.ItemsSource = _state.DailyQuests.Where(q => q.GameId == questGame).ToList();
        WeeklyGoalsList.ItemsSource = BuildWeeklyGoals();
    }

    private List<WeeklyGoalModel> BuildWeeklyGoals() =>
    [
        new("daily-clears", "Complete 10 daily quests", _state.Weekly.DailyClears, 10, 150, _state.Weekly.Claimed.Contains("daily-clears")),
        new("event-clears", "Complete 3 game events", _state.Weekly.EventClears, 3, 200, _state.Weekly.Claimed.Contains("event-clears")),
        new("boss-clears", "Defeat 1 Rift boss", _state.Weekly.Bosses, 1, 250, _state.Weekly.Claimed.Contains("boss-clears"))
    ];

    private void RefreshSummary()
    {
        BalanceText.Text = _state.Balance.ToString("N0");
        UpcomingText.Text = _state.Events.Count(e => !e.Done && e.Ends.Date >= DateTime.Today).ToString("00");
        var quests = _state.DailyQuests.Where(q => _state.Games.Any(g => g.Id == q.GameId)).ToList();
        DailyProgressText.Text = $"{quests.Count(q => q.CompletedOn == DateTime.Today.ToString("yyyy-MM-dd"))} / {quests.Count}";
        WaveSummaryText.Text = $"Wave {_state.Battler.Wave}";
    }

    private void RefreshCharacters()
    {
        CharactersList.ItemsSource = CharacterCatalog.Select(c =>
        {
            var count = _state.OwnedCharacters.GetValueOrDefault(c.Id);
            var duplicates = Math.Max(0, count - 1);
            var image = LoadPortrait(_state.CharacterImages.GetValueOrDefault(c.Id));
            var initials = string.Concat(c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0]));
            var label = count == 0 ? "NOT PULLED" : $"×{count}" + (duplicates > 0 ? $" · +{duplicates * 3} ATK" : "");
            return new CharacterCardModel(c.Id, c.Name, $"{c.Rarity}-star · {c.Source}", label, initials, c.Rarity, image);
        }).ToList();
        CollectionSummaryText.Text = $"{_state.OwnedCharacters.Count(kv => kv.Value > 0)} / {CharacterCatalog.Length} owned · 5-star pity {_state.PityFive}/50 · 4-star pity {_state.PityFour}/10\n3-star 70% · 4-star 24% · 5-star 6%\nDuplicates add +12 HP, +3 ATK, +1 CRIT, +1 ARMOR each.";
        GachaResultText.Text = _state.LastPulls.Count == 0 ? "Pulls cost 100 Pulse, or 900 for ten." : $"Last pull: {string.Join(", ", _state.LastPulls)}";
    }

    private static BitmapImage? LoadPortrait(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch { return null; }
    }

    private void AddGame_Click(object sender, RoutedEventArgs e)
    {
        var name = GameNameInput.Text.Trim();
        if (name.Length == 0 || _state.Games.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        var game = new GameModel { Name = name };
        _state.Games.Add(game);
        _state.DailyQuests.AddRange([new QuestModel { GameId = game.Id, Title = "Play for 15 minutes", Reward = 20 }, new QuestModel { GameId = game.Id, Title = "Complete one activity", Reward = 25 }]);
        _selectedGameId = game.Id;
        GameNameInput.Clear();
        SaveState();
        RefreshAll();
    }

    private void RemoveGame_Click(object sender, RoutedEventArgs e)
    {
        if (GamesList.SelectedItem is not GameModel game) return;
        if (MessageBox.Show($"Remove {game.Name} and its events and quests?", "Airi's", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _state.Games.Remove(game);
        _state.Events.RemoveAll(item => item.GameId == game.Id);
        _state.DailyQuests.RemoveAll(item => item.GameId == game.Id);
        _selectedGameId = _state.Games.FirstOrDefault()?.Id;
        SaveState();
        RefreshAll();
    }

    private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_binding) return;
        _selectedGameId = GamesList.SelectedValue as string;
        RefreshTrackerLists();
    }

    private void QuestGameInput_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_binding) RefreshTrackerLists(); }

    private void AddEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventGameInput.SelectedItem is not GameModel game || string.IsNullOrWhiteSpace(EventTitleInput.Text)) return;
        var start = EventStartInput.SelectedDate?.Date ?? DateTime.Today;
        var end = EventEndInput.SelectedDate?.Date ?? start;
        if (end < start) { MessageBox.Show("The end date must be on or after the start date.", "Airi's"); return; }
        _state.Events.Add(new EventModel { GameId = game.Id, Title = EventTitleInput.Text.Trim(), Starts = start, Ends = end });
        EventTitleInput.Clear();
        SaveState();
        RefreshAll();
    }

    private void CompleteEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventsList.SelectedItem is not EventRow row || row.Event.Done) return;
        row.Event.Done = true;
        if (!row.Event.RewardClaimed) { row.Event.RewardClaimed = true; _state.Balance += EventReward; _state.Weekly.EventClears++; }
        SaveState();
        RefreshAll();
    }

    private void RemoveEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventsList.SelectedItem is not EventRow row) return;
        _state.Events.Remove(row.Event);
        SaveState();
        RefreshAll();
    }

    private void AddDailyQuest_Click(object sender, RoutedEventArgs e)
    {
        if (QuestGameInput.SelectedItem is not GameModel game || string.IsNullOrWhiteSpace(DailyQuestNameInput.Text)) return;
        _state.DailyQuests.Add(new QuestModel { GameId = game.Id, Title = DailyQuestNameInput.Text.Trim(), Reward = 25 });
        DailyQuestNameInput.Clear(); SaveState(); RefreshAll();
    }

    private void CompleteDaily_Click(object sender, RoutedEventArgs e)
    {
        if (DailyQuestsList.SelectedItem is not QuestModel quest || quest.CompletedOn == DateTime.Today.ToString("yyyy-MM-dd")) return;
        quest.CompletedOn = DateTime.Today.ToString("yyyy-MM-dd"); quest.EarnedOn = quest.CompletedOn;
        _state.Balance += quest.Reward; _state.Weekly.DailyClears++; SaveState(); RefreshAll();
    }

    private void RemoveDailyQuest_Click(object sender, RoutedEventArgs e)
    {
        if (DailyQuestsList.SelectedItem is not QuestModel quest) return;
        _state.DailyQuests.Remove(quest);
        SaveState();
        RefreshAll();
    }

    private void ClaimWeekly_Click(object sender, RoutedEventArgs e)
    {
        if (WeeklyGoalsList.SelectedItem is not WeeklyGoalModel goal || goal.Claimed || goal.Progress < goal.Target) return;
        _state.Weekly.Claimed.Add(goal.Id); _state.Balance += goal.Reward; SaveState(); RefreshAll();
    }

    private CharacterModel DrawCharacter()
    {
        int rarity;
        if (_state.PityFive + 1 >= 50 || _random.NextDouble() < .06) { rarity = 5; _state.PityFive = 0; _state.PityFour = 0; }
        else
        {
            _state.PityFive++;
            if (_state.PityFour + 1 >= 10 || _random.NextDouble() < .24) { rarity = 4; _state.PityFour = 0; }
            else { rarity = 3; _state.PityFour++; }
        }
        var pool = CharacterCatalog.Where(c => c.Rarity == rarity).ToArray();
        return pool[_random.Next(pool.Length)];
    }

    private void PullOne_Click(object sender, RoutedEventArgs e) => Pull(1);
    private void PullTen_Click(object sender, RoutedEventArgs e) => Pull(10);
    private void Pull(int count)
    {
        var cost = count == 1 ? SinglePullCost : TenPullCost;
        if (_state.Balance < cost) { GachaResultText.Text = $"Need {cost} Pulse for that pull."; return; }
        _state.Balance -= cost;
        _state.LastPulls = Enumerable.Range(0, count).Select(_ => DrawCharacter()).Select(c =>
        {
            _state.OwnedCharacters[c.Id] = _state.OwnedCharacters.GetValueOrDefault(c.Id) + 1;
            return $"{c.Name} {new string('★', c.Rarity)}";
        }).ToList();
        SaveState(); RefreshAll();
    }

    private void CharactersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CharactersList.SelectedItem is CharacterCardModel card) _selectedCharacterId = card.Id;
    }

    private void ImportPortrait_Click(object sender, RoutedEventArgs e)
    {
        if (CharactersList.SelectedItem is not CharacterCardModel card) return;
        var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif" };
        if (dialog.ShowDialog(this) != true) return;
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiriDesktop", "portraits");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, card.Id + Path.GetExtension(dialog.FileName));
        File.Copy(dialog.FileName, destination, true);
        _state.CharacterImages[card.Id] = destination;
        SaveState(); RefreshCharacters();
    }

    private List<(CharacterModel Character, int Copies)> GetParty()
    {
        var pulled = CharacterCatalog.Where(c => _state.OwnedCharacters.GetValueOrDefault(c.Id) > 0)
            .Select(c => (Character: c, Copies: _state.OwnedCharacters[c.Id])).ToList();
        return pulled.Count > 0 ? pulled : [(new CharacterModel("rift-runner", "Rift Runner", "Airi's starter", 3, "#87B878"), 1)];
    }

    private FighterStats GetPartyStats()
    {
        var total = new FighterStats(0, 0, 0, 0);
        foreach (var member in GetParty())
        {
            var baseStats = RarityStats[member.Character.Rarity - 3];
            var special = CharacterBonuses.GetValueOrDefault(member.Character.Id);
            var duplicates = Math.Max(0, member.Copies - 1);
            total = total with { Hp = total.Hp + baseStats.Hp + special.Hp + duplicates * 12, Attack = total.Attack + baseStats.Attack + special.Attack + duplicates * 3, Crit = total.Crit + baseStats.Crit + special.Crit + duplicates, Armor = total.Armor + baseStats.Armor + special.Armor + duplicates };
        }
        foreach (var slot in Slots)
        {
            if (!_state.Battler.Equipment.TryGetValue(slot, out var id)) continue;
            var item = _state.Battler.Armor.FirstOrDefault(a => a.Id == id);
            if (item is null) continue;
            var rank = Math.Max(1, Array.IndexOf(Rarities, item.Rarity) + 1);
            total = slot == "Weapon" ? total with { Attack = total.Attack + rank * 6 + item.Wave / 40 * 2 } : total with { Armor = total.Armor + rank * 3 + item.Wave / 40 };
            foreach (var bonus in item.Bonuses)
                total = bonus.Key switch { "Might" => total with { Attack = total.Attack + bonus.Value }, "Vitality" => total with { Hp = total.Hp + bonus.Value * 4 }, "Haste" or "Focus" => total with { Crit = total.Crit + bonus.Value }, "Aegis" => total with { Armor = total.Armor + bonus.Value }, _ => total };
        }
        return total with { Crit = Math.Min(100, total.Crit) };
    }

    private double PartyDps() { var stats = GetPartyStats(); return Math.Max(1, stats.Attack * (.55 + stats.Crit / 100)); }

    private void PauseBattle_Click(object sender, RoutedEventArgs e)
    {
        var battle = _state.Battler;
        if (battle.Paused)
        {
            var pause = DateTime.UtcNow - (battle.PausedAt ?? DateTime.UtcNow);
            battle.NextWaveAt += pause;
            if (battle.Boss is not null) { battle.Boss.Deadline += pause; battle.Boss.LastAttackAt += pause; }
            battle.Paused = false; battle.PausedAt = null;
        }
        else { battle.Paused = true; battle.PausedAt = DateTime.UtcNow; }
        SaveState(); RefreshBattle();
    }

    private void ReturnBoss_Click(object sender, RoutedEventArgs e)
    {
        var battle = _state.Battler;
        if (!battle.PrepareBoss || battle.BossFailedWave is null) return;
        battle.Wave = battle.BossFailedWave.Value; battle.PrepareBoss = false; battle.BossFailedWave = null; battle.Boss = null; battle.NextWaveAt = DateTime.UtcNow;
        SaveState(); RefreshBattle();
    }

    private bool ProcessBattle()
    {
        var battle = _state.Battler; var now = DateTime.UtcNow;
        if (battle.Paused) return false;
        if (battle.PrepareBoss)
        {
            if (now < battle.NextWaveAt) return false;
            ProcessRegularWave(battle.Wave); battle.NextWaveAt = now.AddSeconds(12); return true;
        }
        if (battle.Wave % 20 == 0)
        {
            if (battle.Boss is null)
            {
                var hp = 250 + battle.Wave * 12;
                battle.Boss = new BossFight { Wave = battle.Wave, Health = hp, MaxHealth = hp, Deadline = now.AddSeconds(30), LastAttackAt = now };
                AddBattleLog($"Rift Warden entered wave {battle.Wave}. 30 seconds to defeat it."); return true;
            }
            var fight = battle.Boss;
            fight.Health -= PartyDps() * Math.Clamp((now - fight.LastAttackAt).TotalSeconds, 0, 1.5);
            fight.LastAttackAt = now;
            if (fight.Health <= 0)
            {
                var item = AddArmorDrop(fight.Wave, true); _state.Balance += BossReward; _state.Weekly.Bosses++;
                AddBattleLog($"Boss defeated: {item.Rarity} {item.Name}; +{BossReward} Pulse.");
                battle.Wave = fight.Wave + 1; battle.Boss = null; battle.NextWaveAt = now.AddSeconds(12); return true;
            }
            if (now >= fight.Deadline)
            {
                battle.BossFailedWave = fight.Wave; battle.Wave = fight.Wave - 1; battle.PrepareBoss = true; battle.Boss = null; battle.NextWaveAt = now.AddSeconds(12);
                AddBattleLog($"Boss escaped. Farm wave {battle.Wave}, then return."); return true;
            }
            return true;
        }
        if (now < battle.NextWaveAt) return false;
        ProcessRegularWave(battle.Wave); battle.Wave++; battle.NextWaveAt = now.AddSeconds(12); return true;
    }

    private void ProcessRegularWave(int wave)
    {
        if (_random.NextDouble() < Math.Min(.65, .12 + wave / 40 * .05))
        {
            var item = AddArmorDrop(wave, false);
            AddBattleLog($"Wave {wave}: {item.Rarity} {item.Name}{(item.AutoEquipped ? " · auto-equipped" : "")}");
        }
        if (wave % 3 == 0) { _state.Battler.Alloy += 3; AddBattleLog($"Wave {wave}: +3 alloy"); }
    }

    private void AddBattleLog(string line)
    {
        _state.Battler.Log.Insert(0, line);
        if (_state.Battler.Log.Count > 8) _state.Battler.Log.RemoveRange(8, _state.Battler.Log.Count - 8);
    }

    private ArmorModel AddArmorDrop(int wave, bool boss)
    {
        var tier = (wave - 1) / 40;
        var weights = new List<(string Name, double Weight)> { ("Common", Math.Max(10, 65 - tier * 7)), ("Uncommon", 25), ("Rare", 8 + tier * 4), ("Epic", 2 + tier * 3) };
        if (boss) weights.Add(("Unique", 4 + Math.Min(8, wave / 100)));
        var roll = _random.NextDouble() * weights.Sum(w => w.Weight); var rarity = weights[^1].Name;
        foreach (var weight in weights) { roll -= weight.Weight; if (roll < 0) { rarity = weight.Name; break; } }
        var slot = Slots[_random.Next(Slots.Length)];
        var prefix = new Dictionary<string, string> { ["Common"] = "Riftworn", ["Uncommon"] = "Tempered", ["Rare"] = "Runed", ["Epic"] = "Warden's", ["Unique"] = "Starfall" }[rarity];
        var name = $"{prefix} {slot}";
        var old = rarity == "Unique" ? null : _state.Battler.Armor.FirstOrDefault(a => a.Rarity == rarity && a.Slot == slot && a.Name == name);
        if (old is not null) { old.Count++; old.Wave = wave; AutoEquip(old); return old; }
        var item = new ArmorModel { Name = name, Slot = slot, Rarity = rarity, Wave = wave };
        if (rarity == "Unique") foreach (var bonus in BonusNames.OrderBy(_ => _random.Next()).Take(wave >= 100 ? 4 : 3)) item.Bonuses[bonus] = 2 + wave / 10 + _random.Next(4 + wave / 10);
        _state.Battler.Armor.Add(item); AutoEquip(item); return item;
    }

    private void AutoEquip(ArmorModel item)
    {
        var current = _state.Battler.Armor.FirstOrDefault(a => a.Id == _state.Battler.Equipment.GetValueOrDefault(item.Slot));
        if (current is not null && ArmorScore(current) >= ArmorScore(item)) return;
        _state.Battler.Equipment[item.Slot] = item.Id; item.AutoEquipped = true;
    }

    private static double ArmorScore(ArmorModel item) => Math.Max(0, Array.IndexOf(RarityOrder, item.Rarity)) * 10000 + item.Wave * 10 + item.Bonuses.Values.Sum() * 100;

    private void RefreshBattle()
    {
        var battle = _state.Battler; var boss = !battle.PrepareBoss && battle.Wave % 20 == 0;
        var party = GetParty();
        PartyList.ItemsSource = party.Select(p => new PartyMemberModel(p.Character.Id, p.Character.Name, p.Character.Source, p.Character.Rarity, p.Copies, $"{p.Character.Name} · {p.Character.Rarity}-star · {p.Copies} copies")).ToList();
        var stats = GetPartyStats();
        PartyStatsText.Text = $"PARTY TOTAL · HP {stats.Hp:N0}   ATK {stats.Attack:N0}   CRIT {stats.Crit:0}%   ARMOR {stats.Armor:N0}";
        WaveText.Text = battle.PrepareBoss ? $"FARM WAVE {battle.Wave} · BOSS {battle.BossFailedWave} FAILED" : boss ? $"BOSS WAVE {battle.Wave}" : $"WAVE {battle.Wave} · INFINITE DESCENT";
        PauseBattleButton.Content = battle.Paused ? "Resume" : "Pause"; ReturnBossButton.Visibility = battle.PrepareBoss ? Visibility.Visible : Visibility.Collapsed;
        EnemyStatsText.Text = $"Level {battle.Wave} · HP {(100 + (battle.Wave - 1) * 65) * (boss ? 12 : 1):N0} · ATK {(12 + (battle.Wave - 1) * 7) * (boss ? 3 : 1):N0}";
        if (boss && battle.Boss is not null)
        {
            BossHealthBar.Visibility = Visibility.Visible; BossHealthBar.Maximum = battle.Boss.MaxHealth; BossHealthBar.Value = Math.Clamp(battle.Boss.Health, 0, battle.Boss.MaxHealth);
            BossTimeText.Visibility = Visibility.Visible; BossTimeText.Text = battle.Paused ? "Boss paused" : $"Deadline · {Math.Max(0, (int)Math.Ceiling((battle.Boss.Deadline - DateTime.UtcNow).TotalSeconds))}s";
        }
        else { BossHealthBar.Visibility = Visibility.Collapsed; BossTimeText.Visibility = Visibility.Collapsed; BattleStatusText.Text = battle.PrepareBoss ? "Farm this level; return when ready." : battle.Paused ? "Expedition paused" : $"Next wave in {Math.Max(0, (int)Math.Ceiling((battle.NextWaveAt - DateTime.UtcNow).TotalSeconds))}s"; }
        ArmorList.ItemsSource = battle.Armor.OrderByDescending(ArmorScore).ToList(); WaveSummaryText.Text = $"Wave {battle.Wave}"; RenderArena();
    }

    private void RenderArena()
    {
        if (ArenaCanvas is null || ArenaCanvas.ActualWidth < 10) return;
        var wave = Math.Max(1, _state.Battler.Wave); var biome = Biomes[((wave - 1) / 5) % Biomes.Length]; var boss = !_state.Battler.PrepareBoss && wave % 20 == 0;
        BiomeText.Text = biome.Name; MonsterNameText.Text = boss ? "Rift Warden" : $"{MonsterAdjectives[(wave * 3 + wave / 5) % MonsterAdjectives.Length]} {MonsterForms[(wave * 7 + wave / 2) % MonsterForms.Length]}";
        MonsterGlyphText.Text = boss ? "♜" : wave % 3 == 0 ? "◈" : "◆"; var sky = ColorFrom(biome.Sky); var ground = ColorFrom(biome.Ground); var glow = ColorFrom(biome.Glow);
        ArenaCard.Background = new LinearGradientBrush(sky, ground, 90); MonsterGlyphText.Foreground = new SolidColorBrush(glow);
        if (_lastArenaWave == wave) return;
        _lastArenaWave = wave; ArenaCanvas.Children.Clear(); var random = new Random(wave * 37); var width = ArenaCanvas.ActualWidth; var height = ArenaCanvas.ActualHeight;
        var moon = new Ellipse { Width = 70, Height = 70, Fill = new SolidColorBrush(glow), Opacity = .35 }; Canvas.SetLeft(moon, width * .73); Canvas.SetTop(moon, 25 + random.Next(25)); ArenaCanvas.Children.Add(moon);
        for (var layer = 0; layer < 3; layer++)
        {
            var points = new PointCollection { new(0, height) }; var step = width / 8;
            for (var i = 0; i <= 8; i++) points.Add(new Point(i * step, height * (.42 + random.NextDouble() * .2) + layer * 8));
            points.Add(new Point(width, height));
            ArenaCanvas.Children.Add(new Polygon { Points = points, Fill = new SolidColorBrush(layer == 0 ? Color.FromRgb(17, 24, 20) : ground), Opacity = .45 + layer * .16 });
        }
        var monster = new Polygon { Points = new PointCollection([new(0, 36), new(18, 8), new(35, 25), new(53, 0), new(70, 27), new(92, 14), new(100, 54), new(82, 68), new(93, 115), new(60, 98), new(43, 130), new(25, 92), new(3, 108), new(18, 68)]), Fill = new LinearGradientBrush(glow, Color.FromRgb(38, 49, 39), 90) };
        Canvas.SetLeft(monster, width * .7); Canvas.SetTop(monster, Math.Max(0, height - 145)); ArenaCanvas.Children.Add(monster);
    }

    private static Color ColorFrom(string value) => (Color)ColorConverter.ConvertFromString(value)!;
}

internal sealed record EventRow(EventModel Event, string GameName)
{
    public string Summary => $"{Event.Title} · {GameName} · {Event.Starts:MMM d}–{Event.Ends:MMM d} · {(Event.Done ? "Completed" : "Upcoming")}";
}