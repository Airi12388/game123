using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
    private const int DefaultQuestReward = 25;
    // AIRI_DATA_DIR lets you point the app at a separate folder (e.g. for testing) without touching your real save.
    private static readonly string AppFolder = Environment.GetEnvironmentVariable("AIRI_DATA_DIR") is { Length: > 0 } custom
        ? custom
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiriDesktop");
    private static readonly string PortraitFolder = Path.Combine(AppFolder, "portraits");
    private readonly string _statePath = Path.Combine(AppFolder, "state.json");
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, ImageSource?> _portraitCache = [];
    private AppState _state;
    private bool _ready;
    private bool _binding;
    private string _selectedGameId = "";
    private string _eventFilter = "all";
    private int _rarityFilter;
    private bool _ownedOnly;
    private string? _selectedCharacterId;
    private string _battleListKey = "";
    private DateTime _lastDay = DateTime.Today;
    private int _lastArenaWave = -1;
    private int _ticks;

    private static readonly string[] Slots = ["Chest", "Legs", "Helm", "Boots", "Hands", "Weapon", "Cloak", "Rings1", "Ring2", "Trinket1", "Trinket2"];
    private static readonly string[] Rarities = ["Common", "Uncommon", "Rare", "Epic", "Unique"];
    private static readonly string[] BonusNames = ["Might", "Vitality", "Haste", "Aegis", "Focus"];
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
    private static readonly CharacterModel StarterFighter = new("rift-runner", "Rift Runner", "Airi's starter", 3, "#87B878");
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

    public MainWindow()
    {
        InitializeComponent();
        _state = LoadState();
        SeedFirstRun();
        EnsureWeekly();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        ArenaCanvas.SizeChanged += (_, _) => { _lastArenaWave = -1; RenderArena(); };
        Closing += (_, _) => { _timer.Stop(); SaveState(); };
        EventStartInput.SelectedDate = EventEndInput.SelectedDate = DateTime.Today;
        _ready = true;
        RefreshAll();
        _timer.Start();
    }

    private static string TodayKey => DateTime.Today.ToString("yyyy-MM-dd");
    private static string? TagOf(object sender) => (sender as FrameworkElement)?.Tag as string;

    // ───────────────────────── Persistence ─────────────────────────

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
            Directory.CreateDirectory(AppFolder);
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

    private bool EnsureWeekly()
    {
        if (_state.Weekly.WeekOf == GetWeekKey()) return false;
        _state.Weekly = new WeeklyState { WeekOf = GetWeekKey() };
        return true;
    }

    // ───────────────────────── Refresh ─────────────────────────

    private void Tick()
    {
        _ticks++;
        if (DateTime.Today != _lastDay) { _lastDay = DateTime.Today; EnsureWeekly(); RefreshAll(); }
        else if (EnsureWeekly()) RefreshWeekly();
        if (ProcessBattle()) SaveState();
        RefreshBattle();
        RefreshSummary();
        if (_ticks % 60 == 0) RefreshEvents(); // keep "Ends in …" countdowns current
    }

    private void RefreshAll()
    {
        _binding = true;
        EnsureWeekly();
        if (_selectedGameId != "" && _state.Games.All(g => g.Id != _selectedGameId)) _selectedGameId = "";

        var rows = new List<GameRowModel> { new("", "All games", Palette.Muted, _state.Events.Count.ToString(), false) };
        rows.AddRange(_state.Games.Select((g, i) => new GameRowModel(g.Id, g.Name, Palette.Game(i), _state.Events.Count(e => e.GameId == g.Id).ToString(), true)));
        GamesList.ItemsSource = rows;
        GamesList.SelectedValue = _selectedGameId;

        var fallback = _selectedGameId != "" ? _selectedGameId : _state.Games.FirstOrDefault()?.Id;
        var eventGame = EventGameInput.SelectedValue as string;
        EventGameInput.ItemsSource = _state.Games.ToList();
        EventGameInput.SelectedValue = _selectedGameId != "" ? _selectedGameId : IsGame(eventGame) ? eventGame : fallback;
        var questGame = QuestGameInput.SelectedValue as string;
        QuestGameInput.ItemsSource = _state.Games.ToList();
        QuestGameInput.SelectedValue = IsGame(questGame) ? questGame : fallback;

        RefreshTrackerLists();
        RefreshCharacters();
        RefreshBattle(force: true);
        RefreshSummary();
        _binding = false;
    }

    private bool IsGame(string? id) => id is not null && _state.Games.Any(g => g.Id == id);

    private void RefreshTrackerLists()
    {
        RefreshEvents();
        RefreshQuests();
        RefreshWeekly();
    }

    private void RefreshEvents()
    {
        var today = DateTime.Today;
        var events = _state.Events.Where(e => _selectedGameId == "" || e.GameId == _selectedGameId);
        events = _eventFilter switch
        {
            "upcoming" => events.Where(e => !e.Done && e.Ends.Date >= today),
            "done" => events.Where(e => e.Done),
            _ => events
        };
        var rows = events.OrderBy(e => e.Starts).Select(BuildEventRow).ToList();
        EventsList.ItemsSource = rows;
        EventCountText.Text = rows.Count.ToString();
        EventsEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private EventRowModel BuildEventRow(EventModel e)
    {
        var index = _state.Games.FindIndex(g => g.Id == e.GameId);
        var gameName = index >= 0 ? _state.Games[index].Name : "Unknown game";
        var today = DateTime.Today;
        var (status, brush) = e.Done ? ("COMPLETED", Palette.Green)
            : e.Ends.Date < today ? ("ENDED", Palette.Muted)
            : e.Starts.Date <= today ? ("LIVE NOW", Palette.Red)
            : ("UPCOMING", Palette.Blue);
        var dates = e.Starts.Date == e.Ends.Date ? $"{e.Starts:MMM d}" : $"{e.Starts:MMM d} – {e.Ends:MMM d}";
        return new EventRowModel(e.Id, e.Title, gameName, Palette.Game(index), dates, status, brush, Countdown(e), e.Done);
    }

    private static string Countdown(EventModel e)
    {
        var now = DateTime.Now;
        if (e.Done || e.Ends.Date < now.Date) return "";
        var startsLater = e.Starts.Date > now.Date;
        var span = (startsLater ? e.Starts.Date : e.Ends.Date.AddDays(1)) - now;
        var text = span.TotalDays >= 1 ? $"{(int)span.TotalDays}d {span.Hours}h" : span.TotalHours >= 1 ? $"{span.Hours}h {span.Minutes}m" : $"{Math.Max(1, span.Minutes)}m";
        return $"{(startsLater ? "Starts in" : "Ends in")} {text}";
    }

    private void RefreshQuests()
    {
        var questGame = QuestGameInput.SelectedValue as string;
        var today = TodayKey;
        var rows = _state.DailyQuests.Where(q => q.GameId == questGame).Select(q => new QuestRowModel(q.Id, q.Title, q.Reward, q.CompletedOn == today)).ToList();
        DailyQuestsList.ItemsSource = rows;
        QuestsEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private List<WeeklyGoalRow> BuildWeeklyGoals() =>
    [
        new("daily-clears", "Complete 10 daily quests", _state.Weekly.DailyClears, 10, 150, _state.Weekly.Claimed.Contains("daily-clears")),
        new("event-clears", "Complete 3 game events", _state.Weekly.EventClears, 3, 200, _state.Weekly.Claimed.Contains("event-clears")),
        new("boss-clears", "Defeat 1 Rift boss", _state.Weekly.Bosses, 1, 250, _state.Weekly.Claimed.Contains("boss-clears"))
    ];

    private void RefreshWeekly()
    {
        var goals = BuildWeeklyGoals();
        WeeklyGoalsList.ItemsSource = goals;
        WeeklyDoneText.Text = $"{goals.Count(g => g.Claimed)}/{goals.Count}";
    }

    private void RefreshSummary()
    {
        BalanceText.Text = _state.Balance.ToString("N0");
        var quests = _state.DailyQuests.Where(q => IsGame(q.GameId)).ToList();
        ChipDailyText.Text = $"{quests.Count(q => q.CompletedOn == TodayKey)}/{quests.Count}";
        ChipWeeklyText.Text = BuildWeeklyGoals().Count(g => g.Ready).ToString();
        ChipEventsText.Text = _state.Events.Count(e => !e.Done && e.Ends.Date >= DateTime.Today).ToString("00");
        ChipPityText.Text = (50 - _state.PityFive).ToString();
        ChipWaveText.Text = $"W{_state.Battler.Wave}";
        TopWaveText.Text = $"W{_state.Battler.Wave}";
        HeroAffordText.Text = (_state.Balance / SinglePullCost).ToString();
        HeroPullButton.IsEnabled = PullOneButton.IsEnabled = _state.Balance >= SinglePullCost;
        HeroPullTenButton.IsEnabled = PullTenButton.IsEnabled = _state.Balance >= TenPullCost;
    }

    // ───────────────────────── Navigation & filters ─────────────────────────

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (TrackerView is null || BoostersView is null || RiftView is null) return;
        TrackerView.Visibility = NavTracker.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        BoostersView.Visibility = NavBoosters.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        RiftView.Visibility = NavRift.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        if (RiftView.Visibility == Visibility.Visible) { _lastArenaWave = -1; Dispatcher.BeginInvoke(RenderArena, DispatcherPriority.Loaded); }
    }

    private void EventFilter_Checked(object sender, RoutedEventArgs e)
    {
        _eventFilter = TagOf(sender) ?? "all";
        if (_ready) RefreshEvents();
    }

    private void RarityFilter_Checked(object sender, RoutedEventArgs e)
    {
        _rarityFilter = int.TryParse(TagOf(sender), out var stars) ? stars : 0;
        if (_ready) RefreshCharacters();
    }

    private void OwnedOnly_Changed(object sender, RoutedEventArgs e)
    {
        _ownedOnly = OwnedOnlyToggle.IsChecked == true;
        if (_ready) RefreshCharacters();
    }

    // ───────────────────────── Tracker actions ─────────────────────────

    private void GameNameInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AddGame_Click(sender, e); }
    private void EventTitleInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AddEvent_Click(sender, e); }
    private void DailyQuestNameInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AddDailyQuest_Click(sender, e); }

    private void AddGame_Click(object sender, RoutedEventArgs e)
    {
        var name = GameNameInput.Text.Trim();
        if (name.Length == 0) return;
        if (_state.Games.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("That game is already in your library.", "Airi's");
            return;
        }
        var game = new GameModel { Name = name };
        _state.Games.Add(game);
        _state.DailyQuests.AddRange([new QuestModel { GameId = game.Id, Title = "Play for 15 minutes", Reward = 20 }, new QuestModel { GameId = game.Id, Title = "Complete one activity", Reward = 25 }]);
        _selectedGameId = game.Id;
        QuestGameInput.SelectedValue = game.Id;
        GameNameInput.Clear();
        SaveState();
        RefreshAll();
    }

    private void RemoveGame_Click(object sender, RoutedEventArgs e)
    {
        var game = _state.Games.FirstOrDefault(g => g.Id == TagOf(sender));
        if (game is null) return;
        if (MessageBox.Show($"Remove {game.Name} and its events and quests?", "Airi's", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _state.Games.Remove(game);
        _state.Events.RemoveAll(item => item.GameId == game.Id);
        _state.DailyQuests.RemoveAll(item => item.GameId == game.Id);
        if (_selectedGameId == game.Id) _selectedGameId = "";
        SaveState();
        RefreshAll();
    }

    private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_binding || !_ready) return;
        _selectedGameId = GamesList.SelectedValue as string ?? "";
        if (_selectedGameId != "")
        {
            _binding = true;
            EventGameInput.SelectedValue = _selectedGameId;
            QuestGameInput.SelectedValue = _selectedGameId;
            _binding = false;
        }
        RefreshTrackerLists();
    }

    private void QuestGameInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_binding && _ready) RefreshQuests();
    }

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
        var ev = _state.Events.FirstOrDefault(x => x.Id == TagOf(sender));
        if (ev is null || ev.Done) return;
        ev.Done = true;
        if (!ev.RewardClaimed) { ev.RewardClaimed = true; _state.Balance += EventReward; _state.Weekly.EventClears++; }
        SaveState();
        RefreshAll();
    }

    private void RemoveEvent_Click(object sender, RoutedEventArgs e)
    {
        _state.Events.RemoveAll(x => x.Id == TagOf(sender));
        SaveState();
        RefreshAll();
    }

    private void AddDailyQuest_Click(object sender, RoutedEventArgs e)
    {
        if (QuestGameInput.SelectedItem is not GameModel game || string.IsNullOrWhiteSpace(DailyQuestNameInput.Text)) return;
        var reward = int.TryParse(QuestRewardInput.Text.Trim(), out var value) ? Math.Clamp(value, 1, 1000) : DefaultQuestReward;
        _state.DailyQuests.Add(new QuestModel { GameId = game.Id, Title = DailyQuestNameInput.Text.Trim(), Reward = reward });
        DailyQuestNameInput.Clear();
        QuestRewardInput.Clear();
        SaveState();
        RefreshQuests();
        RefreshSummary();
    }

    private void CompleteDaily_Click(object sender, RoutedEventArgs e)
    {
        var quest = _state.DailyQuests.FirstOrDefault(q => q.Id == TagOf(sender));
        if (quest is null || quest.CompletedOn == TodayKey) return;
        quest.CompletedOn = TodayKey;
        quest.EarnedOn = quest.CompletedOn;
        _state.Balance += quest.Reward;
        _state.Weekly.DailyClears++;
        SaveState();
        RefreshQuests();
        RefreshWeekly();
        RefreshSummary();
    }

    private void RemoveDailyQuest_Click(object sender, RoutedEventArgs e)
    {
        _state.DailyQuests.RemoveAll(q => q.Id == TagOf(sender));
        SaveState();
        RefreshQuests();
        RefreshSummary();
    }

    private void ClaimWeekly_Click(object sender, RoutedEventArgs e)
    {
        var goal = BuildWeeklyGoals().FirstOrDefault(g => g.Id == TagOf(sender));
        if (goal is null || !goal.Ready) return;
        _state.Weekly.Claimed.Add(goal.Id);
        _state.Balance += goal.Reward;
        SaveState();
        RefreshWeekly();
        RefreshSummary();
    }

    // ───────────────────────── Characters & cards ─────────────────────────

    private static FighterStats CharacterStats(CharacterModel c, int copies)
    {
        var b = RarityStats[Math.Clamp(c.Rarity - 3, 0, 2)];
        var s = CharacterBonuses.GetValueOrDefault(c.Id);
        var d = Math.Max(0, copies - 1);
        return new(b.Hp + s.Hp + d * 12, b.Attack + s.Attack + d * 3, b.Crit + s.Crit + d, b.Armor + s.Armor + d);
    }

    private CharacterCardModel BuildCard(CharacterModel c, int copies)
    {
        var stats = CharacterStats(c, Math.Max(1, copies));
        var rarity = Palette.Rarity(c.Rarity);
        var index = Array.FindIndex(CharacterCatalog, x => x.Id == c.Id);
        var number = index >= 0 ? $"#{index + 1:00}/{CharacterCatalog.Length}" : "#STARTER";
        var initials = string.Concat(c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0]));
        return new CharacterCardModel(c.Id, c.Name, c.Source, c.Rarity, rarity.Label, rarity.Letter, rarity.Accent, rarity.Surface,
            Palette.Art(c.Color), initials, number, (int)stats.Attack, (int)(stats.Crit + stats.Armor), copies, Portrait(c.Id));
    }

    private void RefreshCharacters()
    {
        var cards = CharacterCatalog.Select(c => BuildCard(c, _state.OwnedCharacters.GetValueOrDefault(c.Id))).ToList();
        var visible = cards
            .Where(c => (_rarityFilter == 0 || c.Rarity == _rarityFilter) && (!_ownedOnly || c.IsOwned))
            .OrderByDescending(c => c.IsOwned).ThenByDescending(c => c.Rarity).ThenBy(c => c.Name)
            .ToList();
        var keep = _selectedCharacterId;
        CharactersList.ItemsSource = visible;
        CharactersList.SelectedItem = visible.FirstOrDefault(c => c.Id == keep);

        var owned = cards.Count(c => c.IsOwned);
        CollectionCountText.Text = $"{owned}/{cards.Count}";
        TileOwnedText.Text = $"{owned}/{cards.Count}";
        TileLegendText.Text = (50 - _state.PityFive).ToString();
        TileEpicText.Text = (10 - _state.PityFour).ToString();
        PoolSubtitleText.Text = $"Every character · {cards.Count} cards";

        // Hero showcase: your three best cards, topped up with Legendaries you haven't pulled yet.
        var featured = cards.Where(c => c.IsOwned).OrderByDescending(c => c.Rarity).ThenByDescending(c => c.Copies).Take(3).ToList();
        featured.AddRange(cards.Where(c => c.Rarity == 5 && !featured.Contains(c)).Take(3 - featured.Count));
        HeroCenter.Content = featured[0];
        HeroLeft.Content = featured[1];
        HeroRight.Content = featured[2];

        var last = _state.LastPulls
            .Select(entry => CharacterCatalog.FirstOrDefault(c => c.Id == entry) ?? CharacterCatalog.FirstOrDefault(c => entry.StartsWith(c.Name + " ")))
            .OfType<CharacterModel>()
            .Select(c => cards.First(card => card.Id == c.Id))
            .ToList();
        LastPullsList.ItemsSource = last;
        LastPullsEmpty.Visibility = last.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshSelectedCard();
    }

    private void RefreshSelectedCard()
    {
        var character = CharacterCatalog.FirstOrDefault(c => c.Id == _selectedCharacterId);
        SelectedCardText.Text = character?.Name ?? "Select a card";
        SetArtButton.IsEnabled = character is not null;
        RemoveArtButton.IsEnabled = character is not null && _state.CharacterImages.ContainsKey(character.Id);
    }

    private void CharactersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CharactersList.SelectedItem is CharacterCardModel card) _selectedCharacterId = card.Id;
        RefreshSelectedCard();
    }

    private void CharactersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CharactersList.SelectedItem is CharacterCardModel card) ImportArt(card.Id);
    }

    private void ImportPortrait_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCharacterId is not null) ImportArt(_selectedCharacterId);
    }

    private void RemoveArt_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCharacterId is null) return;
        RemoveArt(_selectedCharacterId);
        SaveState();
        RefreshCharacters();
        RefreshBattle(force: true);
    }

    private void ImportArt(string characterId)
    {
        var name = CharacterCatalog.FirstOrDefault(c => c.Id == characterId)?.Name ?? "this card";
        var dialog = new OpenFileDialog { Title = $"Choose art for {name}", Filter = "Images and GIFs|*.gif;*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            Directory.CreateDirectory(PortraitFolder);
            // A fresh file name each time, so a GIF that is still playing never blocks the replacement.
            var destination = Path.Combine(PortraitFolder, $"{characterId}-{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(dialog.FileName).ToLowerInvariant()}");
            File.Copy(dialog.FileName, destination, true);
            if (LoadPortrait(destination) is null)
            {
                TryDelete(destination);
                MessageBox.Show("That file couldn't be opened as an image.", "Airi's", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            RemoveArt(characterId);
            _state.CharacterImages[characterId] = destination;
            SaveState();
            RefreshCharacters();
            RefreshBattle(force: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't import that picture.\n\n{ex.Message}", "Airi's", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveArt(string characterId)
    {
        if (!_state.CharacterImages.Remove(characterId, out var old)) return;
        _portraitCache.Remove(old);
        if (Path.GetFullPath(old).StartsWith(PortraitFolder, StringComparison.OrdinalIgnoreCase)) TryDelete(old);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* still in use; harmless leftover */ }
    }

    private ImageSource? Portrait(string characterId)
    {
        var path = _state.CharacterImages.GetValueOrDefault(characterId);
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!_portraitCache.TryGetValue(path, out var image)) _portraitCache[path] = image = LoadPortrait(path);
        return image;
    }

    private static BitmapImage? LoadPortrait(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            // Downscale big stills; GIFs are decoded frame-by-frame by the animator.
            if (!path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) bitmap.DecodePixelWidth = 420;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch { return null; }
    }

    // ───────────────────────── Gacha ─────────────────────────

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
        if (_state.Balance < cost) { GachaResultText.Text = $"You need {cost} ◆ for that pull."; return; }
        _state.Balance -= cost;
        var results = Enumerable.Range(0, count).Select(_ => DrawCharacter()).ToList();
        foreach (var c in results) _state.OwnedCharacters[c.Id] = _state.OwnedCharacters.GetValueOrDefault(c.Id) + 1;
        _state.LastPulls = results.Select(c => c.Id).ToList();
        var best = results.MaxBy(c => c.Rarity)!;
        GachaResultText.Text = count == 1
            ? $"{Palette.Rarity(best.Rarity).Label} · {best.Name}"
            : $"{results.Count(c => c.Rarity == 5)} Legendary · {results.Count(c => c.Rarity == 4)} Epic · {results.Count(c => c.Rarity == 3)} Rare";
        SaveState();
        RefreshCharacters();
        RefreshBattle(force: true);
        RefreshSummary();
    }

    // ───────────────────────── Rift battler ─────────────────────────

    private List<(CharacterModel Character, int Copies)> GetParty()
    {
        var pulled = CharacterCatalog.Where(c => _state.OwnedCharacters.GetValueOrDefault(c.Id) > 0)
            .Select(c => (Character: c, Copies: _state.OwnedCharacters[c.Id])).ToList();
        return pulled.Count > 0 ? pulled : [(StarterFighter, 1)];
    }

    private FighterStats GetPartyStats()
    {
        var total = new FighterStats(0, 0, 0, 0);
        foreach (var (character, copies) in GetParty())
        {
            var s = CharacterStats(character, copies);
            total = new(total.Hp + s.Hp, total.Attack + s.Attack, total.Crit + s.Crit, total.Armor + s.Armor);
        }
        foreach (var slot in Slots)
        {
            if (!_state.Battler.Equipment.TryGetValue(slot, out var id)) continue;
            var item = _state.Battler.Armor.FirstOrDefault(a => a.Id == id);
            if (item is null) continue;
            var rank = Math.Max(1, Array.IndexOf(Rarities, item.Rarity) + 1);
            total = slot == "Weapon" ? total with { Attack = total.Attack + rank * 6 + item.Wave / 40 * 2 } : total with { Armor = total.Armor + rank * 3 + item.Wave / 40 };
            foreach (var bonus in item.Bonuses)
                total = bonus.Key switch
                {
                    "Might" => total with { Attack = total.Attack + bonus.Value },
                    "Vitality" => total with { Hp = total.Hp + bonus.Value * 4 },
                    "Haste" or "Focus" => total with { Crit = total.Crit + bonus.Value },
                    "Aegis" => total with { Armor = total.Armor + bonus.Value },
                    _ => total
                };
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
                AddBattleLog($"Boss defeated: {item.Rarity} {item.Name}; +{BossReward} ◆.");
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

    private static double ArmorScore(ArmorModel item) => Math.Max(0, Array.IndexOf(Rarities, item.Rarity)) * 10000 + item.Wave * 10 + item.Bonuses.Values.Sum() * 100;

    private void RefreshBattle(bool force = false)
    {
        var battle = _state.Battler;
        var boss = !battle.PrepareBoss && battle.Wave % 20 == 0;

        // Only rebuild lists when something in them changed, so scroll position and selection survive the 1s timer.
        var key = string.Join("|", battle.Wave, battle.Armor.Count, battle.Armor.Sum(a => a.Count), string.Join(",", battle.Equipment.Values),
            battle.Log.FirstOrDefault(), battle.Alloy, _state.Weekly.Bosses);
        if (force || key != _battleListKey)
        {
            _battleListKey = key;
            PartyList.ItemsSource = GetParty().Select(p => BuildCard(p.Character, p.Copies)).OrderByDescending(c => c.Rarity).ThenByDescending(c => c.Copies).ToList();
            var stats = GetPartyStats();
            StatHpText.Text = stats.Hp.ToString("N0");
            StatAtkText.Text = stats.Attack.ToString("N0");
            StatCritText.Text = $"{stats.Crit:0}%";
            StatArmorText.Text = stats.Armor.ToString("N0");
            ArmorList.ItemsSource = battle.Armor.OrderByDescending(ArmorScore).Select(a => new ArmorRowModel(
                a.Name, $"{a.Rarity} · {a.Slot} · W{a.Wave}", string.Join("  ·  ", a.Bonuses.Select(b => $"{b.Key} +{b.Value}")),
                $"×{a.Count}", Palette.Armor(a.Rarity), battle.Equipment.GetValueOrDefault(a.Slot) == a.Id)).ToList();
            ArmorEmpty.Visibility = battle.Armor.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ArmorCountText.Text = battle.Armor.Sum(a => a.Count).ToString();
            AlloyRun.Text = battle.Alloy.ToString();
            BattleLogList.ItemsSource = battle.Log.ToList();
            RefreshWeekly();
        }

        WaveText.Text = battle.PrepareBoss ? $"FARM WAVE {battle.Wave} · BOSS {battle.BossFailedWave} FAILED" : boss ? $"BOSS WAVE {battle.Wave}" : $"WAVE {battle.Wave} · INFINITE DESCENT";
        PauseBattleButton.Content = battle.Paused ? "Resume" : "Pause";
        ReturnBossButton.Visibility = battle.PrepareBoss ? Visibility.Visible : Visibility.Collapsed;
        ReturnBossButton.Content = $"Return to wave {battle.BossFailedWave} boss";
        EnemyStatsText.Text = $"LV {battle.Wave}  ·  HP {(100 + (battle.Wave - 1) * 65) * (boss ? 12 : 1):N0}  ·  ATK {(12 + (battle.Wave - 1) * 7) * (boss ? 3 : 1):N0}";

        var secondsLeft = Math.Max(0, (battle.NextWaveAt - DateTime.UtcNow).TotalSeconds);
        var waveProgress = battle.Paused ? WaveProgressBar.Value : Math.Clamp(1 - secondsLeft / 12, 0, 1);
        TopWaveBar.Value = waveProgress;
        if (boss && battle.Boss is not null)
        {
            BossHealthBar.Visibility = Visibility.Visible; WaveProgressBar.Visibility = Visibility.Collapsed;
            BossHealthBar.Maximum = battle.Boss.MaxHealth; BossHealthBar.Value = Math.Clamp(battle.Boss.Health, 0, battle.Boss.MaxHealth);
            BattleStatusText.Text = battle.Paused ? "Boss fight paused"
                : $"Rift Warden · {Math.Max(0, battle.Boss.Health):N0} / {battle.Boss.MaxHealth:N0} HP · {Math.Max(0, (int)Math.Ceiling((battle.Boss.Deadline - DateTime.UtcNow).TotalSeconds))}s left";
        }
        else
        {
            BossHealthBar.Visibility = Visibility.Collapsed; WaveProgressBar.Visibility = Visibility.Visible;
            WaveProgressBar.Value = waveProgress;
            BattleStatusText.Text = battle.Paused ? "Expedition paused"
                : battle.PrepareBoss ? $"Farming for gear · next drop roll in {(int)Math.Ceiling(secondsLeft)}s"
                : $"Next wave in {(int)Math.Ceiling(secondsLeft)}s";
        }
        RenderArena();
    }

    private void RenderArena()
    {
        if (ArenaCanvas is null || ArenaCanvas.ActualWidth < 10) return;
        var wave = Math.Max(1, _state.Battler.Wave); var biome = Biomes[((wave - 1) / 5) % Biomes.Length]; var boss = !_state.Battler.PrepareBoss && wave % 20 == 0;
        BiomeText.Text = biome.Name.ToUpperInvariant();
        MonsterNameText.Text = boss ? "Rift Warden" : $"{MonsterAdjectives[(wave * 3 + wave / 5) % MonsterAdjectives.Length]} {MonsterForms[(wave * 7 + wave / 2) % MonsterForms.Length]}";
        MonsterGlyphText.Text = boss ? "♜" : wave % 3 == 0 ? "◈" : "◆";
        var sky = ColorFrom(biome.Sky); var ground = ColorFrom(biome.Ground); var glow = ColorFrom(biome.Glow);
        ArenaCard.Background = new LinearGradientBrush(sky, ground, 90); MonsterGlyphText.Foreground = new SolidColorBrush(glow);
        if (_lastArenaWave == wave) return;
        _lastArenaWave = wave; ArenaCanvas.Children.Clear(); var random = new Random(wave * 37); var width = ArenaCanvas.ActualWidth; var height = ArenaCanvas.ActualHeight;
        var moon = new Ellipse { Width = 80, Height = 80, Fill = new SolidColorBrush(glow), Opacity = .3 }; Canvas.SetLeft(moon, width * .62); Canvas.SetTop(moon, 25 + random.Next(25)); ArenaCanvas.Children.Add(moon);
        for (var layer = 0; layer < 3; layer++)
        {
            var points = new PointCollection { new(0, height) }; var step = width / 8;
            for (var i = 0; i <= 8; i++) points.Add(new Point(i * step, height * (.48 + random.NextDouble() * .2) + layer * 10));
            points.Add(new Point(width, height));
            ArenaCanvas.Children.Add(new Polygon { Points = points, Fill = new SolidColorBrush(layer == 0 ? Color.FromRgb(17, 20, 18) : ground), Opacity = .45 + layer * .16 });
        }
        var monster = new Polygon { Points = new PointCollection([new(0, 36), new(18, 8), new(35, 25), new(53, 0), new(70, 27), new(92, 14), new(100, 54), new(82, 68), new(93, 115), new(60, 98), new(43, 130), new(25, 92), new(3, 108), new(18, 68)]), Fill = new LinearGradientBrush(glow, Color.FromRgb(38, 49, 39), 90) };
        Canvas.SetLeft(monster, width * .78); Canvas.SetTop(monster, Math.Max(0, height - 150)); ArenaCanvas.Children.Add(monster);
    }

    private static Color ColorFrom(string value) => (Color)ColorConverter.ConvertFromString(value)!;
}
