const STORAGE_KEY = "playlog-state-v1";
const gameColors = ["#73954b", "#e17d5c", "#6e8ca0", "#b8904d", "#8771a0", "#4e9a8b"];
const EVENT_CLEAR_REWARD = 100;
const SINGLE_PULL_COST = 100;
const TEN_PULL_COST = 900;
const BOSS_PULSE_REWARD = 250;
const weeklyGoals = [
	{ id: "daily-clears", title: "Complete 10 daily quests", field: "dailyClears", target: 10, reward: 150 },
	{ id: "event-clears", title: "Complete 3 game events", field: "eventClears", target: 3, reward: 200 },
	{ id: "boss-clears", title: "Defeat 1 Rift boss", field: "bosses", target: 1, reward: 250 }
];
const armorDrops = [
	{ id: "scrapguard", name: "Scrapguard Vest", rarity: "Common", slot: "Body" },
	{ id: "riftscout", name: "Riftscout Greaves", rarity: "Common", slot: "Legs" },
	{ id: "lumenplate", name: "Lumenplate Cuirass", rarity: "Uncommon", slot: "Body" },
	{ id: "voidhelm", name: "Voidglass Helm", rarity: "Rare", slot: "Helm" },
	{ id: "warden-mail", name: "Warden's Aegis", rarity: "Epic", slot: "Body" },
	{ id: "starweave", name: "Starweave Mantle", rarity: "Epic", slot: "Back" }
];
const armorSlots = ["Chest", "Legs", "Helm", "Boots", "Hands", "Weapon", "Cloak", "Rings1", "Ring2", "Trinket1", "Trinket2"];
const rarityNames = ["Common", "Uncommon", "Rare", "Epic", "Unique"];
const armorPrefixes = { Common: "Riftworn", Uncommon: "Tempered", Rare: "Runed", Epic: "Warden's", Unique: "Starfall" };
const uniqueBonusStats = ["Might", "Vitality", "Haste", "Aegis", "Fortune", "Focus"];
const characterBaseStats = {
	3: { hp: 120, attack: 20, crit: 5, defense: 8 },
	4: { hp: 165, attack: 32, crit: 9, defense: 14 },
	5: { hp: 220, attack: 45, crit: 14, defense: 20 }
};
const characterCombatProfiles = {
	"rift-runner": { attack: 8, defense: 4 },
	naruto: { attack: 9, crit: 2 },
	"sailor-moon": { hp: 30, defense: 4 },
	kafka: { attack: 4, crit: 7 },
	miyabi: { attack: 10, crit: 4 },
	ellen: { attack: 7, crit: 6 },
	raiden: { attack: 6, crit: 5 },
	"silver-wolf": { crit: 8, defense: 3 },
	furina: { hp: 25, crit: 3 },
	arabella: { attack: 6, defense: 5 }
};
const battleBiomes = [
	{ name: "Mosslight Ruins", sky: "#20372f", ground: "#263b30", glow: "#a5cf74" },
	{ name: "Cinderfall Basin", sky: "#442b28", ground: "#382824", glow: "#ed9368" },
	{ name: "Glassfrost Shelf", sky: "#263b48", ground: "#263640", glow: "#94d9e3" },
	{ name: "Sable Observatory", sky: "#302744", ground: "#2c2639", glow: "#c2a0e3" }
];
const monsterAdjectives = ["Shardbound", "Cinder-veiled", "Gloam-marked", "Frost-bitten", "Hollowglass"];
const monsterForms = ["Riftling", "Gloomstalker", "Obelisk brute", "Veil sentinel", "Shard fiend"];
const characters = [
	{ id: "naruto", name: "Naruto Uzumaki", source: "Naruto", rarity: 5, color: "#d98545" },
	{ id: "sailor-moon", name: "Sailor Moon", source: "Sailor Moon", rarity: 5, color: "#d881a8" },
	{ id: "tanjiro", name: "Tanjiro Kamado", source: "Demon Slayer", rarity: 4, color: "#538c74" },
	{ id: "anya", name: "Anya Forger", source: "Spy x Family", rarity: 3, color: "#c56d78" },
	{ id: "anby", name: "Anby Demara", source: "Zenless Zone Zero", rarity: 4, color: "#7594af" },
	{ id: "nicole", name: "Nicole Demara", source: "Zenless Zone Zero", rarity: 4, color: "#d17a98" },
	{ id: "ellen", name: "Ellen Joe", source: "Zenless Zone Zero", rarity: 5, color: "#5c8ca2" },
	{ id: "miyabi", name: "Hoshimi Miyabi", source: "Zenless Zone Zero", rarity: 5, color: "#b77986" },
	{ id: "amber", name: "Amber", source: "Genshin Impact", rarity: 3, color: "#db8657" },
	{ id: "xiangling", name: "Xiangling", source: "Genshin Impact", rarity: 4, color: "#ce6850" },
	{ id: "furina", name: "Furina", source: "Genshin Impact", rarity: 5, color: "#6194b5" },
	{ id: "march-7", name: "March 7th", source: "Honkai: Star Rail", rarity: 3, color: "#da8b9b" },
	{ id: "dan-heng", name: "Dan Heng", source: "Honkai: Star Rail", rarity: 4, color: "#578c89" },
	{ id: "kafka", name: "Kafka", source: "Honkai: Star Rail", rarity: 5, color: "#9e6d91" },
	{ id: "adelheid", name: "Adelheid", source: "Chaos Zero Nightmare", rarity: 4, color: "#937eaf" },
	{ id: "amir", name: "Amir", source: "Chaos Zero Nightmare", rarity: 4, color: "#6687a1" },
	{ id: "cassius", name: "Cassius", source: "Chaos Zero Nightmare", rarity: 3, color: "#b58a54" },
	{ id: "arabella", name: "Arabella", source: "Chaos Zero Nightmare", rarity: 5, color: "#b36971" },
	{ id: "levi", name: "Levi Ackerman", source: "Attack on Titan", rarity: 5, color: "#657b8c" },
	{ id: "luffy", name: "Monkey D. Luffy", source: "One Piece", rarity: 5, color: "#cf6548" },
	{ id: "nezuko", name: "Nezuko Kamado", source: "Demon Slayer", rarity: 4, color: "#c97b94" },
	{ id: "killua", name: "Killua Zoldyck", source: "Hunter x Hunter", rarity: 4, color: "#62a9bd" },
	{ id: "koleda", name: "Koleda Belobog", source: "Zenless Zone Zero", rarity: 4, color: "#d5805a" },
	{ id: "zhu-yuan", name: "Zhu Yuan", source: "Zenless Zone Zero", rarity: 5, color: "#577aab" },
	{ id: "raiden", name: "Raiden Shogun", source: "Genshin Impact", rarity: 5, color: "#8164aa" },
	{ id: "kazuha", name: "Kaedehara Kazuha", source: "Genshin Impact", rarity: 5, color: "#bc7c62" },
	{ id: "himeko", name: "Himeko", source: "Honkai: Star Rail", rarity: 5, color: "#b36a53" },
	{ id: "silver-wolf", name: "Silver Wolf", source: "Honkai: Star Rail", rarity: 5, color: "#588ec5" },
	{ id: "beryl", name: "Beryl", source: "Chaos Zero Nightmare", rarity: 4, color: "#64a078" },
	{ id: "anika", name: "Anika", source: "Chaos Zero Nightmare", rarity: 3, color: "#ba8056" },
	{ id: "lisa", name: "Lisa", source: "Genshin Impact", rarity: 3, color: "#896da9" },
	{ id: "kaeya", name: "Kaeya", source: "Genshin Impact", rarity: 4, color: "#5879a7" },
	{ id: "navia", name: "Navia", source: "Genshin Impact", rarity: 5, color: "#c5a15e" },
	{ id: "acheron", name: "Acheron", source: "Honkai: Star Rail", rarity: 5, color: "#7d668b" },
	{ id: "firefly", name: "Firefly", source: "Honkai: Star Rail", rarity: 5, color: "#6aa880" },
	{ id: "pela", name: "Pela", source: "Honkai: Star Rail", rarity: 4, color: "#7594af" },
	{ id: "jane-doe", name: "Jane Doe", source: "Zenless Zone Zero", rarity: 5, color: "#9d7165" },
	{ id: "qingyi", name: "Qingyi", source: "Zenless Zone Zero", rarity: 5, color: "#69a397" },
	{ id: "burnice", name: "Burnice White", source: "Zenless Zone Zero", rarity: 5, color: "#dd774e" },
	{ id: "frieren", name: "Frieren", source: "Frieren: Beyond Journey's End", rarity: 5, color: "#79a8a2" },
	{ id: "gojo", name: "Satoru Gojo", source: "Jujutsu Kaisen", rarity: 5, color: "#7299d1" },
	{ id: "goku", name: "Son Goku", source: "Dragon Ball", rarity: 5, color: "#df854b" },
	{ id: "rukia", name: "Rukia Kuchiki", source: "Bleach", rarity: 4, color: "#8293bd" },
	{ id: "rengoku", name: "Kyojuro Rengoku", source: "Demon Slayer", rarity: 5, color: "#d57848" },
	{ id: "saitama", name: "Saitama", source: "One-Punch Man", rarity: 5, color: "#cc5c4c" }
];
const starterFighter = { id: "rift-runner", name: "Rift Runner", source: "Playlog starter", rarity: 3, color: "#87b878" };
let today = dateKey(new Date());

