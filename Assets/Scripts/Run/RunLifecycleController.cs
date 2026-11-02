using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.Run
{
    public enum RunLifecycleState { EnteringMenu, MenuReady, LoadingGameplay, Initializing, Gameplay, Returning, Error }

    /// <summary>Scene navigation owns only a confirmed immutable profile and a generation, never a retained old run.</summary>
    public sealed class RunLifecycleController : MonoBehaviour
    {
        public const string MenuScene = "Assets/Scenes/MainMenu.unity";
        public const string GameplayScene = "Assets/Scenes/SampleScene.unity";
        public static RunLifecycleController Instance { get; private set; }
        public static bool OwnsStartup => Instance != null;
        public RunLifecycleState State { get; private set; } = RunLifecycleState.EnteringMenu;
        public string Status { get; private set; } = "Preparing menu";
        public long Generation { get; private set; }
        public event Action Changed;
        private MoralePyramidProfileSnapshot confirmedProfile;
        private bool transitioning;
        private RunPhaseController activeRun;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() { Instance = null; }

        public static RunLifecycleController Ensure()
        {
            if (Instance == null) new GameObject("Run Lifecycle").AddComponent<RunLifecycleController>();
            return Instance;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameplayInteractionLock.Acquire(this);
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            GameplayInteractionLock.Release(this);
        }
        private void SetState(RunLifecycleState state, string message)
        {
            State = state; Status = message;
            Changed?.Invoke();
        }
        public bool IsCurrent(long generation) => Generation == generation && State == RunLifecycleState.Gameplay;

        public void EnterMenu()
        {
            if (transitioning || State == RunLifecycleState.MenuReady) return;
            StartCoroutine(PrepareMenu());
        }
        private IEnumerator PrepareMenu()
        {
            transitioning = true;
            SetState(RunLifecycleState.EnteringMenu, "Clearing previous run");
            GameplayInteractionLock.Acquire(this);
            // sceneLoaded can precede old OnDestroy. Do not publish Ready until teardown has finished.
            yield return null;
            Exception failure = null;
            try { RunStateCleanup.ClearStatics(); }
            catch (Exception error) { failure = error; }
            confirmedProfile = null; activeRun = null;
            yield return WaitForReleasedInput();
            transitioning = false;
            SetState(failure == null ? RunLifecycleState.MenuReady : RunLifecycleState.Error,
                failure == null ? "Select a profile, then start" : "Cleanup failed: " + failure.Message);
        }
        public bool TryStart(MoralePyramidProfileData selection, out string error)
        {
            error = null;
            if (State != RunLifecycleState.MenuReady || transitioning) { error = "Menu is not ready"; return false; }
            if (selection == null) { error = "Select a morale profile"; return false; }
            if (!selection.TryCreateSnapshot(out var snapshot, out error)) return false;
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene) || !Application.CanStreamedLevelBeLoaded(MenuScene))
            { error = "MainMenu and SampleScene must be enabled in Build Settings"; return false; }
            confirmedProfile = snapshot;
            Generation++;
            transitioning = true;
            SetState(RunLifecycleState.LoadingGameplay, "Loading new run");
            StartCoroutine(LoadGameplay());
            return true;
        }
        private IEnumerator LoadGameplay()
        {
            yield return WaitForReleasedInput();
            AsyncOperation operation = null;
            string error = null;
            try { operation = SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single); }
            catch (Exception exception) { error = exception.Message; }
            if (operation == null) { FailInMenu(error ?? "Gameplay scene could not load"); yield break; }
            yield return operation;
            yield return null;
            var runs = FindObjectsOfType<RunPhaseController>();
            if (runs.Length != 1) { FailGameplay("Gameplay must contain exactly one RunPhaseController"); yield break; }
            activeRun = runs[0];
            SetState(RunLifecycleState.Initializing, "Initializing new run");
            bool initialized = false;
            try { initialized = activeRun.TryStartRun(confirmedProfile, out error); }
            catch (Exception exception) { error = exception.Message; }
            if (!initialized) { FailGameplay(error ?? "Run initialization failed"); yield break; }
            confirmedProfile = null;
            yield return WaitForReleasedInput();
            transitioning = false;
            GameplayInteractionLock.Release(this);
            SetState(RunLifecycleState.Gameplay, "Run ready");
        }
        // Explicit development entry for pressing Play directly in SampleScene. Menu never calls this.
        public void StartDirectGameplay(RunPhaseController run)
        {
            if (State != RunLifecycleState.EnteringMenu || transitioning) return;
            string error = null;
            if (run == null || run.DefaultMoraleProfile == null || !run.DefaultMoraleProfile.TryCreateSnapshot(out confirmedProfile, out error))
            { FailGameplay(error ?? "Default profile is missing"); return; }
            Generation++; transitioning = true;
            StartCoroutine(InitializeDirect(run));
        }
        private IEnumerator InitializeDirect(RunPhaseController run)
        {
            yield return null;
            activeRun = run;
            SetState(RunLifecycleState.Initializing, "Initializing direct scene test");
            string error = null; bool initialized = false;
            try { initialized = run.TryStartRun(confirmedProfile, out error); }
            catch (Exception exception) { error = exception.Message; }
            if (!initialized) { FailGameplay(error ?? "Run initialization failed"); yield break; }
            confirmedProfile = null;
            yield return WaitForReleasedInput();
            transitioning = false;
            GameplayInteractionLock.Release(this);
            SetState(RunLifecycleState.Gameplay, "Run ready (direct scene test)");
        }
        public bool RequestReturnToMenu()
        {
            if (transitioning || State != RunLifecycleState.Gameplay) return false;
            if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            { Status = "MainMenu is missing from Build Settings"; Changed?.Invoke(); return false; }
            BeginReturn(null);
            return true;
        }
        private void FailGameplay(string error) { BeginReturn(error); }
        private void BeginReturn(string error)
        {
            transitioning = true; Generation++;
            activeRun?.InvalidateRun();
            ValueTransactions.InvalidateRun();
            GameplayInteractionLock.Acquire(this);
            SetState(RunLifecycleState.Returning, error ?? "Returning to menu");
            StartCoroutine(ReturnToMenu(error));
        }
        private IEnumerator ReturnToMenu(string failure)
        {
            // Never reset the transaction bridge inside the submitting operation's call stack.
            yield return null;
            activeRun?.ClearRunState();
            activeRun = null; confirmedProfile = null;
            ValueTransactions.ResetPresentation();
            AsyncOperation operation = null;
            string loadError = null;
            try { operation = SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single); }
            catch (Exception exception) { loadError = exception.Message; }
            if (operation == null)
            { transitioning = false; SetState(RunLifecycleState.Error, loadError ?? "Menu scene could not load"); yield break; }
            yield return operation;
            transitioning = false;
            yield return PrepareMenu();
            if (State == RunLifecycleState.MenuReady && failure != null)
            { Status = "Previous attempt failed: " + failure + ". Select a profile to retry."; Changed?.Invoke(); }
        }
        private void FailInMenu(string error)
        {
            confirmedProfile = null; transitioning = false;
            SetState(RunLifecycleState.MenuReady, error + ". Select a profile to retry.");
        }
        public void RetryMenu()
        {
            if (transitioning || State != RunLifecycleState.Error) return;
            if (SceneManager.GetActiveScene().path == MenuScene) EnterMenu();
            else BeginReturn(null);
        }
        internal static IEnumerator WaitForReleasedInput()
        {
            yield return null;
            while ((Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
                || (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed))
                yield return null;
            yield return null;
        }
    }
}
