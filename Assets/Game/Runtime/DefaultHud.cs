using UnityEngine;

namespace SecondChance
{
    public sealed class DefaultHud : MonoBehaviour
    {
        private GameSession session;
        private UnityEngine.UI.Text stats, center, controls;
        private UnityEngine.UI.Image health, dash, veil;
        private Canvas canvas;
        private Font font;

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
            veil = Image("State overlay", Vector2.zero, Vector2.one, new Color(0,0,0,.65f));
            stats = Text("Status", new Vector2(.025f,.88f), new Vector2(.7f,.98f), 30, TextAnchor.UpperLeft);
            Image("Health background", new Vector2(.025f,.845f), new Vector2(.26f,.865f), new Color(.2f,.2f,.25f));
            health = Image("Health", new Vector2(.025f,.845f), new Vector2(.26f,.865f), new Color(.15f,.9f,.8f));
            Image("Dash background", new Vector2(.025f,.817f), new Vector2(.26f,.83f), new Color(.2f,.2f,.25f));
            dash = Image("Dash", new Vector2(.025f,.817f), new Vector2(.26f,.83f), new Color(.8f,.85f,1));
            center = Text("State", new Vector2(.13f,.3f), new Vector2(.87f,.72f), 44, TextAnchor.MiddleCenter);
            controls = Text("Controls", new Vector2(.025f,.02f), new Vector2(.98f,.10f), 24, TextAnchor.LowerCenter);
            Refresh(false);
        }

        public void Refresh(bool paused)
        {
            if (session == null || canvas == null) return;
            var player = session.Player;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, session.Settings.RunDuration - session.Elapsed));
            stats.text = $"ВТОРОЙ ШАНС   /   {seconds / 60:00}:{seconds % 60:00}\nВОЛНА {session.Wave}   |   УБИТО {session.Kills}   |   ПЕРЕСЕЛЕНИЙ {session.Transfers}";
            if(session.Settings.UseRooms)
            {
                int elapsed=Mathf.FloorToInt(session.Elapsed);
                stats.text=$"{session.Layout.Name}   {session.Room+1}/{session.Settings.RoomCount}   /   {elapsed/60:00}:{elapsed%60:00}\nВОЛНА {session.WavesThisRoom}/{session.Settings.WavesPerRoom}   |   ПЕРЕСЕЛЕНИЙ {session.Transfers}";
            }
            if (player != null)
            {
                stats.text += $"\n{BodyName(player.Definition.Kind)}   {Mathf.CeilToInt(player.Health)} / {player.Definition.Health:0}";
                SetBar(health, Mathf.Clamp01(player.Health / player.Definition.Health), .845f, .865f);
            }
            SetBar(dash, session.DashReady, .817f, .83f);
            controls.text = "WASD / стрелки — движение     ЛКМ — атака     ПРОБЕЛ / SHIFT — рывок\nE — переселение / выход     ESC — пауза";
            veil.gameObject.SetActive(paused || session.Phase == RunPhase.Ready || session.Phase == RunPhase.Won || session.Phase == RunPhase.Lost);
            center.color = Color.white;
            var centerRect = center.rectTransform;
            bool choosing = !paused && session.Phase == RunPhase.Possessing;
            centerRect.anchorMin = choosing ? new Vector2(.15f,.11f) : new Vector2(.13f,.3f);
            centerRect.anchorMax = choosing ? new Vector2(.85f,.31f) : new Vector2(.87f,.72f);
            center.fontSize = choosing ? 32 : session.Phase == RunPhase.Ready ? 36 : 42;
            if (paused) center.text = "ПАУЗА\nESC — продолжить";
            else switch (session.Phase)
            {
                case RunPhase.Ready:
                    center.text = "ВТОРОЙ ШАНС\n\nРанение оставляет на враге временную метку.\nПосле смерти у тебя 2 секунды: нажми E,\nчтобы захватить отмеченного врага поблизости.\nТы получишь его оружие и ОСТАВШЕЕСЯ здоровье.\n"+(session.Settings.UseRooms ? "Зачисти локацию и нажми E у голубого выхода.\nВрагов видно только в конусе обзора к курсору.\n" : "")+"\nENTER — начать"; break;
                case RunPhase.Possessing:
                    center.color = new Color(1,.4f,.75f);
                    center.text = $"ВТОРОЙ ШАНС   {session.PossessionRemaining:0.0} с\n" + (session.Candidate != null ? $"E — ЗАХВАТИТЬ: {BodyName(session.Candidate.Definition.Kind)}\nЗдоровье: {session.Candidate.Health:0}" : "РЯДОМ НЕТ ДОСТУПНОГО ТЕЛА С МЕТКОЙ"); break;
                case RunPhase.Intermission: center.text = session.AwaitingExit ? "ЛОКАЦИЯ ЗАЧИЩЕНА\nПодойди к голубому выходу и нажми E" : "ВОЛНА ОТБИТА\nПереведи дух"; break;
                case RunPhase.Won: center.text = "ТЫ ВЫЖИЛ\nСущность продолжает жить.\n\nR / ENTER — сыграть снова"; break;
                case RunPhase.Lost: center.text = "ВТОРОГО ШАНСА НЕТ\nПоследнее тело потеряно.\n\nR / ENTER — повторить"; break;
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
            text.raycastTarget = false;
            return text;
        }
        private UnityEngine.UI.Image Image(string name, Vector2 minimum, Vector2 maximum, Color color)
        { var image = Rect(name, minimum, maximum).gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image; }
        private RectTransform Rect(string name, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false); rect.anchorMin = minimum; rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
