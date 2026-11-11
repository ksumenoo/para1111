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
            stats.text = $"SECOND CHANCE   /   {seconds / 60:00}:{seconds % 60:00}\nWAVE {session.Wave}   |   KILLS {session.Kills}   |   TRANSFERS {session.Transfers}";
            if(session.Settings.UseRooms)
            {
                int elapsed=Mathf.FloorToInt(session.Elapsed);
                stats.text=$"SECOND CHANCE   /   {elapsed/60:00}:{elapsed%60:00}\nROOM {session.Room+1}/{session.Settings.RoomCount}   |   WAVE {session.WavesThisRoom}/{session.Settings.WavesPerRoom}   |   TRANSFERS {session.Transfers}";
            }
            if (player != null)
            {
                stats.text += $"\n{player.Definition.Label}   {Mathf.CeilToInt(player.Health)} / {player.Definition.Health:0}";
                SetBar(health, Mathf.Clamp01(player.Health / player.Definition.Health), .845f, .865f);
            }
            SetBar(dash, session.DashReady, .817f, .83f);
            controls.text = "WASD / ARROWS  move     LMB  attack     SPACE / SHIFT  dash     E  possess     ESC  pause";
            veil.gameObject.SetActive(paused || session.Phase == RunPhase.Ready || session.Phase == RunPhase.Won || session.Phase == RunPhase.Lost);
            center.color = Color.white;
            var centerRect = center.rectTransform;
            bool choosing = !paused && session.Phase == RunPhase.Possessing;
            centerRect.anchorMin = choosing ? new Vector2(.15f,.11f) : new Vector2(.13f,.3f);
            centerRect.anchorMax = choosing ? new Vector2(.85f,.31f) : new Vector2(.87f,.72f);
            center.fontSize = choosing ? 34 : 44;
            if (paused) center.text = "PAUSED\nESC — continue";
            else switch (session.Phase)
            {
                case RunPhase.Ready:
                    center.text = "SECOND CHANCE\n\nWound an enemy to mark its body.\nWhen you die: 2 seconds to press E and possess it.\nYou inherit its weapon and REMAINING health.\n"+(session.Settings.UseRooms ? "Clear each room, then press E at its exit.\nEnemies are visible only in your sight cone.\n" : "")+"\nENTER — begin"; break;
                case RunPhase.Possessing:
                    center.color = new Color(1,.4f,.75f);
                    center.text = $"SECOND CHANCE   {session.PossessionRemaining:0.0}s\n" + (session.Candidate != null ? $"E — POSSESS {session.Candidate.Definition.Label}\n{session.Candidate.Health:0} HP" : "NO VISIBLE MARKED BODY IN RANGE"); break;
                case RunPhase.Intermission: center.text = session.AwaitingExit ? "ROOM CLEAR\nGo to the cyan exit — E" : "WAVE CLEARED\nCatch your breath"; break;
                case RunPhase.Won: center.text = "YOU SURVIVED\nThe soul lives on.\n\nR / ENTER — play again"; break;
                case RunPhase.Lost: center.text = "NO SECOND CHANCE\nThe last body is gone.\n\nR / ENTER — retry"; break;
                default: center.text = ""; break;
            }
        }

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
