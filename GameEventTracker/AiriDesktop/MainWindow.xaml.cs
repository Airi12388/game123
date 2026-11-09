using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
    // The roster lives in characters.json (embedded in the exe). Order matters: it sets each card's #number.
    private static readonly CharacterModel[] CharacterCatalog = LoadCatalog();

    private static CharacterModel[] LoadCatalog()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("characters.json")
            ?? throw new InvalidOperationException("characters.json is missing from the build.");
        return JsonSerializer.Deserialize<CharacterModel[]>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

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
        SeriesFilterInput.ItemsSource = new[] { AllSeries }.Concat(CharacterCatalog.Select(c => c.Source).Distinct().OrderBy(s => s)).ToList();
        SeriesFilterInput.SelectedIndex = 0;
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
        if (BoostersView.Visibility == Visibility.Visible && _ready) RefreshCharacters(); // pick up files dropped into the art folder
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

    private const string AllSeries = "All series";
    private string _seriesFilter = AllSeries;

    private void SeriesFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _seriesFilter = SeriesFilterInput.SelectedItem as string ?? AllSeries;
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
            .Where(c => (_rarityFilter == 0 || c.Rarity == _rarityFilter) && (!_ownedOnly || c.IsOwned) && (_seriesFilter == AllSeries || c.Source == _seriesFilter))
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
        SelectedCardFileText.Text = character is null ? "" : $"Folder file name: {character.Id}.png / .jpg / .gif";
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
        if (dialog.ShowDialog(this) == true) ImportArtFrom(characterId, dialog.FileName);
    }

    private void ImportArtFrom(string characterId, string sourcePath)
    {
        try
        {
            Directory.CreateDirectory(PortraitFolder);
            // A fresh file name each time, so a GIF that is still playing never blocks the replacement.
            var destination = Path.Combine(PortraitFolder, $"{characterId}-{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourcePath).ToLowerInvariant()}");
            File.Copy(sourcePath, destination, true);
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

    private static readonly string[] ImageExtensions = [".gif", ".png", ".jpg", ".jpeg", ".bmp"];

    private static string? DroppedImage(DragEventArgs e) =>
        e.Data.GetData(DataFormats.FileDrop) is string[] files
            ? files.FirstOrDefault(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            : null;

    private void CharactersList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DroppedImage(e) is not null && (e.OriginalSource as FrameworkElement)?.DataContext is CharacterCardModel ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void CharactersList_Drop(object sender, DragEventArgs e)
    {
        if (DroppedImage(e) is not { } file || (e.OriginalSource as FrameworkElement)?.DataContext is not CharacterCardModel card) return;
        _selectedCharacterId = card.Id;
        ImportArtFrom(card.Id, file);
    }

    private void OpenArtFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(LibraryFolder);
        System.Diagnostics.Process.Start("explorer.exe", LibraryFolder);
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

    // Card art library: the repo's GameEventTracker\character-art folder (git-ignored) when the app runs from inside the repo,
    // otherwise %LOCALAPPDATA%\AiriDesktop\portraits\library.
    private static readonly string LibraryFolder = FindRepoArtFolder() ?? Path.Combine(PortraitFolder, "library");

    private static string? FindRepoArtFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "character-art");
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }
    private static readonly string[] LibraryExtensions = [".gif", ".png", ".jpg", ".jpeg"];

    /// <summary>Your own imported art wins; otherwise fall back to the art library's {id}.* (files named after the character id, e.g. ellen.png).</summary>
    private ImageSource? Portrait(string characterId)
    {
        var path = _state.CharacterImages.GetValueOrDefault(characterId);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            path = LibraryExtensions.Select(ext => Path.Combine(LibraryFolder, characterId + ext)).FirstOrDefault(File.Exists);
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
            if (!path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) bitmap.DecodePixelWidth = 300;
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
        var pulled = new List<(CharacterModel Character, bool IsNew)>();
        foreach (var c in results)
        {
            var before = _state.OwnedCharacters.GetValueOrDefault(c.Id);
            _state.OwnedCharacters[c.Id] = before + 1;
            pulled.Add((c, before == 0));
        }
        _state.LastPulls = results.Select(c => c.Id).ToList();
        var best = results.MaxBy(c => c.Rarity)!;
        var summary = count == 1
            ? $"{Palette.Rarity(best.Rarity).Label} · {best.Name}"
            : $"{results.Count(c => c.Rarity == 5)} Legendary · {results.Count(c => c.Rarity == 4)} Epic · {results.Count(c => c.Rarity == 3)} Rare";
        GachaResultText.Text = summary;
        SaveState();
        RefreshCharacters();
        RefreshBattle(force: true);
        RefreshSummary();
        ShowPullReveal(pulled, summary);
    }

    // ───────────────────────── Pull reveal animation ─────────────────────────
    // Pack rises → shakes and glows in the best rarity's color → top tears off, pack bursts with a flash →
    // cards fly out face-down → flip one by one (lowest rarity first, best last). Click / Space skips; Esc / Enter closes.

    private const string ScaleXPath = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)";
    private const string ScaleYPath = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)";
    private const string AnglePath = "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)";
    private const string MoveXPath = "(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.X)";
    private const string MoveYPath = "(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)";
    private const string GlowPath = "(UIElement.Effect).(DropShadowEffect.Opacity)";
    private Storyboard? _pullStoryboard;
    private bool _pullAnimating;

    private static Color RarityColor(int stars) => stars switch { 5 => Color.FromRgb(0xF5, 0xC5, 0x18), 4 => Color.FromRgb(0xA1, 0x5C, 0xF6), _ => Color.FromRgb(0x4C, 0x8D, 0xFF) };

    private static void WithTransforms(UIElement element)
    {
        element.RenderTransformOrigin = new Point(.5, .5);
        element.RenderTransform = new TransformGroup { Children = { new ScaleTransform(), new RotateTransform(), new TranslateTransform() } };
    }

    /// <summary>Keyframe animation with absolute times (seconds). Each frame eases in from the previous one.</summary>
    private static DoubleAnimationUsingKeyFrames Frames(params (double T, double V, IEasingFunction? Ease)[] frames)
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        foreach (var (t, v, ease) in frames) anim.KeyFrames.Add(new EasingDoubleKeyFrame(v, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t)), ease));
        return anim;
    }

    private static ObjectAnimationUsingKeyFrames Switch(Visibility from, Visibility to, double at) => new()
    {
        KeyFrames = { new DiscreteObjectKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)), new DiscreteObjectKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(at))) }
    };

    private static void Animate(Storyboard sb, DependencyObject target, string path, Timeline anim)
    {
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(path));
        sb.Children.Add(anim);
    }

    private static FrameworkElement BuildCardBack() => new Border
    {
        CornerRadius = new CornerRadius(13),
        BorderThickness = new Thickness(3),
        BorderBrush = Palette.Hex("#4A4A55"),
        Background = new LinearGradientBrush(Color.FromRgb(0x2A, 0x2A, 0x33), Color.FromRgb(0x12, 0x12, 0x16), 90),
        Child = new Grid
        {
            Children =
            {
                new Border { Margin = new Thickness(10), CornerRadius = new CornerRadius(8), BorderBrush = Palette.Hex("#40F5C518"), BorderThickness = new Thickness(1) },
                new TextBlock { Text = "AIRI'S", FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Palette.Muted, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 24, 0, 0) },
                new Border
                {
                    Width = 76, Height = 76, CornerRadius = new CornerRadius(38), Background = Palette.Hex("#1A1405"), BorderBrush = Palette.Yellow, BorderThickness = new Thickness(3),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = "信", FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Palette.Yellow, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                }
            }
        }
    };

    private void ShowPullReveal(List<(CharacterModel Character, bool IsNew)> pulled, string summary)
    {
        _pullStoryboard?.Remove(PullOverlay);
        PullCardsPanel.Children.Clear();

        var best = pulled.Max(p => p.Character.Rarity);
        var glow = RarityColor(best);
        PackCountText.Text = pulled.Count == 1 ? "1 card" : $"{pulled.Count} cards";
        PullSummaryText.Text = summary;
        PullGlow.Fill = new RadialGradientBrush(Color.FromArgb(0x70, glow.R, glow.G, glow.B), Color.FromArgb(0, glow.R, glow.G, glow.B));
        PackRoot.Effect = new DropShadowEffect { Color = glow, BlurRadius = 70, ShadowDepth = 0, Opacity = 0 };
        WithTransforms(PackRoot);
        WithTransforms(PackTear);
        PullContinueButton.IsHitTestVisible = false;
        PullHintText.Text = "Click anywhere to skip";
        PullOverlay.Visibility = Visibility.Visible;

        var sb = new Storyboard();
        var backOut = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = .35 };
        var cubicOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        var cubicIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        // 1. Backdrop and pack entrance
        Animate(sb, PullOverlay, "Opacity", Frames((0, 0, null), (.25, 1, null)));
        Animate(sb, PackRoot, MoveYPath, Frames((0, 560, null), (.05, 560, null), (.65, 0, backOut)));
        foreach (var path in new[] { ScaleXPath, ScaleYPath })
            Animate(sb, PackRoot, path, Frames((0, .6, null), (.05, .6, null), (.65, 1, backOut), (1.6, 1.04, null), (1.95, 1.35, cubicOut)));

        // 2. Shake and glow in the best rarity's color
        Animate(sb, PackRoot, AnglePath, Frames((0, 0, null), (.7, 0, null), (.8, -5, null), (.9, 5, null), (1.0, -4, null), (1.1, 4, null), (1.2, -3, null), (1.3, 3, null), (1.42, 0, null)));
        Animate(sb, PackRoot, GlowPath, Frames((0, 0, null), (.7, 0, null), (1.3, 1, null)));
        Animate(sb, PullGlow, "Opacity", Frames((0, 0, null), (.7, 0, null), (1.5, 1, null)));

        // 3. Tear the top off, burst the pack, flash
        Animate(sb, PackTear, MoveYPath, Frames((0, 0, null), (1.45, 0, null), (1.9, -200, cubicOut)));
        Animate(sb, PackTear, MoveXPath, Frames((0, 0, null), (1.45, 0, null), (1.9, 70, cubicOut)));
        Animate(sb, PackTear, AnglePath, Frames((0, 0, null), (1.45, 0, null), (1.9, -28, cubicOut)));
        Animate(sb, PackTear, "Opacity", Frames((0, 1, null), (1.6, 1, null), (1.9, 0, null)));
        Animate(sb, PackRoot, "Opacity", Frames((0, 1, null), (1.6, 1, null), (1.95, 0, cubicIn)));
        Animate(sb, PullFlash, "Opacity", Frames((0, 0, null), (1.6, 0, null), (1.68, .8, null), (2.1, 0, cubicOut)));

        // 4. Cards: lowest rarity first so the best card is revealed last
        var ordered = pulled.Select((p, i) => (p.Character, p.IsNew, Index: i)).OrderBy(p => p.Character.Rarity).ThenBy(p => p.Index).ToList();
        var single = ordered.Count == 1;
        var scale = single ? 1.15 : .72;
        PullCardsPanel.MaxWidth = 5 * (180 * scale + 20) + 2;
        var lastReveal = 0.0;
        for (var i = 0; i < ordered.Count; i++)
        {
            var (character, isNew, _) = ordered[i];
            var card = BuildCard(character, _state.OwnedCharacters.GetValueOrDefault(character.Id));
            var face = new ContentPresenter { Content = card, ContentTemplate = (DataTemplate)FindResource("CardFace"), Visibility = Visibility.Hidden };
            var back = BuildCardBack();
            var flipper = new Grid { Children = { face, back } };
            WithTransforms(flipper);
            var badge = new Border
            {
                Background = Palette.Yellow, CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 3, 12, 3),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -13, 0, 0), Visibility = Visibility.Hidden,
                Child = new TextBlock { Text = "NEW", FontFamily = (FontFamily)FindResource("Display"), FontSize = 12, Foreground = Palette.Hex("#17140A") }
            };
            var holder = new Grid
            {
                Margin = new Thickness(10, 16, 10, 10), Opacity = 0,
                LayoutTransform = new ScaleTransform(scale, scale),
                Effect = new DropShadowEffect { Color = RarityColor(character.Rarity), BlurRadius = 45, ShadowDepth = 0, Opacity = 0 },
                Children = { flipper, badge }
            };
            WithTransforms(holder);
            PullCardsPanel.Children.Add(holder);

            var appear = 1.75 + i * .05;
            var reveal = single ? 2.35 : 2.45 + i * .22;
            var flip = reveal + .14;
            lastReveal = flip;
            Animate(sb, holder, "Opacity", Frames((0, 0, null), (appear, 0, null), (appear + .25, 1, null)));
            Animate(sb, holder, MoveYPath, Frames((0, 110, null), (appear, 110, null), (appear + .4, 0, backOut)));
            Animate(sb, flipper, ScaleXPath, Frames((0, 1, null), (reveal, 1, null), (flip, 0, cubicIn), (flip + .14, 1, cubicOut)));
            Animate(sb, back, "Visibility", Switch(Visibility.Visible, Visibility.Collapsed, flip));
            Animate(sb, face, "Visibility", Switch(Visibility.Hidden, Visibility.Visible, flip));
            if (isNew) Animate(sb, badge, "Visibility", Switch(Visibility.Hidden, Visibility.Visible, flip));
            Animate(sb, holder, GlowPath, Frames((0, 0, null), (flip, 0, null), (flip + .3, character.Rarity == 5 ? 1 : character.Rarity == 4 ? .8 : .4, null)));
            if (character.Rarity == 5)
                foreach (var path in new[] { ScaleXPath, ScaleYPath })
                    Animate(sb, holder, path, Frames((0, 1, null), (flip, 1, null), (flip + .18, 1.12, cubicOut), (flip + .5, 1, backOut)));
        }

        // 5. Summary and Continue
        var end = lastReveal + .45;
        Animate(sb, PullHeader, "Opacity", Frames((0, 0, null), (end - .2, 0, null), (end + .2, 1, null)));
        Animate(sb, PullContinueButton, "Opacity", Frames((0, 0, null), (end - .2, 0, null), (end + .2, 1, null)));

        sb.Completed += (_, _) => FinishPullReveal();
        _pullStoryboard = sb;
        _pullAnimating = true;
        sb.Begin(PullOverlay, true);
    }

    private void FinishPullReveal()
    {
        if (!_pullAnimating) return;
        _pullAnimating = false;
        PullContinueButton.IsHitTestVisible = true;
        PullHintText.Text = "Enter or Esc to close";
        PullContinueButton.Focus();
    }

    private void SkipPullReveal()
    {
        if (!_pullAnimating || _pullStoryboard is null) return;
        _pullStoryboard.SkipToFill(PullOverlay);
        FinishPullReveal();
    }

    private void ClosePullReveal()
    {
        _pullStoryboard?.Remove(PullOverlay);
        _pullStoryboard = null;
        _pullAnimating = false;
        PullOverlay.Visibility = Visibility.Collapsed;
        PullCardsPanel.Children.Clear();
    }

    private void PullOverlay_MouseDown(object sender, MouseButtonEventArgs e) => SkipPullReveal();
    private void PullContinue_Click(object sender, RoutedEventArgs e) => ClosePullReveal();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (PullOverlay.Visibility != Visibility.Visible) return;
        if (_pullAnimating && e.Key is Key.Space or Key.Enter or Key.Escape) SkipPullReveal();
        else if (!_pullAnimating && e.Key is Key.Enter or Key.Escape or Key.Space) ClosePullReveal();
        e.Handled = true;
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
