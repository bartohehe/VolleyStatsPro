using System.Collections.Generic;

namespace VolleyStatsPro.Helpers
{
    /// <summary>
    /// Static localization helper. Call Loc.Get("key") anywhere in the UI.
    /// Language is read from SettingsManager at startup; a restart applies changes.
    /// </summary>
    public static class Loc
    {
        // ── Available languages shown in the Settings dropdown ──────────────────
        public static readonly (string Code, string DisplayName)[] Languages =
        {
            ("en", "English"),
            ("pl", "Polski (Polish)"),
            ("de", "Deutsch (German)"),
            ("fr", "Français (French)"),
            ("es", "Español (Spanish)"),
            ("it", "Italiano (Italian)"),
            ("cs", "Čeština (Czech)"),
        };

        // ── Lookup ───────────────────────────────────────────────────────────────
        private static readonly Dictionary<string, Dictionary<string, string>> _all = new()
        {
            ["en"] = English,
            ["pl"] = Polish,
            ["de"] = German,
            ["fr"] = French,
            ["es"] = Spanish,
            ["it"] = Italian,
            ["cs"] = Czech,
        };

        private static string _lang = "en";

        public static void Init(string lang)
        {
            _lang = _all.ContainsKey(lang) ? lang : "en";
        }

        public static string Get(string key)
        {
            if (_all.TryGetValue(_lang, out var dict) && dict.TryGetValue(key, out var val))
                return val;
            // fallback to English
            if (_all["en"].TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }

        // ── English ──────────────────────────────────────────────────────────────
        private static Dictionary<string, string> English => new()
        {
            // Navigation
            ["nav.dashboard"]    = "Dashboard",
            ["nav.players"]      = "Players",
            ["nav.matches"]      = "Matches",
            ["nav.teamstats"]    = "Team Stats",
            ["nav.manageteams"]  = "Manage Teams",
            ["nav.settings"]     = "Settings",

            // Header
            ["header.startmatch"] = "▶  Start Match",

            // Common
            ["common.team"]      = "Team:",
            ["common.save"]      = "Save",
            ["common.cancel"]    = "Cancel",
            ["common.delete"]    = "Delete",
            ["common.edit"]      = "Edit",
            ["common.name"]      = "Name:",
            ["common.confirm"]   = "Confirm",
            ["common.error"]     = "Error",
            ["common.ok"]        = "OK",
            ["common.yes"]       = "Yes",
            ["common.no"]        = "No",

            // Dashboard
            ["dash.title"]        = "Team Dashboard",
            ["dash.subtitle"]     = "Comprehensive performance analytics and key metrics",
            ["dash.playerperf"]   = "Player Performance Comparison",
            ["dash.skills"]       = "Skills Analysis vs League Average",
            ["dash.topperf"]      = "Top Performers",
            ["dash.topperf.hint"] = "pts = Kills + Aces + Blocks",
            ["dash.form"]         = "Last 8 Matches Form",
            ["dash.winrate"]      = "WIN RATE",
            ["dash.attackeff"]    = "ATTACK EFF",
            ["dash.blockavg"]     = "BLOCK AVG",
            ["dash.serveeff"]     = "SERVE EFF",
            ["dash.kpi.attack"]   = "K-E/Att",
            ["dash.kpi.perSet"]   = "Per Set",
            ["dash.kpi.serve"]    = "A/(A+SE)",
            ["dash.w"]            = "W",
            ["dash.l"]            = "L",

            // Action types
            ["action.attack"]     = "Attack",
            ["action.serve"]      = "Serve",
            ["action.reception"]  = "Reception",
            ["action.block"]      = "Block",
            ["action.defense"]    = "Defense",
            ["action.setting"]    = "Setting",
            ["action.dig"]        = "Dig",

            // Players view
            ["players.title"]      = "Players",
            ["players.subtitle"]   = "Individual player profiles and statistics",
            ["players.attacks"]    = "ATTACKS",
            ["players.efficiency"] = "EFFICIENCY",
            ["players.eff.hint"]   = "Kills-Errors/Att",
            ["players.serves"]     = "SERVES",
            ["players.blocks"]     = "BLOCKS",
            ["players.reception"]  = "RECEPTION",
            ["players.digs"]       = "DIGS",
            ["players.attackmap"]  = "Attack Map",
            ["players.servemap"]   = "Serve Map",
            ["players.recepmap"]   = "Reception Map",
            ["players.active"]     = "ACTIVE",
            ["players.inactive"]   = "INACTIVE",

            // Positions
            ["pos.oh"]  = "Outside Hitter",
            ["pos.mb"]  = "Middle Blocker",
            ["pos.s"]   = "Setter",
            ["pos.l"]   = "Libero",
            ["pos.opp"] = "Opposite",
            ["pos.ds"]  = "Defensive Specialist",

            // Matches view
            ["matches.title"]         = "Matches",
            ["matches.subtitle"]      = "Schedule, results and live tracking",
            ["matches.new"]           = "+ New Match",
            ["matches.openlive"]      = "▶ Open Live",
            ["matches.exportcsv"]     = "Export CSV",
            ["matches.exportpdf"]     = "Export PDF",
            ["matches.col.date"]      = "Date",
            ["matches.col.home"]      = "Home",
            ["matches.col.score"]     = "Score",
            ["matches.col.away"]      = "Away",
            ["matches.col.location"]  = "Location",
            ["matches.col.status"]    = "Status",
            ["matches.nomatch"]       = "No match selected",
            ["matches.selectexport"]  = "Select a match to export.",
            ["matches.exported"]      = "Statistics exported to:",
            ["matches.exportdone"]    = "Export complete",
            ["matches.pdfexported"]   = "PDF exported to:",
            ["matches.exportfailed"]  = "Export failed:",
            ["matches.deleteconfirm"] = "Delete this match and all its data?",
            ["matches.noteams"]       = "You need at least 2 teams to create a match.\nGo to Manage Teams first.",
            ["matches.noteams.title"] = "Not enough teams",

            // New Match dialog
            ["newmatch.title"]    = "New Match",
            ["newmatch.home"]     = "Home Team:",
            ["newmatch.away"]     = "Away Team:",
            ["newmatch.date"]     = "Date:",
            ["newmatch.location"] = "Location:",
            ["newmatch.create"]   = "Create",
            ["newmatch.val.teams"]  = "Select both teams.",
            ["newmatch.val.same"]   = "Home and Away must be different teams.",

            // Status values
            ["status.scheduled"] = "Scheduled",
            ["status.live"]      = "Live",
            ["status.finished"]  = "Finished",

            // Team Stats view
            ["teamstats.title"]     = "Team Statistics",
            ["teamstats.subtitle"]  = "Performance metrics and analysis",
            ["teamstats.tab.overview"]  = "Overview",
            ["teamstats.tab.players"]   = "Players",
            ["teamstats.tab.heatmaps"]  = "Heatmaps",
            ["teamstats.tab.radar"]     = "Radar",
            ["teamstats.winrate"]    = "WIN RATE",
            ["teamstats.attackeff"]  = "ATTACK EFF",
            ["teamstats.blockkills"] = "BLOCK KILLS",
            ["teamstats.serveeff"]   = "SERVE EFF",
            ["teamstats.totals"]     = "Action Totals",
            ["teamstats.col.name"]   = "Name",
            ["teamstats.col.pos"]    = "Pos",
            ["teamstats.col.ace"]    = "Ace",
            ["teamstats.col.srverr"] = "SrvErr",
            ["teamstats.col.kill"]   = "Kill",
            ["teamstats.col.atterr"] = "AttErr",
            ["teamstats.col.recep"]  = "Recep",
            ["teamstats.heat.serve"] = "Serve Heatmap",
            ["teamstats.heat.attack"]= "Attack Heatmap",
            ["teamstats.heat.block"] = "Block Heatmap",

            // Manage Teams view
            ["manage.title"]       = "Team Management",
            ["manage.subtitle"]    = "Add and manage teams and their players",
            ["manage.addteam"]     = "+ Add Team",
            ["manage.teams"]       = "Teams",
            ["manage.delteam"]     = "Delete Team",
            ["manage.players"]     = "Players",
            ["manage.addplayer"]   = "+ Add Player",
            ["manage.teamname"]    = "Team Name:",
            ["manage.shortname"]   = "Short Name:",
            ["manage.color"]       = "Color:",
            ["manage.pickcolor"]   = "Pick Color",
            ["manage.addteam.dlg"] = "Add Team",
            ["manage.editteam.dlg"]= "Edit Team",
            ["manage.addplayer.dlg"]  = "Add Player",
            ["manage.editplayer.dlg"] = "Edit Player",
            ["manage.number"]      = "Number:",
            ["manage.position"]    = "Position:",
            ["manage.val.name"]    = "Name required.",
            ["manage.val.number"]  = "Number must be 1–99.",
            ["manage.val.selectteam"] = "Select a team first.",
            ["manage.del.team"]    = "Delete team '{0}'?\nAll players and match data will be removed.",
            ["manage.del.player"]  = "Delete player '{0}'?",
            ["manage.colorhint"]   = "Enter hex color (e.g. #FF5500):",

            // Live Match view
            ["live.nomatch"]       = "No Match Loaded",
            ["live.back"]          = "Matches",
            ["live.heatmap"]       = "Live Heatmap",
            ["live.zone"]          = "Zone (click to pre-select)",
            ["live.homept"]        = "HOME pt (/er h)",
            ["live.awaypt"]        = "AWAY pt (/er a)",
            ["live.endset"]        = "End Set (/es)",
            ["live.endmatch"]      = "End Match (/em)",
            ["live.home"]          = "HOME",
            ["live.away"]          = "AWAY",
            ["live.rotation"]      = "ROTATION",
            ["live.setlineup"]     = "Set Lineup",
            ["live.savelineup"]    = "Save Lineup",
            ["live.setlineup.dlg"] = "Set Lineup — Set {0}",
            ["live.tab.attack"]    = "Attack",
            ["live.tab.serve"]     = "Serve",

            // Settings dialog
            ["settings.title"]        = "Settings",
            ["settings.datastorage"]  = "Data Storage",
            ["settings.dblabel"]      = "Database folder  (file: volleystats.db)",
            ["settings.browse"]       = "Browse…",
            ["settings.copydb"]       = "Copy existing database to new location",
            ["settings.reset"]        = "Reset to Default",
            ["settings.language"]     = "Language",
            ["settings.restart.msg"]  = "Language change will take effect after restarting.\nRestart now?",
            ["settings.restart.title"]= "Restart required",
            ["settings.restart.btn"]  = "Restart Now",
            ["settings.saved"]        = "Settings saved",
            ["settings.db.updated"]   = "Database location updated.\nNow using:\n{0}",
            ["settings.val.folder"]   = "Please enter a valid folder path.",
            ["settings.val.nocreate"] = "Cannot create folder:\n{0}",
            ["settings.copy.failed"]  = "Failed to copy database:\n{0}\n\nSave new path anyway?",
            ["settings.copy.title"]   = "Copy failed",
        };

        // ── Polish ───────────────────────────────────────────────────────────────
        private static Dictionary<string, string> Polish => new()
        {
            ["nav.dashboard"]    = "Panel główny",
            ["nav.players"]      = "Zawodnicy",
            ["nav.matches"]      = "Mecze",
            ["nav.teamstats"]    = "Statystyki",
            ["nav.manageteams"]  = "Drużyny",
            ["nav.settings"]     = "Ustawienia",

            ["header.startmatch"] = "▶  Rozpocznij mecz",

            ["common.team"]      = "Drużyna:",
            ["common.save"]      = "Zapisz",
            ["common.cancel"]    = "Anuluj",
            ["common.delete"]    = "Usuń",
            ["common.edit"]      = "Edytuj",
            ["common.name"]      = "Imię:",
            ["common.confirm"]   = "Potwierdź",
            ["common.error"]     = "Błąd",
            ["common.ok"]        = "OK",
            ["common.yes"]       = "Tak",
            ["common.no"]        = "Nie",

            ["dash.title"]        = "Panel drużyny",
            ["dash.subtitle"]     = "Kompleksowe analizy i kluczowe wskaźniki",
            ["dash.playerperf"]   = "Porównanie wydajności zawodników",
            ["dash.skills"]       = "Analiza umiejętności vs średnia ligowa",
            ["dash.topperf"]      = "Najlepsi zawodnicy",
            ["dash.topperf.hint"] = "pkt = Zabójstwa + Asy + Bloki",
            ["dash.form"]         = "Forma – ostatnie 8 meczów",
            ["dash.winrate"]      = "% WYGRANYCH",
            ["dash.attackeff"]    = "EFF. ATAKU",
            ["dash.blockavg"]     = "ŚR. BLOKÓW",
            ["dash.serveeff"]     = "EFF. ZAGRYWKI",
            ["dash.kpi.attack"]   = "K-B/Ata",
            ["dash.kpi.perSet"]   = "Na set",
            ["dash.kpi.serve"]    = "As/(As+BłZ)",
            ["dash.w"]            = "W",
            ["dash.l"]            = "P",

            ["action.attack"]     = "Atak",
            ["action.serve"]      = "Zagrywka",
            ["action.reception"]  = "Przyjęcie",
            ["action.block"]      = "Blok",
            ["action.defense"]    = "Obrona",
            ["action.setting"]    = "Rozgrywka",
            ["action.dig"]        = "Obrona",

            ["players.title"]      = "Zawodnicy",
            ["players.subtitle"]   = "Profile i statystyki zawodników",
            ["players.attacks"]    = "ATAKI",
            ["players.efficiency"] = "WYDAJNOŚĆ",
            ["players.eff.hint"]   = "(Kill-Błędy)/Ata",
            ["players.serves"]     = "ZAGRYWKI",
            ["players.blocks"]     = "BLOKI",
            ["players.reception"]  = "PRZYJĘCIE",
            ["players.digs"]       = "OBRONA",
            ["players.attackmap"]  = "Mapa ataku",
            ["players.servemap"]   = "Mapa zagrywki",
            ["players.recepmap"]   = "Mapa przyjęcia",
            ["players.active"]     = "AKTYWNY",
            ["players.inactive"]   = "NIEAKTYWNY",

            ["pos.oh"]  = "Przyjmujący",
            ["pos.mb"]  = "Środkowy",
            ["pos.s"]   = "Rozgrywający",
            ["pos.l"]   = "Libero",
            ["pos.opp"] = "Atakujący",
            ["pos.ds"]  = "Spec. obrony",

            ["matches.title"]         = "Mecze",
            ["matches.subtitle"]      = "Harmonogram, wyniki i śledzenie na żywo",
            ["matches.new"]           = "+ Nowy mecz",
            ["matches.openlive"]      = "▶ Otwórz",
            ["matches.exportcsv"]     = "Eksport CSV",
            ["matches.exportpdf"]     = "Eksport PDF",
            ["matches.col.date"]      = "Data",
            ["matches.col.home"]      = "Gospodarz",
            ["matches.col.score"]     = "Wynik",
            ["matches.col.away"]      = "Gość",
            ["matches.col.location"]  = "Miejsce",
            ["matches.col.status"]    = "Status",
            ["matches.nomatch"]       = "Nie wybrano meczu",
            ["matches.selectexport"]  = "Wybierz mecz do eksportu.",
            ["matches.exported"]      = "Statystyki wyeksportowane do:",
            ["matches.exportdone"]    = "Eksport zakończony",
            ["matches.pdfexported"]   = "PDF wyeksportowany do:",
            ["matches.exportfailed"]  = "Eksport nieudany:",
            ["matches.deleteconfirm"] = "Usunąć ten mecz i wszystkie jego dane?",
            ["matches.noteams"]       = "Potrzebujesz co najmniej 2 drużyn.\nPrzejdź do sekcji Drużyny.",
            ["matches.noteams.title"] = "Za mało drużyn",

            ["newmatch.title"]    = "Nowy mecz",
            ["newmatch.home"]     = "Gospodarz:",
            ["newmatch.away"]     = "Gość:",
            ["newmatch.date"]     = "Data:",
            ["newmatch.location"] = "Miejsce:",
            ["newmatch.create"]   = "Utwórz",
            ["newmatch.val.teams"]  = "Wybierz obie drużyny.",
            ["newmatch.val.same"]   = "Gospodarz i Gość muszą być różnymi drużynami.",

            ["status.scheduled"] = "Zaplanowany",
            ["status.live"]      = "Na żywo",
            ["status.finished"]  = "Zakończony",

            ["teamstats.title"]     = "Statystyki drużyny",
            ["teamstats.subtitle"]  = "Wskaźniki wydajności i analiza",
            ["teamstats.tab.overview"]  = "Przegląd",
            ["teamstats.tab.players"]   = "Zawodnicy",
            ["teamstats.tab.heatmaps"]  = "Mapy cieplne",
            ["teamstats.tab.radar"]     = "Radar",
            ["teamstats.winrate"]    = "% WYGRANYCH",
            ["teamstats.attackeff"]  = "EFF. ATAKU",
            ["teamstats.blockkills"] = "BLOKI SKUT.",
            ["teamstats.serveeff"]   = "EFF. ZAGRYWKI",
            ["teamstats.totals"]     = "Łączne akcje",
            ["teamstats.col.name"]   = "Imię",
            ["teamstats.col.pos"]    = "Poz",
            ["teamstats.col.ace"]    = "As",
            ["teamstats.col.srverr"] = "BłZag",
            ["teamstats.col.kill"]   = "Zabój",
            ["teamstats.col.atterr"] = "BłAta",
            ["teamstats.col.recep"]  = "Przyj",
            ["teamstats.heat.serve"] = "Mapa cieplna zagrywki",
            ["teamstats.heat.attack"]= "Mapa cieplna ataku",
            ["teamstats.heat.block"] = "Mapa cieplna bloku",

            ["manage.title"]       = "Zarządzanie drużynami",
            ["manage.subtitle"]    = "Dodawaj i zarządzaj drużynami oraz zawodnikami",
            ["manage.addteam"]     = "+ Dodaj drużynę",
            ["manage.teams"]       = "Drużyny",
            ["manage.delteam"]     = "Usuń drużynę",
            ["manage.players"]     = "Zawodnicy",
            ["manage.addplayer"]   = "+ Dodaj zawodnika",
            ["manage.teamname"]    = "Nazwa drużyny:",
            ["manage.shortname"]   = "Skrót:",
            ["manage.color"]       = "Kolor:",
            ["manage.pickcolor"]   = "Wybierz kolor",
            ["manage.addteam.dlg"] = "Dodaj drużynę",
            ["manage.editteam.dlg"]= "Edytuj drużynę",
            ["manage.addplayer.dlg"]  = "Dodaj zawodnika",
            ["manage.editplayer.dlg"] = "Edytuj zawodnika",
            ["manage.number"]      = "Numer:",
            ["manage.position"]    = "Pozycja:",
            ["manage.val.name"]    = "Nazwa jest wymagana.",
            ["manage.val.number"]  = "Numer musi być od 1 do 99.",
            ["manage.val.selectteam"] = "Najpierw wybierz drużynę.",
            ["manage.del.team"]    = "Usun\u0105\u0107 dru\u017cyn\u0119 '{0}'?\nWszystkie dane zostan\u0105 usuni\u0119te.",
            ["manage.del.player"]  = "Usun\u0105\u0107 zawodnika '{0}'?",
            ["manage.colorhint"]   = "Podaj kolor hex (np. #FF5500):",

            ["live.nomatch"]       = "Brak meczu",
            ["live.back"]          = "Mecze",
            ["live.heatmap"]       = "Heatmap",
            ["live.zone"]          = "Strefa (kliknij, aby wybrać)",
            ["live.homept"]        = "PUNKT GOSP (/er h)",
            ["live.awaypt"]        = "PUNKT GOŚCI (/er a)",
            ["live.endset"]        = "Koniec seta (/es)",
            ["live.endmatch"]      = "Koniec meczu (/em)",
            ["live.home"]          = "GOSP",
            ["live.away"]          = "GOŚĆ",
            ["live.rotation"]      = "USTAWIENIE",
            ["live.setlineup"]     = "Ustaw skład",
            ["live.savelineup"]    = "Zapisz skład",
            ["live.setlineup.dlg"] = "Ustaw skład — Set {0}",
            ["live.tab.attack"]    = "Atak",
            ["live.tab.serve"]     = "Zagrywka",

            ["settings.title"]        = "Ustawienia",
            ["settings.datastorage"]  = "Przechowywanie danych",
            ["settings.dblabel"]      = "Folder bazy danych  (plik: volleystats.db)",
            ["settings.browse"]       = "Przeglądaj…",
            ["settings.copydb"]       = "Kopiuj istniejącą bazę danych do nowej lokalizacji",
            ["settings.reset"]        = "Przywróć domyślne",
            ["settings.language"]     = "Język",
            ["settings.restart.msg"]  = "Zmiana języka wymaga ponownego uruchomienia.\nUruchomić teraz?",
            ["settings.restart.title"]= "Wymagany restart",
            ["settings.restart.btn"]  = "Uruchom ponownie",
            ["settings.saved"]        = "Ustawienia zapisane",
            ["settings.db.updated"]   = "Zaktualizowano lokalizację bazy danych.\nUżywana ścieżka:\n{0}",
            ["settings.val.folder"]   = "Podaj prawidłową ścieżkę folderu.",
            ["settings.val.nocreate"] = "Nie można utworzyć folderu:\n{0}",
            ["settings.copy.failed"]  = "Nie udało się skopiować bazy danych:\n{0}\n\nZapisać nową ścieżkę mimo to?",
            ["settings.copy.title"]   = "Kopiowanie nieudane",
        };

        // ── German (stub — falls back to English for missing keys) ───────────────
        private static Dictionary<string, string> German => new()
        {
            ["nav.dashboard"]   = "Dashboard",
            ["nav.players"]     = "Spieler",
            ["nav.matches"]     = "Spiele",
            ["nav.teamstats"]   = "Team-Statistik",
            ["nav.manageteams"] = "Teams verwalten",
            ["nav.settings"]    = "Einstellungen",
            ["header.startmatch"] = "▶  Spiel starten",
            ["common.save"]     = "Speichern",
            ["common.cancel"]   = "Abbrechen",
            ["common.delete"]   = "Löschen",
            ["common.edit"]     = "Bearbeiten",
            ["settings.title"]  = "Einstellungen",
            ["settings.language"] = "Sprache",
            ["settings.restart.msg"]   = "Die Sprachänderung wird nach einem Neustart wirksam.\nJetzt neu starten?",
            ["settings.restart.title"] = "Neustart erforderlich",
            ["settings.restart.btn"]   = "Jetzt neu starten",
        };

        // ── French (stub) ────────────────────────────────────────────────────────
        private static Dictionary<string, string> French => new()
        {
            ["nav.dashboard"]   = "Tableau de bord",
            ["nav.players"]     = "Joueurs",
            ["nav.matches"]     = "Matchs",
            ["nav.teamstats"]   = "Stats équipe",
            ["nav.manageteams"] = "Gérer les équipes",
            ["nav.settings"]    = "Paramètres",
            ["header.startmatch"] = "▶  Démarrer match",
            ["common.save"]     = "Enregistrer",
            ["common.cancel"]   = "Annuler",
            ["common.delete"]   = "Supprimer",
            ["common.edit"]     = "Modifier",
            ["settings.title"]  = "Paramètres",
            ["settings.language"] = "Langue",
            ["settings.restart.msg"]   = "Le changement de langue prendra effet après le redémarrage.\nRedémarrer maintenant ?",
            ["settings.restart.title"] = "Redémarrage requis",
            ["settings.restart.btn"]   = "Redémarrer",
        };

        // ── Spanish (stub) ───────────────────────────────────────────────────────
        private static Dictionary<string, string> Spanish => new()
        {
            ["nav.dashboard"]   = "Panel",
            ["nav.players"]     = "Jugadores",
            ["nav.matches"]     = "Partidos",
            ["nav.teamstats"]   = "Estadísticas",
            ["nav.manageteams"] = "Gestionar equipos",
            ["nav.settings"]    = "Configuración",
            ["header.startmatch"] = "▶  Iniciar partido",
            ["common.save"]     = "Guardar",
            ["common.cancel"]   = "Cancelar",
            ["common.delete"]   = "Eliminar",
            ["common.edit"]     = "Editar",
            ["settings.title"]  = "Configuración",
            ["settings.language"] = "Idioma",
            ["settings.restart.msg"]   = "El cambio de idioma tendrá efecto después de reiniciar.\n¿Reiniciar ahora?",
            ["settings.restart.title"] = "Reinicio requerido",
            ["settings.restart.btn"]   = "Reiniciar ahora",
        };

        // ── Italian (stub) ───────────────────────────────────────────────────────
        private static Dictionary<string, string> Italian => new()
        {
            ["nav.dashboard"]   = "Dashboard",
            ["nav.players"]     = "Giocatori",
            ["nav.matches"]     = "Partite",
            ["nav.teamstats"]   = "Statistiche",
            ["nav.manageteams"] = "Gestisci squadre",
            ["nav.settings"]    = "Impostazioni",
            ["header.startmatch"] = "▶  Inizia partita",
            ["common.save"]     = "Salva",
            ["common.cancel"]   = "Annulla",
            ["common.delete"]   = "Elimina",
            ["common.edit"]     = "Modifica",
            ["settings.title"]  = "Impostazioni",
            ["settings.language"] = "Lingua",
            ["settings.restart.msg"]   = "La modifica della lingua avrà effetto dopo il riavvio.\nRiavviare ora?",
            ["settings.restart.title"] = "Riavvio richiesto",
            ["settings.restart.btn"]   = "Riavvia ora",
        };

        // ── Czech (stub) ─────────────────────────────────────────────────────────
        private static Dictionary<string, string> Czech => new()
        {
            ["nav.dashboard"]   = "Přehled",
            ["nav.players"]     = "Hráči",
            ["nav.matches"]     = "Zápasy",
            ["nav.teamstats"]   = "Statistiky",
            ["nav.manageteams"] = "Správa týmů",
            ["nav.settings"]    = "Nastavení",
            ["header.startmatch"] = "▶  Zahájit zápas",
            ["common.save"]     = "Uložit",
            ["common.cancel"]   = "Zrušit",
            ["common.delete"]   = "Smazat",
            ["common.edit"]     = "Upravit",
            ["settings.title"]  = "Nastavení",
            ["settings.language"] = "Jazyk",
            ["settings.restart.msg"]   = "Změna jazyka se projeví po restartu.\nRestartovat nyní?",
            ["settings.restart.title"] = "Vyžadován restart",
            ["settings.restart.btn"]   = "Restartovat nyní",
        };
    }
}