function makeId() {
	return globalThis.crypto?.randomUUID?.() || `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function dateKey(date) {
	const year = date.getFullYear();
	const month = String(date.getMonth() + 1).padStart(2, "0");
	const day = String(date.getDate()).padStart(2, "0");
	return `${year}-${month}-${day}`;
}

function shiftedDate(days) {
	const date = new Date();
	date.setDate(date.getDate() + days);
	return dateKey(date);
}

function getWeekKey(date = new Date()) {
	const monday = new Date(date.getFullYear(), date.getMonth(), date.getDate());
	monday.setDate(monday.getDate() - ((monday.getDay() + 6) % 7));
	return dateKey(monday);
}

function createWeeklyState() {
	return { weekOf: getWeekKey(), progress: { dailyClears: 0, eventClears: 0, bosses: 0 }, claimed: [] };
}

function createBattlerState() {
	return { wave: 1, nextWaveAt: Date.now() + getWaveDuration(1) * 1000, paused: false, pausedAt: 0, armor: [], alloy: 0, log: ["Expedition underway. The first wave is approaching."], characterId: starterFighter.id, equipment: {}, bossFight: null, bossFailedWave: null, prepareBoss: false };
}

function getWaveDuration(wave) {
	return wave % 20 === 0 ? 30 : 12;
}

function createStarterState() {
	const games = [
		{ id: makeId(), name: "Genshin Impact", color: gameColors[0] },
		{ id: makeId(), name: "Final Fantasy XIV", color: gameColors[1] }
	];
	const quests = [
		{ id: makeId(), gameId: games[0].id, title: "Spend some Original Resin", reward: 25, completedOn: null, earnedOn: null },
		{ id: makeId(), gameId: games[0].id, title: "Complete a commission", reward: 20, completedOn: null, earnedOn: null },
		{ id: makeId(), gameId: games[1].id, title: "Run one duty", reward: 25, completedOn: null, earnedOn: null },
		{ id: makeId(), gameId: games[1].id, title: "Check your retainers", reward: 15, completedOn: null, earnedOn: null }
	];
	const events = [
		{ id: makeId(), gameId: games[0].id, title: "Weekend co-op session", startDate: shiftedDate(2), endDate: shiftedDate(4), done: false, sample: true },
		{ id: makeId(), gameId: games[1].id, title: "Community game night", startDate: shiftedDate(5), endDate: shiftedDate(5), done: false, sample: true }
	];
	return { games, quests, events, balance: 0, ownedCharacters: {}, characterImages: {}, pityFive: 0, pityFour: 0, totalPulls: 0, lastPulls: [], weekly: createWeeklyState(), battler: createBattlerState() };
}

function normalizeWeekly(saved) {
	if (!saved || saved.weekOf !== getWeekKey()) return createWeeklyState();
	return {
		weekOf: saved.weekOf,
		progress: {
			dailyClears: Number(saved.progress?.dailyClears) || 0,
			eventClears: Number(saved.progress?.eventClears) || 0,
			bosses: Number(saved.progress?.bosses) || 0
		},
		claimed: Array.isArray(saved.claimed) ? saved.claimed : []
	};
}

function normalizeBattler(saved) {
	if (!saved || !Number.isFinite(Number(saved.wave))) return createBattlerState();
	return {
		wave: Math.max(1, Math.floor(Number(saved.wave))),
		nextWaveAt: Number(saved.nextWaveAt) || Date.now() + getWaveDuration(Number(saved.wave)) * 1000,
		paused: Boolean(saved.paused),
		pausedAt: Number(saved.pausedAt) || 0,
		armor: Array.isArray(saved.armor) ? saved.armor : [],
		alloy: Math.max(0, Number(saved.alloy) || 0),
		log: Array.isArray(saved.log) ? saved.log.slice(0, 5) : [],
		characterId: typeof saved.characterId === "string" ? saved.characterId : starterFighter.id,
		equipment: saved.equipment && typeof saved.equipment === "object" ? saved.equipment : {},
		bossFight: saved.bossFight && Number(saved.bossFight.wave) > 0 ? {
			wave: Number(saved.bossFight.wave),
			health: Math.max(0, Number(saved.bossFight.health) || 0),
			maxHealth: Math.max(1, Number(saved.bossFight.maxHealth) || 1),
			deadline: Number(saved.bossFight.deadline) || Date.now(),
			lastAttackAt: Number(saved.bossFight.lastAttackAt) || Date.now()
		} : null,
		bossFailedWave: Number(saved.bossFailedWave) || null,
		prepareBoss: Boolean(saved.prepareBoss)
	};
}

function loadState() {
	try {
		const saved = JSON.parse(localStorage.getItem(STORAGE_KEY));
		if (saved && Array.isArray(saved.games) && Array.isArray(saved.quests) && Array.isArray(saved.events)) {
			return {
				games: saved.games,
				quests: saved.quests,
				events: saved.events,
				balance: Number(saved.balance) || 0,
				ownedCharacters: saved.ownedCharacters && typeof saved.ownedCharacters === "object" ? saved.ownedCharacters : {},
				characterImages: saved.characterImages && typeof saved.characterImages === "object" ? saved.characterImages : {},
				pityFive: Number(saved.pityFive) || 0,
				pityFour: Number(saved.pityFour) || 0,
				totalPulls: Number(saved.totalPulls) || 0,
				lastPulls: Array.isArray(saved.lastPulls) ? saved.lastPulls : [],
				weekly: normalizeWeekly(saved.weekly),
				battler: normalizeBattler(saved.battler)
			};
		}
	} catch (error) {
		console.warn("Playlog could not read saved data.", error);
	}
	return createStarterState();
}

let state = loadState();
let selectedGameId = "all";
let eventFilter = "all";
let toastTimer;

const elements = {
	wallet: document.querySelector("#wallet-balance"),
	summaryUpcoming: document.querySelector("#summary-upcoming"),
	summaryQuests: document.querySelector("#summary-quests"),
	today: document.querySelector("#today-label"),
	gameFilters: document.querySelector("#game-filters"),
	events: document.querySelector("#events-list"),
	eventCount: document.querySelector("#event-count"),
	questGame: document.querySelector("#quest-game"),
	quests: document.querySelector("#quests-list"),
	weekly: document.querySelector("#weekly-list"),
	weeklyCount: document.querySelector("#weekly-progress-count"),
	eventDialog: document.querySelector("#event-dialog"),
	gameDialog: document.querySelector("#game-dialog"),
	questDialog: document.querySelector("#quest-dialog"),
	eventForm: document.querySelector("#event-form"),
	gameForm: document.querySelector("#game-form"),
	questForm: document.querySelector("#quest-form"),
	gachaResult: document.querySelector("#gacha-result"),
	collection: document.querySelector("#collection-list"),
	collectionCount: document.querySelector("#collection-count"),
	portraitCharacter: document.querySelector("#portrait-character"),
	portraitFile: document.querySelector("#portrait-file"),
	clearPortrait: document.querySelector("#clear-portrait"),
	pityFive: document.querySelector("#pity-five"),
	pityFour: document.querySelector("#pity-four"),
	pullOne: document.querySelector("#pull-one"),
	pullTen: document.querySelector("#pull-ten"),
	tabPulse: document.querySelector("#tab-pulse"),
	tabWave: document.querySelector("#tab-wave"),
	battleToggle: document.querySelector("#battle-toggle"),
	battlePhase: document.querySelector("#battle-phase"),
	battleOpponent: document.querySelector("#battle-opponent"),
	battleEnemyStats: document.querySelector("#battle-enemy-stats"),
	bossHealthWrap: document.querySelector("#boss-health-wrap"),
	bossHealthLabel: document.querySelector("#boss-health-label"),
	bossHealthBar: document.querySelector("#boss-health-bar"),
	bossFailureControls: document.querySelector("#boss-failure-controls"),
	bossFailureNote: document.querySelector("#boss-failure-note"),
	returnToBoss: document.querySelector("#return-to-boss"),
	bossHealthWrap: document.querySelector("#boss-health-wrap"),
	bossHealthLabel: document.querySelector("#boss-health-label"),
	bossHealthBar: document.querySelector("#boss-health-bar"),
	bossFailureControls: document.querySelector("#boss-failure-controls"),
	bossFailureNote: document.querySelector("#boss-failure-note"),
	returnToBoss: document.querySelector("#return-to-boss"),
	battlePartyName: document.querySelector("#battle-party-name"),
	battlePartyCount: document.querySelector("#battle-party-count"),
	battlePartyList: document.querySelector("#battle-party-list"),
	battleCharacterStats: document.querySelector("#battle-character-stats"),
	battleEquipment: document.querySelector("#battle-equipment"),
	arenaScene: document.querySelector("#arena-scene"),
	arenaBiome: document.querySelector("#arena-biome"),
	generatedMonster: document.querySelector("#generated-monster"),
	generatedMonsterName: document.querySelector("#generated-monster-name"),
	battleCountdown: document.querySelector("#battle-countdown"),
	battleProgress: document.querySelector("#battle-progress-bar"),
	battleLog: document.querySelector("#battle-log"),
	battleCrest: document.querySelector("#battle-crest-icon"),
	armor: document.querySelector("#armor-list"),
	armorCount: document.querySelector("#armor-count"),
	alloyCount: document.querySelector("#material-count"),
	toast: document.querySelector("#toast")
};

function persist() {
	localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

function escapeHtml(value) {
	return String(value).replace(/[&<>"']/g, (character) => ({
		"&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
	})[character]);
}

function gameById(id) {
	return state.games.find((game) => game.id === id);
}

function formatDate(value) {
	const [year, month, day] = value.split("-").map(Number);
	return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(new Date(year, month - 1, day));
}

function formatRange(start, end) {
	return start === end ? formatDate(start) : `${formatDate(start)} - ${formatDate(end)}`;
}

function getEventStatus(event) {
	if (event.done) return { text: "Completed", className: "completed" };
	if (event.endDate < today) return { text: "Ended", className: "ended" };
	if (event.startDate <= today) return { text: "Live now", className: "live" };
	return { text: "Upcoming", className: "" };
}

function getEventCountdown(event, now = new Date()) {
	const todayKey = dateKey(now);
	if (event.done) return "Completed";
	if (event.endDate < todayKey) return "Ended";
	const startsLater = event.startDate > todayKey;
	const date = startsLater ? event.startDate : event.endDate;
	const [year, month, day] = date.split("-").map(Number);
	const target = new Date(year, month - 1, day + (startsLater ? 0 : 1));
	const minutes = Math.max(0, Math.floor((target.getTime() - now.getTime()) / 60000));
	const days = Math.floor(minutes / 1440);
	const hours = Math.floor((minutes % 1440) / 60);
	const restMinutes = minutes % 60;
	const time = days ? `${days}d ${hours}h` : hours ? `${hours}h ${restMinutes}m` : `${Math.max(1, restMinutes)}m`;
	return `${startsLater ? "Starts in" : "Ends in"} ${time}`;
}

function render() {
	persist();
	renderSummary();
	renderGameFilters();
	renderEvents();
	renderQuestGameOptions();
	renderQuests();
	renderWeekly();
	renderGacha();
	renderBattler();
}

function incrementWeekly(field, amount = 1) {
	state.weekly.progress[field] = (Number(state.weekly.progress[field]) || 0) + amount;
}

function renderSummary() {
	const upcoming = state.events.filter((event) => !event.done && event.endDate >= today).length;
	const validQuests = state.quests.filter((quest) => state.games.some((game) => game.id === quest.gameId));
	const completed = validQuests.filter((quest) => quest.completedOn === today).length;
	elements.wallet.textContent = state.balance.toLocaleString();
	elements.summaryUpcoming.textContent = String(upcoming).padStart(2, "0");
	elements.summaryQuests.textContent = `${completed}/${validQuests.length}`;
	elements.today.textContent = new Intl.DateTimeFormat(undefined, { weekday: "short", month: "short", day: "numeric" }).format(new Date());
}

function renderGameFilters() {
	const allGame = `<button type="button" class="game-chip ${selectedGameId === "all" ? "is-active" : ""}" data-game-select="all"><span class="game-dot"></span> All games</button>`;
	const games = state.games.map((game) => `<div class="game-filter">
		<button type="button" class="game-chip ${selectedGameId === game.id ? "is-active" : ""}" data-game-select="${game.id}" style="--game-color:${game.color}"><span class="game-dot"></span>${escapeHtml(game.name)}</button>
		<button type="button" class="game-remove" data-game-remove="${game.id}" aria-label="Remove game ${escapeHtml(game.name)}" title="Remove game">×</button>
	</div>`).join("");
	elements.gameFilters.innerHTML = `${allGame}${games}`;
	elements.gameFilters.querySelectorAll("[data-game-select]").forEach((button) => {
		button.addEventListener("click", () => {
			selectedGameId = button.dataset.gameSelect;
			renderGameFilters();
			renderEvents();
		});
	});
	elements.gameFilters.querySelectorAll("[data-game-remove]").forEach((button) => {
		button.addEventListener("click", () => {
			const game = gameById(button.dataset.gameRemove);
			if (!game || !window.confirm(`Remove ${game.name} and its events and daily quests?`)) return;
			state.games = state.games.filter((item) => item.id !== game.id);
			state.events = state.events.filter((event) => event.gameId !== game.id);
			state.quests = state.quests.filter((quest) => quest.gameId !== game.id);
			if (selectedGameId === game.id) selectedGameId = "all";
			render();
			showToast(`${game.name} removed`);
		});
	});
}

function renderEvents() {
	let visible = state.events.filter((event) => selectedGameId === "all" || event.gameId === selectedGameId);
	if (eventFilter === "upcoming") visible = visible.filter((event) => !event.done && event.endDate >= today);
	if (eventFilter === "done") visible = visible.filter((event) => event.done);
	visible.sort((a, b) => a.startDate.localeCompare(b.startDate));
	elements.eventCount.textContent = String(visible.length);
	if (!visible.length) {
		elements.events.innerHTML = '<div class="empty-state"><strong>No events in this view</strong><p>Add an event to keep its dates close at hand.</p></div>';
		return;
	}
	elements.events.innerHTML = visible.map((event, index) => {
		const game = gameById(event.gameId);
		const status = getEventStatus(event);
		return `<article class="event-row" style="--game-color:${game?.color || "#73816b"};animation-delay:${Math.min(index, 6) * 35}ms">
			<span class="event-accent" aria-hidden="true"></span>
			<div class="event-main"><div class="event-title-line"><span class="event-title">${escapeHtml(event.title)}</span>${event.sample ? '<span class="demo-tag">EXAMPLE</span>' : ""}</div>
				<div class="event-meta"><span class="event-game">${escapeHtml(game?.name || "Unknown game")}</span><span class="event-date">${formatRange(event.startDate, event.endDate)}</span><span class="event-status ${status.className}">${status.text}</span><span class="event-countdown" data-event-countdown="${event.id}">${getEventCountdown(event)}</span></div>
			</div>
			<div class="event-side"><label class="done-control" title="Completing an event earns ${EVENT_CLEAR_REWARD} Pulse"><input type="checkbox" data-event-done="${event.id}" ${event.done ? "checked" : ""}><span>Done</span></label><button type="button" class="event-remove" data-event-remove="${event.id}" aria-label="Remove ${escapeHtml(event.title)}">Remove</button></div>
		</article>`;
	}).join("");
	elements.events.querySelectorAll("[data-event-done]").forEach((checkbox) => checkbox.addEventListener("change", () => {
		const event = state.events.find((item) => item.id === checkbox.dataset.eventDone);
		if (event) {
			event.done = checkbox.checked;
			if (checkbox.checked && !event.rewardClaimed) {
				event.rewardClaimed = true;
				state.balance += EVENT_CLEAR_REWARD;
				incrementWeekly("eventClears");
				showToast(`+${EVENT_CLEAR_REWARD} Pulse for the event clear`);
			}
		}
		render();
	}));
	elements.events.querySelectorAll("[data-event-remove]").forEach((button) => button.addEventListener("click", () => {
		state.events = state.events.filter((event) => event.id !== button.dataset.eventRemove);
		render();
		showToast("Event removed");
	}));
}

function renderQuestGameOptions() {
	const currentValue = elements.questGame.value;
	if (!state.games.length) {
		elements.questGame.innerHTML = '<option value="">Add a game to get started</option>';
		elements.questGame.disabled = true;
		return;
	}
	elements.questGame.disabled = false;
	elements.questGame.innerHTML = state.games.map((game) => `<option value="${game.id}">${escapeHtml(game.name)}</option>`).join("");
	elements.questGame.value = state.games.some((game) => game.id === currentValue) ? currentValue : state.games[0].id;
}

function renderQuests() {
	const activeGameId = elements.questGame.value;
	const quests = state.quests.filter((quest) => quest.gameId === activeGameId);
	if (!quests.length) {
		elements.quests.innerHTML = '<div class="empty-state"><strong>No daily quests yet</strong><p>Add one to start earning Pulse.</p></div>';
		return;
	}
	elements.quests.innerHTML = quests.map((quest, index) => {
		const isDone = quest.completedOn === today;
		return `<div class="quest-item ${isDone ? "is-done" : ""}" style="animation-delay:${index * 35}ms">
			<label class="quest-check"><input type="checkbox" data-quest-done="${quest.id}" ${isDone ? "checked disabled" : ""} aria-label="Complete ${escapeHtml(quest.title)}"></label>
			<span class="quest-name">${escapeHtml(quest.title)}<span class="quest-game-name">${escapeHtml(gameById(quest.gameId)?.name || "")}</span></span>
			<span class="quest-reward">${isDone ? "CLAIMED" : `+${quest.reward} P`}</span>
			<button type="button" class="quest-remove" data-quest-remove="${quest.id}" aria-label="Remove daily quest ${escapeHtml(quest.title)}" title="Remove daily quest">×</button>
		</div>`;
	}).join("");
	elements.quests.querySelectorAll("[data-quest-done]").forEach((checkbox) => checkbox.addEventListener("change", () => {
		const quest = state.quests.find((item) => item.id === checkbox.dataset.questDone);
		if (!quest || quest.earnedOn === today) return;
		quest.completedOn = today;
		quest.earnedOn = today;
		state.balance += Number(quest.reward);
		incrementWeekly("dailyClears");
		render();
		showToast(`+${quest.reward} Pulse earned`);
	}));
	elements.quests.querySelectorAll("[data-quest-remove]").forEach((button) => button.addEventListener("click", () => {
		state.quests = state.quests.filter((quest) => quest.id !== button.dataset.questRemove);
		render();
		showToast("Daily quest removed");
	}));
}

function renderWeekly() {
	const completeCount = weeklyGoals.filter((goal) => state.weekly.claimed.includes(goal.id)).length;
	elements.weeklyCount.textContent = `${completeCount}/${weeklyGoals.length}`;
	elements.weekly.innerHTML = weeklyGoals.map((goal) => {
		const progress = Math.min(goal.target, Number(state.weekly.progress[goal.field]) || 0);
		const claimed = state.weekly.claimed.includes(goal.id);
		const ready = progress >= goal.target;
		return `<div class="weekly-item ${claimed ? "is-claimed" : ""}">
			<div class="weekly-item-copy"><strong>${escapeHtml(goal.title)}</strong><span>${progress}/${goal.target} complete · +${goal.reward} P</span></div>
			<button class="weekly-claim" type="button" data-weekly-claim="${goal.id}" ${!ready || claimed ? "disabled" : ""}>${claimed ? "Claimed" : ready ? "Claim" : "In progress"}</button>
		</div>`;
	}).join("");
	elements.weekly.querySelectorAll("[data-weekly-claim]").forEach((button) => button.addEventListener("click", () => {
		const goal = weeklyGoals.find((item) => item.id === button.dataset.weeklyClaim);
		if (!goal || state.weekly.claimed.includes(goal.id) || state.weekly.progress[goal.field] < goal.target) return;
		state.weekly.claimed.push(goal.id);
		state.balance += goal.reward;
		render();
		showToast(`+${goal.reward} Pulse weekly reward`);
	}));
}

function rollArmorRarity(wave, boss) {
	const tier = Math.floor((wave - 1) / 40);
	const weights = [
		{ rarity: "Common", weight: Math.max(10, 65 - tier * 7) },
		{ rarity: "Uncommon", weight: 25 },
		{ rarity: "Rare", weight: 8 + tier * 4 },
		{ rarity: "Epic", weight: 2 + tier * 3 }
	];
	if (boss) weights.push({ rarity: "Unique", weight: 4 + Math.min(8, Math.floor(wave / 100)) });
	const totalWeight = weights.reduce((sum, item) => sum + item.weight, 0);
	let roll = Math.random() * totalWeight;
	for (const item of weights) {
		roll -= item.weight;
		if (roll < 0) return item.rarity;
	}
	return boss ? "Unique" : "Epic";
}

function rollUniqueBonuses(wave) {
	const shuffled = [...uniqueBonusStats].sort(() => Math.random() - 0.5);
	const bonusCount = 3 + (wave >= 100 ? 1 : 0);
	const levelBonus = Math.floor(wave / 10);
	return shuffled.slice(0, bonusCount).map((stat) => ({
		stat,
		value: 2 + levelBonus + Math.floor(Math.random() * (4 + levelBonus))
	}));
}

function tryAutoEquipArmor(armor) {
	const slot = normalizedEquipmentSlot(armor.slot);
	if (!armorSlots.includes(slot)) return false;
	const equipped = state.battler.armor.find((item) => item.id === state.battler.equipment[slot]);
	if (equipped && getArmorScore(armor) <= getArmorScore(equipped)) return false;
	state.battler.equipment[slot] = armor.id;
	armor.autoEquipped = true;
	return true;
}

function addArmorDrop(wave, boss) {
	const rarity = rollArmorRarity(wave, boss);
	const slot = armorSlots[Math.floor(Math.random() * armorSlots.length)];
	const name = `${armorPrefixes[rarity]} ${slot}`;
	if (rarity !== "Unique") {
		const existing = state.battler.armor.find((item) => item.rarity === rarity && item.slot === slot && item.name === name);
		if (existing) {
			existing.count = (Number(existing.count) || 1) + 1;
			existing.wave = wave;
			tryAutoEquipArmor(existing);
			return existing;
		}
	}
	const armor = {
		id: makeId(),
		name,
		rarity,
		slot,
		wave,
		count: 1,
		bonuses: rarity === "Unique" ? rollUniqueBonuses(wave) : []
	};
	state.battler.armor.push(armor);
	tryAutoEquipArmor(armor);
	return armor;
}

function getArmorScore(armor) {
	const rarity = Math.max(0, rarityNames.indexOf(armor.rarity));
	const bonuses = (armor.bonuses || []).reduce((sum, bonus) => sum + (Number(bonus.value) || 0), 0);
	return rarity * 10000 + (Number(armor.wave) || 1) * 10 + bonuses * 100;
}

function getEnemyStats(wave, boss) {
	const baseHealth = 100 + (wave - 1) * 65;
	const baseAttack = 12 + (wave - 1) * 7;
	const multiplier = boss ? 12 : 1;
	return {
		level: wave,
		health: Math.floor(baseHealth * multiplier),
		attack: Math.floor(baseAttack * (boss ? 3 : 1))
	};
}

function normalizedEquipmentSlot(slot) {
	return ({ Body: "Chest", Head: "Helm", Back: "Cloak" })[slot] || slot;
}

function getBattleCharacters() {
	const pulled = characters.filter((character) => (Number(state.ownedCharacters[character.id]) || 0) > 0);
	return pulled.length ? pulled : [starterFighter];
}

function getPartyStats() {
	const party = getBattleCharacters();
	const stats = { hp: 0, attack: 0, crit: 0, defense: 0 };
	for (const character of party) {
		const base = characterBaseStats[character.rarity] || characterBaseStats[3];
		const profile = characterCombatProfiles[character.id] || {};
		stats.hp += base.hp + (profile.hp || 0);
		stats.attack += base.attack + (profile.attack || 0);
		stats.crit += base.crit + (profile.crit || 0);
		stats.defense += base.defense + (profile.defense || 0);
		const duplicateCount = Math.max(0, (Number(state.ownedCharacters[character.id]) || 0) - 1);
		stats.hp += duplicateCount * 12;
		stats.attack += duplicateCount * 3;
		stats.crit += duplicateCount;
		stats.defense += duplicateCount;
	}
	for (const slot of armorSlots) {
		const itemId = state.battler.equipment[slot];
		const item = state.battler.armor.find((entry) => entry.id === itemId);
		if (!item) continue;
		const tier = Math.max(1, rarityNames.indexOf(item.rarity) + 1);
		const waveScale = Math.floor((Number(item.wave) || 1) / 40);
		if (slot === "Weapon") stats.attack += tier * 6 + waveScale * 2;
		else stats.defense += tier * 3 + waveScale;
		for (const bonus of item.bonuses || []) {
			if (bonus.stat === "Might") stats.attack += Number(bonus.value) || 0;
			if (bonus.stat === "Vitality") stats.hp += (Number(bonus.value) || 0) * 4;
			if (bonus.stat === "Haste" || bonus.stat === "Focus") stats.crit += Number(bonus.value) || 0;
			if (bonus.stat === "Aegis") stats.defense += Number(bonus.value) || 0;
		}
	}
	stats.crit = Math.min(100, stats.crit);
	return stats;
}

function getArenaEncounter(wave) {
	const biomeIndex = Math.floor((wave - 1) / 5) % battleBiomes.length;
	const biome = battleBiomes[biomeIndex];
	const boss = wave % 20 === 0;
	const adjective = monsterAdjectives[(wave * 3 + Math.floor(wave / 5)) % monsterAdjectives.length];
	const form = boss ? "Rift Warden" : monsterForms[(wave * 7 + Math.floor(wave / 2)) % monsterForms.length];
	return {
		biome,
		name: `${adjective} ${form}`,
		color: battleBiomes[(wave + 1) % battleBiomes.length].glow,
		variant: wave % 3
	};
}

function getBossMaxHealth(wave) {
	return 300 + wave * 30;
}

function getFighterDps() {
	const stats = getPartyStats();
	return Math.max(1, stats.attack * (0.55 + stats.crit / 100));
}

function processRegularWave(wave, battle, rewards) {
	const dropChance = Math.min(0.65, 0.12 + Math.floor(wave / 40) * 0.05);
	if (Math.random() < dropChance) {
		const armor = addArmorDrop(wave, false);
		rewards.unshift(`Wave ${wave} drop: ${armor.rarity} ${armor.name}${armor.autoEquipped ? " (auto-equipped)" : ""}`);
	}
	if (wave % 3 === 0) {
		battle.alloy += 3;
		rewards.unshift(`Wave ${wave}: +3 alloy`);
	}
}

function processBossRound(battle, now) {
	if (!battle.bossFight) {
		const maxHealth = getBossMaxHealth(battle.wave);
		battle.bossFight = {
			wave: battle.wave,
			health: maxHealth,
			maxHealth,
			deadline: battle.nextWaveAt > now ? battle.nextWaveAt : now + 30000,
			lastAttackAt: now
		};
	}
	const fight = battle.bossFight;
	const elapsed = Math.max(0, Math.min(1, (now - fight.lastAttackAt) / 1000));
	if (elapsed > 0) {
		fight.health -= getFighterDps() * elapsed;
		fight.lastAttackAt = now;
	}
	if (fight.health <= 0) {
		const armor = addArmorDrop(fight.wave, true);
		state.balance += BOSS_PULSE_REWARD;
		incrementWeekly("bosses");
		const bonuses = armor.bonuses.length ? ` (${armor.bonuses.map((bonus) => `${bonus.stat} +${bonus.value}`).join(", ")})` : "";
		battle.log = [`Boss defeated on wave ${fight.wave}: ${armor.rarity} ${armor.name}${bonuses} and +${BOSS_PULSE_REWARD} Pulse${armor.autoEquipped ? "; auto-equipped" : ""}`, ...battle.log].slice(0, 5);
		battle.bossFight = null;
		battle.bossFailedWave = null;
		battle.prepareBoss = false;
		battle.wave = fight.wave + 1;
		battle.nextWaveAt = now + getWaveDuration(battle.wave) * 1000;
		return true;
	}
	if (now >= fight.deadline) {
		battle.bossFailedWave = fight.wave;
		battle.prepareBoss = true;
		battle.bossFight = null;
		battle.wave = fight.wave - 1;
		battle.nextWaveAt = now + getWaveDuration(battle.wave) * 1000;
		battle.log = [`Boss escaped on wave ${fight.wave}. Farming wave ${battle.wave}; return when ready.`, ...battle.log].slice(0, 5);
		return true;
	}
	return elapsed > 0;
}

function processBattlerProgress(now = Date.now()) {
	const battle = state.battler;
	if (battle.paused) return false;
	if (battle.prepareBoss) {
		if (now < battle.nextWaveAt) return false;
		const farmWave = battle.bossFailedWave - 1;
		const rewards = [];
		processRegularWave(farmWave, battle, rewards);
		battle.wave = farmWave;
		battle.nextWaveAt += getWaveDuration(farmWave) * 1000;
		if (rewards.length) battle.log = [...rewards, ...battle.log].slice(0, 5);
		return true;
	}
	if (battle.wave % 20 === 0) return processBossRound(battle, now);
	let wavesCleared = 0;
	const rewards = [];
	while (now >= battle.nextWaveAt && wavesCleared < 2000 && battle.wave % 20 !== 0) {
		const wave = battle.wave;
		processRegularWave(wave, battle, rewards);
		battle.wave += 1;
		battle.nextWaveAt += getWaveDuration(battle.wave) * 1000;
		wavesCleared += 1;
	}
	if (!wavesCleared) return false;
	battle.log = [...rewards, ...battle.log].slice(0, 5);
	return true;
}

function renderFighter() {
	const battle = state.battler;
	const party = getBattleCharacters();
	const pulledCount = party.reduce((sum, character) => sum + (Number(state.ownedCharacters[character.id]) || 0), 0);
	const signature = JSON.stringify([
		party.map((character) => [character.id, state.ownedCharacters[character.id] || 0, state.characterImages[character.id] || ""]),
		battle.equipment,
		battle.armor
	]);
	if (elements.battleEquipment.dataset.signature === signature) return;
	elements.battleEquipment.dataset.signature = signature;
	elements.battlePartyName.textContent = pulledCount ? `${party.length}-character party` : starterFighter.name;
	elements.battlePartyCount.textContent = pulledCount ? `${pulledCount} pulls · all contribute power` : "Starter unit · pulls join your party";
	elements.battlePartyList.innerHTML = party.map((character) => {
		const owned = Number(state.ownedCharacters[character.id]) || 0;
		const portrait = state.characterImages[character.id] || `https://api.dicebear.com/9.x/adventurer/svg?seed=${encodeURIComponent(character.id)}&backgroundColor=${character.color.slice(1)}`;
		return `<article class="party-member"><img src="${escapeHtml(portrait)}" alt="" loading="lazy" decoding="async" referrerpolicy="no-referrer"><div><strong>${escapeHtml(character.name)}</strong><small>${character.rarity}-star · ${escapeHtml(character.source)}</small></div><span>${owned > 1 ? `×${owned}` : owned ? "OWNED" : "STARTER"}</span></article>`;
	}).join("");
	const stats = getPartyStats();
	elements.battleCharacterStats.innerHTML = [
		["HP", stats.hp], ["ATK", stats.attack], ["CRIT", `${stats.crit}%`], ["ARMOR", stats.defense]
	].map(([label, value]) => `<div class="fighter-stat"><span>${label}</span><strong>${value}</strong></div>`).join("");
	elements.battleEquipment.innerHTML = armorSlots.map((slot) => {
		const available = battle.armor.filter((item) => normalizedEquipmentSlot(item.slot) === slot);
		const selected = battle.equipment[slot] || "";
		const equipped = available.find((item) => item.id === selected);
		const options = available.map((item) => {
			const quantity = Math.max(1, Number(item.count) || 1);
			return `<option value="${item.id}" ${item.id === selected ? "selected" : ""}>${escapeHtml(item.rarity)} · ${escapeHtml(item.name)} ×${quantity}</option>`;
		}).join("");
		const summary = equipped ? `${equipped.rarity}${equipped.bonuses?.length ? ` · ${equipped.bonuses.map((bonus) => `${bonus.stat} +${bonus.value}`).join(" / ")}` : ""}` : "Unequipped";
		return `<article class="equipment-slot ${equipped ? `rarity-${equipped.rarity.toLowerCase()}` : ""}"><span class="equipment-slot-name">${slot}</span><select data-equipment-slot="${slot}" aria-label="Equip ${slot}"><option value="">Empty</option>${options}</select><small>${escapeHtml(summary)}</small></article>`;
	}).join("");
	elements.battleEquipment.querySelectorAll("[data-equipment-slot]").forEach((select) => select.addEventListener("change", () => {
		state.battler.equipment[select.dataset.equipmentSlot] = select.value || null;
		render();
	}));
}

