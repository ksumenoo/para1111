using UnityEngine;

namespace SecondChance
{
    public sealed partial class DefaultHud : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1,.8f,.2f);
        private static readonly Color Pink = new Color(1,.4f,.75f);
        private static readonly Color BossColor = new Color(1,.1f,.55f);
        private GameSession session;
        private UnityEngine.UI.Text stats, center, controls, surge, hosts, bossLabel;
        private UnityEngine.UI.Image health, dash, veil, flash, bossBack, bossBar;
        private Canvas canvas;
        private Font font;
        private Color flashColor;
        private float flashAmount;

        public void Initialize(GameSession game)
        {
            session = game;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas = new GameObject("Game HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            InitializeResponsive();
            flash = Image("Event flash", Vector2.zero, Vector2.one, Color.clear);
            veil = Image("State overlay", Vector2.zero, Vector2.one, new Color(0,0,0,.65f));
            stats = Text("Status", new Vector2(.025f,.88f), new Vector2(.7f,.98f), 30, TextAnchor.UpperLeft);
            Image("Health background", new Vector2(.025f,.845f), new Vector2(.26f,.865f), new Color(.2f,.2f,.25f));
            health = Image("Health", new Vector2(.025f,.845f), new Vector2(.26f,.865f), new Color(.15f,.9f,.8f));
            Image("Dash background", new Vector2(.025f,.817f), new Vector2(.26f,.83f), new Color(.2f,.2f,.25f));
            dash = Image("Dash", new Vector2(.025f,.817f), new Vector2(.26f,.83f), new Color(.8f,.85f,1));
            surge = Text("Chain bonus", new Vector2(.025f,.765f), new Vector2(.6f,.81f), 28, TextAnchor.MiddleLeft);
            hosts = Text("Hosts nearby", new Vector2(.025f,.725f), new Vector2(.6f,.765f), 22, TextAnchor.MiddleLeft);
            // Right of the status block, so the two never overlap on narrow windows.
            bossLabel = Text("Boss name", new Vector2(.45f,.945f), new Vector2(.95f,.99f), 28, TextAnchor.MiddleCenter);
            bossBack = Image("Boss health background", new Vector2(.5f,.925f), new Vector2(.9f,.943f), new Color(.2f,.2f,.25f));
            bossBar = Image("Boss health", new Vector2(.5f,.925f), new Vector2(.9f,.943f), BossColor);
            center = Text("State", new Vector2(.13f,.3f), new Vector2(.87f,.72f), 44, TextAnchor.MiddleCenter);
            controls = Text("Controls", new Vector2(.025f,.02f), new Vector2(.98f,.10f), 24, TextAnchor.LowerCenter);
            InitializeExitGuide();
            session.Combat += OnCombat;
            Refresh(false);
        }

        // Brief full-screen tint: possession, chain bonus, damage taken, boss changing host.
        private void OnCombat(CombatEvent message)
        {
            switch (message.Cue)
            {
                case CombatCue.Possessed: SetFlash(new Color(.2f,1,1), .4f); break;
                case CombatCue.Surge: SetFlash(Gold, .3f); break;
                case CombatCue.BossShift: SetFlash(BossColor, .3f); break;
                case CombatCue.Hit: if (message.Body.IsPlayer) SetFlash(new Color(1,.1f,.15f), .22f); break;
            }
        }

        private void SetFlash(Color color, float amount) { flashColor = color; flashAmount = Mathf.Max(flashAmount, amount); }

        public void Refresh(bool paused)
        {
            if (session == null || canvas == null) return;
            var player = session.Player;
            var phase = session.Phase;
            flashAmount = Mathf.MoveTowards(flashAmount, 0, Time.unscaledDeltaTime * 1.4f);
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAmount);
            RefreshResponsive();
            RefreshExitGuide(paused);
            if(player!=null)SetBar(health,Mathf.Clamp01(player.Health/Mathf.Max(1,player.MaxHealth)),.845f,.865f);
            SetBar(dash,session.DashReady,.817f,.83f);
            // Text changes at 12 Hz; immediate on damage, pause or a phase transition.
            if(!ShouldRefreshText(paused,phase,player))return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, session.Settings.RunDuration - session.Elapsed));
            stats.text = $"ВТОРОЙ ШАНС   /   {seconds / 60:00}:{seconds % 60:00}\nВОЛНА {session.Wave}   |   УБИТО {session.Kills}   |   ПЕРЕСЕЛЕНИЙ {session.Transfers}";
            if(session.Settings.UseRooms)
            {
                int elapsed=Mathf.FloorToInt(session.Elapsed);
                string progress=session.BossFight ? $"БАРОН: тело {session.BossStage}/{session.Settings.BossLives}" : $"ВОЛНА {session.WavesThisRoom}/{session.Settings.WavesPerRoom}";
                stats.text=$"{session.Layout.Name}   {session.Room+1}/{session.Settings.RoomCount}   /   {elapsed/60:00}:{elapsed%60:00}\n{progress}   |   ПЕРЕСЕЛЕНИЙ {session.Transfers}";
            }
            if (player != null)
            {
                stats.text += $"\n{BodyName(player.Definition.Kind)}   {Mathf.CeilToInt(player.Health)} / {player.MaxHealth:0}";
                SetBar(health, Mathf.Clamp01(player.Health / Mathf.Max(1,player.MaxHealth)), .845f, .865f);
            }
            SetBar(dash, session.DashReady, .817f, .83f);

            // Possession chain: first the window to earn it, then the bonus itself.
            bool fighting = !paused && phase == RunPhase.Fighting;
            if (fighting && session.Surging)
            { surge.color = Gold; surge.text = $"РАЗГОН   урон x{session.Settings.SurgeDamage:0.#}   {session.SurgeRemaining:0.0} с"; }
            else if (fighting && session.ChainRemaining > 0)
            { surge.color = new Color(.3f,1,1); surge.text = $"ЦЕПОЧКА: убей за {session.ChainRemaining:0.0} с"; }
            else surge.text = "";

            // How many second chances are standing nearby right now.
            if (fighting && player != null)
            {
                int ready = session.HostsReady;
                bool wounded = player.Health <= player.MaxHealth * .35f;
                hosts.color = ready > 0 ? Pink : wounded ? new Color(1,.25f,.2f) : new Color(.6f,.65f,.7f);
                hosts.text = ready > 0 ? $"НОСИТЕЛЕЙ РЯДОМ: {ready}" : wounded ? "НЕТ НОСИТЕЛЯ — ПОМЕТЬ ВРАГА УДАРОМ" : "носителей рядом нет";
            }
            else hosts.text = "";

            var boss = session.Boss;
            bool showBoss = session.BossFight && boss != null && phase != RunPhase.Won && phase != RunPhase.Lost;
            bossLabel.gameObject.SetActive(showBoss); bossBack.gameObject.SetActive(showBoss); bossBar.gameObject.SetActive(showBoss);
            if (showBoss)
            {
                bossLabel.color = BossColor;
                bossLabel.text = $"БАРОН   в теле: {BodyName(boss.Definition.Kind)}   {session.BossStage}/{session.Settings.BossLives}";
                bossBar.rectTransform.anchorMax = new Vector2(.5f + .4f * Mathf.Clamp01(boss.Health / Mathf.Max(1,boss.MaxHealth)), bossBarTop);
            }

            controls.text = "WASD / стрелки — движение     ЛКМ — атака     ПРОБЕЛ / SHIFT — рывок\nE — переселение / выход     ESC — пауза";
            veil.gameObject.SetActive(paused || phase == RunPhase.Ready || phase == RunPhase.Won || phase == RunPhase.Lost);
            center.color = Color.white;
            var centerRect = center.rectTransform;
            bool choosing = !paused && phase == RunPhase.Possessing;
            bool exiting = !paused && session.AwaitingExit;
            centerRect.anchorMin = choosing ? new Vector2(.1f,.11f) : exiting ? new Vector2(.15f,.13f) : new Vector2(.1f,.2f);
            centerRect.anchorMax = choosing ? new Vector2(.9f,.31f) : exiting ? new Vector2(.85f,.25f) : new Vector2(.9f,.72f);
            center.fontSize = center.resizeTextMaxSize = choosing ? 32 : exiting ? 28 : phase == RunPhase.Ready ? 32 : 42;
            int played = Mathf.FloorToInt(session.Elapsed);
            string summary = $"Время {played / 60:00}:{played % 60:00}   |   Убито {session.Kills}   |   Переселений {session.Transfers}";
            if (paused) center.text = "ПАУЗА\nESC — продолжить\nQ — выйти из игры";
            else switch (phase)
            {
                case RunPhase.Ready:
                    center.text = "ВТОРОЙ ШАНС\n\nРанение оставляет на враге временную метку.\nПосле смерти у тебя 2 секунды: нажми E,\nчтобы захватить отмеченного врага поблизости.\nТы получишь его оружие и ОСТАВШЕЕСЯ здоровье.\nУбей кого-нибудь сразу после переселения — получишь РАЗГОН.\nЯщики ломаются, красные баллоны взрываются и метят врагов.\n"+(session.Settings.UseRooms ? "Зачисти локацию и нажми E у голубого выхода.\nВрагов видно только в конусе обзора к курсору.\n" : "")+"\nENTER — начать"; break;
                case RunPhase.Possessing:
                    center.color = Pink;
                    center.text = $"ВТОРОЙ ШАНС   {session.PossessionRemaining:0.0} с\n" + (session.Candidate != null ? $"E — ЗАХВАТИТЬ: {(session.Candidate.IsBoss ? "БАРОН, " : "")}{BodyName(session.Candidate.Definition.Kind)}\nЗдоровье: {session.Candidate.Health:0}" : "РЯДОМ НЕТ ДОСТУПНОГО ТЕЛА С МЕТКОЙ"); break;
                case RunPhase.Intermission:
                    center.text = session.BossPending ? "ВОЛНЫ ОТБИТЫ\nБарон идёт сам. Его придётся убить трижды — в трёх телах" : session.AwaitingExit ? "ЛОКАЦИЯ ЗАЧИЩЕНА\nПодойди к голубому выходу и нажми E" : "ВОЛНА ОТБИТА\nПереведи дух"; break;
                case RunPhase.Won: center.color = new Color(.3f,1,1); center.text = "ТЫ ВЫЖИЛ\nСущность продолжает жить.\n\n" + summary + "\n\nR / ENTER — сыграть снова"; break;
                case RunPhase.Lost: center.color = Pink; center.text = "ВТОРОГО ШАНСА НЕТ\nПоследнее тело потеряно.\n\n" + summary + "\n\nR / ENTER — повторить"; break;
                default: center.text = ""; break;
            }
        }

        private static string BodyName(BodyKind kind) => kind == BodyKind.Brute ? "ГРОМИЛА" : kind == BodyKind.Gunner ? "СТРЕЛОК" : "ДУЭЛЯНТ";

        private void SetBar(UnityEngine.UI.Image image, float fraction, float minY, float maxY)
        { image.rectTransform.anchorMax = new Vector2(.025f + .235f * fraction, maxY); image.rectTransform.anchorMin = new Vector2(.025f,minY); }

        private UnityEngine.UI.Text Text(string name, Vector2 minimum, Vector2 maximum, int size, TextAnchor alignment)
        {
            var text = Rect(name, minimum, maximum).gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = font; text.fontSize = size; text.alignment = alignment; text.color = Color.white;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=size;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
        private UnityEngine.UI.Image Image(string name, Vector2 minimum, Vector2 maximum, Color color)
        { var image = Rect(name, minimum, maximum).gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image; }
        private RectTransform Rect(string name, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(safeRoot, false); rect.anchorMin = minimum; rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private void OnDestroy() { if (session != null) session.Combat -= OnCombat; }
    }
}
