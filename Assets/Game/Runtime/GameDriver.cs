using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace SecondChance
{
    public sealed class GameDriver : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool showDefaultHud = true;
        private readonly Dictionary<int, BodyView> views = new Dictionary<int, BodyView>();
        private readonly List<int> removedViews = new List<int>();
        private readonly List<GameObject> shotViews = new List<GameObject>();
        private GameSession session;
        private GameSettings settings;
        private Transform actorsRoot;
        private DefaultHud hud;
        private CombatFeedback feedback;
        private PossessionView possessionView;
        private Material lineMaterial;
        private readonly Material[] bodyMaterials = new Material[3];
        private bool paused;
        public GameSession Session => session;

        [Inject] public void Construct(GameSession game, GameSettings configuration) { session = game; settings = configuration; }

        private void Start()
        {
            if (gameCamera == null) gameCamera = Camera.main;
            if (gameCamera == null) { Debug.LogError("GameDriver needs a camera.", this); enabled = false; return; }
            actorsRoot = new GameObject("Runtime Bodies").transform;
            actorsRoot.SetParent(transform, false);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            for (int i = 0; i < 3; i++) bodyMaterials[i] = new Material(shader) { color = settings.GetBody((BodyKind)i).Color };
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            feedback = GetComponent<CombatFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<CombatFeedback>();
            feedback.Initialize(gameCamera, lineMaterial);
            possessionView = gameObject.AddComponent<PossessionView>();
            possessionView.Initialize(session, lineMaterial);
            if (showDefaultHud) { hud = gameObject.AddComponent<DefaultHud>(); hud.Initialize(session); }
            session.Spawned += SpawnView;
            session.Combat += OnCombat;
        }

        private void Update()
        {
            if (session == null || actorsRoot == null) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && session.Phase != RunPhase.Ready) paused = !paused;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame)
                && (session.Phase == RunPhase.Ready || session.Phase == RunPhase.Lost || session.Phase == RunPhase.Won))
                Restart();
            if (!paused)
            {
                var command = ReadCommand();
                // Substeps keep collision and telegraph timing stable on slow machines.
                bool handledDeathWindow = session.Phase == RunPhase.Possessing;
                if (handledDeathWindow)
                {
                    // Never cap/drop real time from the two-second death window.
                    session.Tick(Time.unscaledDeltaTime, command);
                    command.Dash = false; command.Possess = false;
                }
                float remaining = handledDeathWindow ? 0 : Mathf.Min(Time.unscaledDeltaTime, .25f);
                while (remaining > 0)
                {
                    float step = Mathf.Min(remaining, 1f / 60);
                    session.Tick(step, command);
                    command.Dash = false; command.Possess = false;
                    remaining -= step;
                }
            }
            foreach (var pair in views) pair.Value.Refresh(session);
            removedViews.Clear();
            foreach (var pair in views) if (!pair.Value.State.Alive) removedViews.Add(pair.Key);
            foreach (var id in removedViews) { Destroy(views[id].gameObject); views.Remove(id); }
            RefreshShots();
            possessionView.Refresh(paused);
            if (hud != null) hud.Refresh(paused);
        }

        public void Restart()
        {
            paused = false;
            if (possessionView != null) possessionView.ResetVisuals();
            foreach (var view in views.Values) if (view != null) Destroy(view.gameObject);
            views.Clear();
            foreach (var shot in shotViews) Destroy(shot);
            shotViews.Clear();
            session.StartRun();
        }

        private PlayerCommand ReadCommand()
        {
            var result = new PlayerCommand();
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                result.Move = new Vector3((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0), 0,
                    (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
                result.Dash = keyboard.spaceKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame;
                result.Possess = keyboard.eKey.wasPressedThisFrame;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                result.Attack = mouse.leftButton.isPressed;
                var ray = gameCamera.ScreenPointToRay(mouse.position.ReadValue());
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float distance)) result.Aim = ray.GetPoint(distance);
            }
            else if (session.Player != null) result.Aim = session.Player.Position + session.Player.Facing;
            return result;
        }

        private void SpawnView(BodyState body)
        {
            var instance = new GameObject(body.IsPlayer ? "Player Body" : body.Definition.Label);
            instance.transform.SetParent(actorsRoot, false);
            var view = instance.AddComponent<BodyView>();
            view.Initialize(body, bodyMaterials[(int)body.Definition.Kind], lineMaterial);
            views.Add(body.Id, view);
        }

        private void OnCombat(CombatEvent message)
        {
            if (views.TryGetValue(message.Body.Id, out var view)) view.React(message);
            feedback.Play(message);
        }

        private void RefreshShots()
        {
            while (shotViews.Count < session.Shots.Count)
            {
                var view = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(view.GetComponent<Collider>());
                view.name = "Projectile";
                view.transform.SetParent(actorsRoot, false);
                view.transform.localScale = new Vector3(.2f, .2f, .5f);
                view.GetComponent<Renderer>().sharedMaterial = bodyMaterials[2];
                shotViews.Add(view);
            }
            for (int i = 0; i < shotViews.Count; i++)
            {
                bool active = i < session.Shots.Count;
                shotViews[i].SetActive(active);
                if (active) { shotViews[i].transform.position = session.Shots[i].Position + Vector3.up * .8f; shotViews[i].transform.rotation = Quaternion.LookRotation(session.Shots[i].Direction); }
            }
        }

        private void OnDestroy()
        {
            if (session != null) { session.Spawned -= SpawnView; session.Combat -= OnCombat; }
            foreach (var material in bodyMaterials) if (material != null) Destroy(material);
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