function renderBattler() {
	const battle = state.battler;
	renderFighter();
	const isBoss = !battle.prepareBoss && battle.wave % 20 === 0;
	const now = battle.paused ? battle.pausedAt : Date.now();
	const duration = getWaveDuration(battle.wave);
	const remaining = Math.max(0, Math.ceil((battle.nextWaveAt - now) / 1000));
	const progress = Math.max(0, Math.min(100, (1 - (battle.nextWaveAt - now) / (duration * 1000)) * 100));
	const enemyStats = getEnemyStats(battle.wave, isBoss);
	elements.tabWave.textContent = `W${battle.wave}`;
	elements.battleToggle.textContent = battle.paused ? "Resume battle" : "Pause battle";
	elements.battlePhase.textContent = battle.prepareBoss ? `FARM WAVE ${battle.wave} · BOSS ${battle.bossFailedWave} FAILED` : isBoss ? `BOSS WAVE ${battle.wave}` : `WAVE ${battle.wave} · INFINITE DESCENT`;
	elements.battleOpponent.textContent = battle.prepareBoss ? "Rift scout · farming phase" : isBoss ? "Rift Warden" : ["Rift scout", "Hollow stalker", "Ashbound sentinel", "Shatterling"][(battle.wave - 1) % 4];
	elements.battleEnemyStats.textContent = `Level ${enemyStats.level} · HP ${enemyStats.health.toLocaleString()} · ATK ${enemyStats.attack.toLocaleString()}`;
	elements.battleCountdown.textContent = battle.paused ? "Expedition paused" : battle.prepareBoss ? `Farming wave ${battle.wave}; gear drops enabled` : isBoss ? `Boss deadline: ${remaining}s` : `Next wave in ${remaining}s`;
	elements.battleProgress.style.width = `${progress}%`;
	elements.bossHealthWrap.hidden = !isBoss;
	if (isBoss) {
		const maxHealth = battle.bossFight?.maxHealth || getBossMaxHealth(battle.wave);
		const health = Math.max(0, battle.bossFight?.health ?? maxHealth);
		elements.bossHealthLabel.textContent = `${Math.ceil(health).toLocaleString()} / ${maxHealth.toLocaleString()}`;
		elements.bossHealthBar.style.width = `${Math.max(0, Math.min(100, health / maxHealth * 100))}%`;
	}
	elements.bossFailureControls.hidden = !battle.prepareBoss;
	if (battle.prepareBoss) {
		elements.bossFailureNote.textContent = `Boss wave ${battle.bossFailedWave} timed out. Farm wave ${battle.wave}, equip better gear, then retry.`;
		elements.returnToBoss.textContent = `Return to wave ${battle.bossFailedWave} boss`;
	}
	elements.battleCrest.textContent = isBoss ? "♜" : "⚔";
	elements.battleCrest.classList.toggle("is-boss", isBoss);
	elements.arenaScene.classList.toggle("boss-arena", isBoss);
	if (elements.arenaScene.dataset.wave !== String(battle.wave)) {
		const encounter = getArenaEncounter(battle.wave);
		elements.arenaScene.dataset.wave = String(battle.wave);
		elements.arenaScene.style.setProperty("--biome-sky", encounter.biome.sky);
		elements.arenaScene.style.setProperty("--biome-ground", encounter.biome.ground);
		elements.arenaScene.style.setProperty("--biome-glow", encounter.biome.glow);
		elements.generatedMonster.style.setProperty("--monster-color", encounter.color);
		elements.arenaBiome.textContent = encounter.biome.name;
		elements.generatedMonsterName.textContent = encounter.name;
		elements.generatedMonster.setAttribute("aria-label", encounter.name);
		elements.generatedMonster.classList.remove("variant-0", "variant-1", "variant-2");
		elements.generatedMonster.classList.add(`variant-${encounter.variant}`);
		elements.arenaScene.classList.remove("scene-change");
		requestAnimationFrame(() => elements.arenaScene.classList.add("scene-change"));
	}
	elements.battleLog.innerHTML = battle.log.length
		? battle.log.map((entry) => `<p>${escapeHtml(entry)}</p>`).join("")
		: "<p>First wave approaching.</p>";
	elements.alloyCount.textContent = String(battle.alloy);
	const inventory = new Map();
	let armorTotal = 0;
	for (const drop of battle.armor) {
		const armor = armorDrops.find((item) => item.id === drop.id) || drop;
		const key = armor.rarity === "Unique" ? `unique:${drop.id}` : `${armor.rarity}:${armor.name}:${armor.slot}`;
		const entry = inventory.get(key) || { armor, count: 0, lastWave: 0 };
		const quantity = Math.max(1, Number(drop.count) || 1);
		entry.count += quantity;
		armorTotal += quantity;
		entry.lastWave = Math.max(entry.lastWave, Number(drop.wave) || 0);
		inventory.set(key, entry);
	}
	elements.armorCount.textContent = String(armorTotal);
	if (!inventory.size) {
		elements.armor.innerHTML = '<div class="empty-state"><strong>No armor yet</strong><p>Clear wave 5 for your first armor drop.</p></div>';
		return;
	}
	elements.armor.innerHTML = [...inventory.values()].map(({ armor, count, lastWave }) => {
		const bonuses = Array.isArray(armor.bonuses) ? armor.bonuses : [];
		return `<div class="armor-item rarity-${armor.rarity.toLowerCase()}"><span class="armor-icon" aria-hidden="true">◇</span><span class="armor-copy"><strong>${escapeHtml(armor.name)}</strong><small>${escapeHtml(armor.rarity)} · ${escapeHtml(normalizedEquipmentSlot(armor.slot))} · W${lastWave}</small>${bonuses.length ? `<span class="armor-bonuses">${bonuses.map((bonus) => `${escapeHtml(bonus.stat)} +${Number(bonus.value)}`).join(" · ")}</span>` : ""}</span><span class="armor-quantity">×${count}</span></div>`;
	}).join("");
}

function rollCharacter() {
	let rarity;
	if (state.pityFive + 1 >= 50 || Math.random() < 0.06) {
		rarity = 5;
		state.pityFive = 0;
		state.pityFour = 0;
	} else {
		state.pityFive += 1;
		if (state.pityFour + 1 >= 10 || Math.random() < 0.24) {
			rarity = 4;
			state.pityFour = 0;
		} else {
			rarity = 3;
			state.pityFour += 1;
		}
	}
	const pool = characters.filter((character) => character.rarity === rarity);
	return pool[Math.floor(Math.random() * pool.length)];
}

function renderGacha() {
	const ownedCount = Object.values(state.ownedCharacters).filter((count) => count > 0).length;
	const selectedPortrait = elements.portraitCharacter.value;
	elements.portraitCharacter.innerHTML = characters.map((character) => `<option value="${character.id}">${escapeHtml(character.name)} · ${escapeHtml(character.source)}</option>`).join("");
	if (characters.some((character) => character.id === selectedPortrait)) elements.portraitCharacter.value = selectedPortrait;
	elements.collectionCount.textContent = `${ownedCount}/${characters.length}`;
	elements.pityFive.textContent = String(Math.max(1, 50 - state.pityFive));
	elements.pityFour.textContent = String(Math.max(1, 10 - state.pityFour));
	elements.tabPulse.textContent = `${state.balance.toLocaleString()} P`;
	elements.clearPortrait.disabled = !state.characterImages[elements.portraitCharacter.value];
	elements.pullOne.disabled = state.balance < SINGLE_PULL_COST;
	elements.pullTen.disabled = state.balance < TEN_PULL_COST;
	const results = state.lastPulls.map((id) => characters.find((character) => character.id === id)).filter(Boolean);
	elements.gachaResult.textContent = results.length
		? `Last signal: ${results.map((character) => `${character.name} ${"★".repeat(character.rarity)}`).join(" · ")}`
		: "Earn Pulse from daily quests and event clears to pull.";
	elements.collection.innerHTML = [...characters].sort((a, b) => b.rarity - a.rarity || a.name.localeCompare(b.name)).map((character) => {
		const owned = Number(state.ownedCharacters[character.id]) || 0;
		const duplicateBonus = Math.max(0, owned - 1) * 3;
		const initials = character.name.split(/\s+/).slice(0, 2).map((part) => part[0]).join("");
		const avatarUrl = state.characterImages[character.id] || `https://api.dicebear.com/9.x/adventurer/svg?seed=${encodeURIComponent(character.id)}&backgroundColor=${character.color.slice(1)}`;
		return `<article class="character-card rarity-${character.rarity} ${owned ? "is-owned" : ""}">
			<div class="character-portrait" style="--sigil-color:${character.color}"><img src="${escapeHtml(avatarUrl)}" alt="${state.characterImages[character.id] ? `${escapeHtml(character.name)} portrait` : ""}" loading="lazy" decoding="async" referrerpolicy="no-referrer"><span hidden>${escapeHtml(initials)}</span></div>
			<div class="character-info"><span class="character-name">${escapeHtml(character.name)}</span><span class="character-source">${escapeHtml(character.source)}</span><span class="character-stars">${"★".repeat(character.rarity)}<span>${character.rarity}★</span></span></div>
			<span class="character-owned">${owned ? `×${owned}${duplicateBonus ? ` · +${duplicateBonus} ATK` : ""}` : "NOT PULLED"}</span>
		</article>`;
	}).join("");
	elements.collection.querySelectorAll(".character-portrait img").forEach((image) => image.addEventListener("error", () => {
		image.hidden = true;
		image.nextElementSibling.hidden = false;
	}));
}

async function saveCharacterImage(file) {
	if (!file || !file.type.startsWith("image/")) return;
	if (file.size > 12 * 1024 * 1024) {
		showToast("Choose an image smaller than 12 MB");
		return;
	}
	const objectUrl = URL.createObjectURL(file);
	try {
		const image = new Image();
		image.src = objectUrl;
		await image.decode();
		const scale = Math.min(1, 320 / image.width, 420 / image.height);
		const canvas = document.createElement("canvas");
		canvas.width = Math.max(1, Math.round(image.width * scale));
		canvas.height = Math.max(1, Math.round(image.height * scale));
		canvas.getContext("2d").drawImage(image, 0, 0, canvas.width, canvas.height);
		const dataUrl = canvas.toDataURL("image/webp", 0.75);
		if (dataUrl.length > 140000) {
			showToast("This picture is too detailed to store locally");
			return;
		}
		state.characterImages[elements.portraitCharacter.value] = dataUrl;
		render();
		showToast("Local character picture saved");
	} catch {
		showToast("That picture could not be opened");
	} finally {
		URL.revokeObjectURL(objectUrl);
		elements.portraitFile.value = "";
	}
}

function pull(count) {
	const cost = count === 1 ? SINGLE_PULL_COST : TEN_PULL_COST;
	if (state.balance < cost) {
		showToast(`You need ${cost} Pulse for this pull`);
		return;
	}
	state.balance -= cost;
	const results = Array.from({ length: count }, () => rollCharacter());
	for (const character of results) {
		state.ownedCharacters[character.id] = (Number(state.ownedCharacters[character.id]) || 0) + 1;
	}
	state.totalPulls += count;
	state.lastPulls = results.map((character) => character.id);
	render();
	showToast(count === 1 ? "Signal acquired" : `${count} signals acquired`);
}

function setSelectOptions(select, preferredId) {
	select.innerHTML = state.games.map((game) => `<option value="${game.id}">${escapeHtml(game.name)}</option>`).join("");
	if (preferredId && state.games.some((game) => game.id === preferredId)) select.value = preferredId;
}

function showToast(message) {
	elements.toast.textContent = message;
	elements.toast.classList.add("is-visible");
	clearTimeout(toastTimer);
	toastTimer = setTimeout(() => elements.toast.classList.remove("is-visible"), 2200);
}

function openDialog(dialog) {
	dialog.showModal();
}

function activateView(tab, focus = false) {
	const tabs = [...document.querySelectorAll("[data-view-tab]")];
	const activePanel = tab.getAttribute("aria-controls");
	for (const item of tabs) {
		const isActive = item === tab;
		item.classList.toggle("is-active", isActive);
		item.setAttribute("aria-selected", String(isActive));
		item.tabIndex = isActive ? 0 : -1;
	}
	for (const panel of document.querySelectorAll("[data-tab-panel]")) {
		panel.hidden = panel.id !== activePanel;
	}
	if (focus) tab.focus();
}

const viewTabs = [...document.querySelectorAll("[data-view-tab]")];
viewTabs.forEach((tab, index) => {
	tab.addEventListener("click", () => activateView(tab));
	tab.addEventListener("keydown", (event) => {
		let nextIndex = index;
		if (event.key === "ArrowRight") nextIndex = (index + 1) % viewTabs.length;
		else if (event.key === "ArrowLeft") nextIndex = (index - 1 + viewTabs.length) % viewTabs.length;
		else if (event.key === "Home") nextIndex = 0;
		else if (event.key === "End") nextIndex = viewTabs.length - 1;
		else return;
		event.preventDefault();
		activateView(viewTabs[nextIndex], true);
	});
});

document.querySelectorAll("[data-close-dialog]").forEach((button) => button.addEventListener("click", () => button.closest("dialog").close()));
document.querySelectorAll("[data-event-filter]").forEach((button) => button.addEventListener("click", () => {
	eventFilter = button.dataset.eventFilter;
	document.querySelectorAll("[data-event-filter]").forEach((tab) => tab.classList.toggle("is-active", tab === button));
	renderEvents();
}));

document.querySelector("#open-event-form").addEventListener("click", () => {
	if (!state.games.length) { showToast("Add a game first"); openDialog(elements.gameDialog); return; }
	setSelectOptions(elements.eventForm.elements.gameId, selectedGameId);
	elements.eventForm.querySelector(".form-error").textContent = "";
	openDialog(elements.eventDialog);
});
document.querySelector("#open-game-form").addEventListener("click", () => openDialog(elements.gameDialog));
document.querySelector("#open-quest-form").addEventListener("click", () => {
	if (!state.games.length) { showToast("Add a game first"); openDialog(elements.gameDialog); return; }
	setSelectOptions(elements.questForm.elements.gameId, elements.questGame.value);
	openDialog(elements.questDialog);
});

elements.gameForm.addEventListener("submit", (event) => {
	event.preventDefault();
	const nameInput = elements.gameForm.elements.name;
	const name = nameInput.value.trim();
	if (!name) return;
	if (state.games.some((game) => game.name.toLowerCase() === name.toLowerCase())) {
		nameInput.setCustomValidity("That game is already in your library.");
		nameInput.reportValidity();
		return;
	}
	nameInput.setCustomValidity("");
	const game = { id: makeId(), name, color: gameColors[state.games.length % gameColors.length] };
	state.games.push(game);
	state.quests.push(
		{ id: makeId(), gameId: game.id, title: "Play for 15 minutes", reward: 20, completedOn: null, earnedOn: null },
		{ id: makeId(), gameId: game.id, title: "Complete one activity", reward: 25, completedOn: null, earnedOn: null }
	);
	selectedGameId = game.id;
	elements.gameForm.reset();
	elements.gameDialog.close();
	render();
	elements.questGame.value = game.id;
	renderQuests();
	showToast(`${name} added to your library`);
});

elements.eventForm.addEventListener("submit", (event) => {
	event.preventDefault();
	const formData = new FormData(elements.eventForm);
	const startDate = String(formData.get("startDate"));
	const endDate = String(formData.get("endDate"));
	const error = elements.eventForm.querySelector(".form-error");
	if (endDate < startDate) { error.textContent = "The end date must be on or after the start date."; return; }
	state.events.push({ id: makeId(), gameId: String(formData.get("gameId")), title: String(formData.get("title")).trim(), startDate, endDate, done: false, sample: false });
	elements.eventForm.reset();
	elements.eventDialog.close();
	eventFilter = "all";
	document.querySelectorAll("[data-event-filter]").forEach((tab) => tab.classList.toggle("is-active", tab.dataset.eventFilter === "all"));
	render();
	showToast("Event added to your radar");
});

elements.questForm.addEventListener("submit", (event) => {
	event.preventDefault();
	const formData = new FormData(elements.questForm);
	const title = String(formData.get("title")).trim();
	const reward = Number(formData.get("reward"));
	if (!title || !Number.isInteger(reward) || reward < 1 || reward > 1000) return;
	const gameId = String(formData.get("gameId"));
	state.quests.push({ id: makeId(), gameId, title, reward, completedOn: null, earnedOn: null });
	elements.questForm.reset();
	elements.questDialog.close();
	render();
	elements.questGame.value = gameId;
	renderQuests();
	showToast("Daily quest added");
});

elements.questGame.addEventListener("change", renderQuests);
elements.gameForm.elements.name.addEventListener("input", () => elements.gameForm.elements.name.setCustomValidity(""));
elements.pullOne.addEventListener("click", () => pull(1));
elements.pullTen.addEventListener("click", () => pull(10));
elements.portraitCharacter.addEventListener("change", renderGacha);
elements.portraitFile.addEventListener("change", () => saveCharacterImage(elements.portraitFile.files?.[0]));
elements.clearPortrait.addEventListener("click", () => {
	const characterId = elements.portraitCharacter.value;
	if (!state.characterImages[characterId]) return;
	delete state.characterImages[characterId];
	render();
	showToast("Local character picture removed");
});
elements.returnToBoss.addEventListener("click", () => {
	const battle = state.battler;
	if (!battle.prepareBoss || !battle.bossFailedWave) return;
	battle.wave = battle.bossFailedWave;
	battle.nextWaveAt = Date.now() + getWaveDuration(battle.wave) * 1000;
	battle.bossFight = null;
	battle.bossFailedWave = null;
	battle.prepareBoss = false;
	render();
});
elements.battleToggle.addEventListener("click", () => {
	const battle = state.battler;
	if (battle.paused) {
		const pausedDuration = Date.now() - battle.pausedAt;
		battle.nextWaveAt += pausedDuration;
		if (battle.bossFight) {
			battle.bossFight.deadline += pausedDuration;
			battle.bossFight.lastAttackAt += pausedDuration;
		}
		battle.pausedAt = 0;
		battle.paused = false;
	} else {
		battle.paused = true;
		battle.pausedAt = Date.now();
	}
	render();
});

setInterval(() => {
	const currentDate = dateKey(new Date());
	let shouldRender = processBattlerProgress();
	if (currentDate !== today) {
		today = currentDate;
		shouldRender = true;
	}
	if (state.weekly.weekOf !== getWeekKey()) {
		state.weekly = createWeeklyState();
		shouldRender = true;
	}
	if (shouldRender) render();
	else renderBattler();
	elements.events.querySelectorAll("[data-event-countdown]").forEach((label) => {
		const event = state.events.find((item) => item.id === label.dataset.eventCountdown);
		if (event) label.textContent = getEventCountdown(event);
	});
}, 1000);

render();
