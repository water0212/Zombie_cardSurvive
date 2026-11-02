#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;
using ZombieCardSurvive.UI.Morale;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.UI.Settlement.Editor
{
    /// <summary>Editor-only scripted Play checks. Uses the scene's real hand, buttons, events and round controller.</summary>
    public static class SampleE1E2PlayProbe
    {
        [Serializable] public sealed class Report
        {
            public string scene;
            public string status;
            public string failure;
            public List<string> checks = new List<string>();
        }
        private static Report report;
        private static SettlementPresenter host;
        private static Coroutine coroutine;
        private static EditorApplication.CallbackFunction drive;
        private static bool previousBackground;
        private static double deadline;

        [MenuItem("Tools/QA/E2/Sample integration/3 Run Play integration checks")]
        public static void StartChecks()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode in the integration test scene first.");
            if (report != null && report.status == "running") throw new InvalidOperationException("Probe already running.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != SampleE1E2IntegrationSetupUtility.TestScenePath && scene.path != SampleE1E2IntegrationSetupUtility.MainScenePath)
                throw new InvalidOperationException("This probe only runs in Sample or its test copy.");
            SampleE1E2IntegrationSetupUtility.ThrowIfInvalid(scene);
            host = SampleE1E2IntegrationSetupUtility.InScene<SettlementPresenter>(scene).Single();
            report = new Report { scene = scene.path, status = "running" };
            SessionState.SetString(SampleE1E2IntegrationSetupUtility.PassedSmokeHashKey, "");
            SaveProgress();
            previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            deadline = EditorApplication.timeSinceStartup + 120;
            drive = DriveEditor;
            EditorApplication.update += drive;
            coroutine = host.StartCoroutine(RunSafely(CheckFlow()));
        }
        public static string ReadReport() => SessionState.GetString(SampleE1E2IntegrationSetupUtility.ProbeResultKey, "not started");
        private static void DriveEditor()
        {
            if (report == null || report.status != "running") { StopDriving(); return; }
            if (!Application.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                if (host != null && coroutine != null) host.StopCoroutine(coroutine);
                Finish(false, "Play stopped or probe timed out."); return;
            }
            // Drive frames for a background Editor. Never take screenshots in this update loop.
            EditorApplication.QueuePlayerLoopUpdate();
        }
        private static IEnumerator RunSafely(IEnumerator checks)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(checks);
            while (stack.Count > 0)
            {
                bool next = false; object current = null; Exception failure = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null)
                {
                    while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                    Finish(false, failure.ToString()); yield break;
                }
                if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
            Finish(true, "");
        }
        private static IEnumerator CheckFlow()
        {
            yield return null;
            var scene = SceneManager.GetActiveScene();
            var run = SampleE1E2IntegrationSetupUtility.InScene<RunPhaseController>(scene).Single();
            var cards = SampleE1E2IntegrationSetupUtility.InScene<CardController>(scene).Single();
            var hand = SampleE1E2IntegrationSetupUtility.InScene<CardHandView>(scene).Single();
            var flow = SampleE1E2IntegrationSetupUtility.InScene<EventFlowController>(scene).Single();
            var hud = SampleE1E2IntegrationSetupUtility.InScene<GameplayHudRuntimePresenter>(scene).Single();
            var e1 = SampleE1E2IntegrationSetupUtility.InScene<MoraleHudPresenter>(scene).Single();
            var debug = SampleE1E2IntegrationSetupUtility.InScene<ResourceDebugView>(scene).Single();
            bool reduced = new SerializedObject(host).FindProperty("reducedMotion").boolValue;
            host.SetReducedMotion(true);
            try
            {
                yield return WaitUntil(() => !host.IsPresenting, "initial presentation");
                Require(!run.IsRunEnded, "Run initialization failed.");
                Record("Unique main controllers, real CardHandView, E1/E2 references and no P0 demo initializer.");

                Require(flow.State == EventFlowState.Choosing || flow.State == EventFlowState.Result, "Expected the first real event.");
                if (flow.State == EventFlowState.Choosing)
                {
                    int option = ChooseAvailable(flow);
                    Require(flow.ChooseOption(option), "First event choice was rejected.");
                    yield return WaitUntil(() => !host.IsPresenting, "first event values");
                }
                Require(flow.State == EventFlowState.Result, "Event did not reach Result.");
                flow.EndEvent();
                yield return WaitUntil(() => run.CurrentPhase == RunPhase.Turn && !host.IsPresenting, "event-to-turn handoff");
                Require(!run.IsRunEnded && !GameplayInteractionLock.IsLocked, "Gameplay stayed locked after the event.");
                Record("Real event choice/result closes into the playable Turn without bypassing event flow.");

                var runtime = cards.HandCards.FirstOrDefault(card => card.Data != null
                    && card.Data.EffectivePlayMode == CardPlayMode.Playable
                    && EnergySystem.CanSpend(card.EffectiveEnergyCost)
                    && card.Data.CanPayCosts(new CardPlayContext(cards, card))
                    && card.Data.Effects.All(effect => effect == null || effect.Category == GameEffectCategory.Value));
                Require(runtime != null, "No affordable simple card was drawn for the smoke test.");
                var cardView = UnityEngine.Object.FindObjectsOfType<CardView>().FirstOrDefault(view => view.RuntimeCard == runtime);
                Require(cardView != null, "Real hand did not spawn a CardView.");
                var before = ValueSnapshot.Capture();
                int notifications = 0; ValueTransactionResult played = null;
                Action<ValueTransactionResult> capture = result => { if (result.Kind == ValueTransactionKind.Card) { notifications++; played = result; } };
                ValueTransactions.TransactionCompleted += capture;
                try
                {
                    Require(hand.RequestPlayCard(cardView), "CardHandView could not play the drawn card.");
                    Require(notifications == 1 && played != null, "One card must complete exactly one transaction.");
                    string expectedFoodFrame = hud.GetResourceText("Food").text;
                    debug.Refresh();
                    Require(hud.GetResourceText("Food").text == expectedFoodFrame, "Legacy polling overwrote presentation before its first frame.");
                    bool blockingCard = ValuePresentationPolicy.RequiresBlocking(played);
                    Require(host.IsBlockingPresentation == blockingCard, "Card presentation used the wrong route.");
                    Require(GameplayInteractionLock.IsLocked == blockingCard, "Only a stage-changing/fatal card may lock normal gameplay.");
                    yield return WaitUntil(() => !host.IsPresenting, "card value presentation");
                    foreach (var id in new[] { ValueId.Food, ValueId.Resource, ValueId.Energy, ValueId.Combat })
                        Require(played.Delta(id) == (long)ValueSnapshot.Capture().Get(id) - before.Get(id), "Card net result mismatch: " + id);
                    Require(hud.GetResourceText("Energy").text == EnergySystem.CurrentEnergy + "/" + EnergySystem.MaxEnergy, "Energy UI did not reach the committed value.");
                    Require(hud.GetResourceText("Food").text.StartsWith(FoodSystem.Food + "/"), "Food UI did not reach the committed value.");
                    Require(!GameplayInteractionLock.IsLocked, "Card presentation left input locked.");
                    Record("Real CardView -> CardHandView -> CardController; one transaction, matching net deltas and final numeric UI.");
                    Record("Legacy polling cannot overwrite numeric animation; ordinary cards stay interactive, stage changes use the blocking route.");
                }
                finally { ValueTransactions.TransactionCompleted -= capture; }

                int turns = 0;
                while (!cards.IsDrawPileEmpty && turns++ < 20)
                {
                    Require(run.CurrentPhase == RunPhase.Turn && !run.IsRunEnded, "Run left Turn before the last draw.");
                    run.EndTurn();
                    yield return WaitUntil(() => !host.IsPresenting, "next turn draw/production");
                    yield return null;
                }
                Require(cards.IsDrawPileEmpty, "Could not reach the final turn within 20 turns.");
                Require(MoraleSystem.CollapseStage == MoraleCollapseStage.Stable, "The fixture already lost a stage before the round warning check.");
                using (var transaction = new ValueTransaction("QA:RoundWarningFixture", ValueTransactionKind.Reset))
                {
                    transaction.Execute(() => { FoodSystem.Reset(0); MoraleSystem.CommitChange("QA:Start72", 72 - MoraleSystem.CurrentMorale); });
                    transaction.Complete();
                }
                int moraleBefore = MoraleSystem.CurrentMorale;
                var e1Serialized = new SerializedObject(e1);
                var confirmation = Read<GameObject>(e1Serialized, "confirmationRoot");
                run.EndTurn();
                Require(run.IsRoundEndConfirmationPending && confirmation.activeSelf, "E1 danger confirmation did not open.");
                Read<Button>(e1Serialized, "returnButton").onClick.Invoke();
                Require(!run.IsRoundEndConfirmationPending && !confirmation.activeSelf && run.CurrentPhase == RunPhase.Turn,
                    "Returning from E1 confirmation did not preserve the Turn.");
                Require(MoraleSystem.CurrentMorale == moraleBefore && FoodSystem.Food == 0, "Canceling the warning mutated gameplay values.");
                Record("E1 warning opens through EndTurn; real Return button cancels without applying settlement.");

                run.EndTurn();
                Read<Button>(e1Serialized, "continueButton").onClick.Invoke();
                Require(run.IsSettlementPending && run.CurrentPhase == RunPhase.RoundEnd, "Confirmed round did not defer its exit.");
                Require(GameplayInteractionLock.IsLocked, "Round presentation did not keep input locked.");
                run.EnterTurnPhase();
                Require(run.CurrentPhase == RunPhase.RoundEnd, "Public turn entry bypassed pending settlement.");
                yield return WaitUntil(() => !host.IsPresenting, "round settlement presentation");
                Require(run.CurrentPhase == RunPhase.Event && !run.IsSettlementPending, "Round did not exit once into Event.");
                Require(!GameplayInteractionLock.IsLocked, "Next event stayed locked after settlement.");
                Require(flow.State == EventFlowState.Choosing || flow.State == EventFlowState.Result, "Next real event was not opened.");
                Record("E1 Continue button -> real RoundEndResolver -> E2 lock/animation -> one next-event handoff.");

                var fatal = new MoraleBatchRequest("QA:IntegrationCollapse");
                fatal.Add("FoodShortage", -100); fatal.Add("ZombieLeak", -10, -5);
                MoraleSystem.CommitBatch(fatal);
                Require(run.IsRunEnded && run.CurrentPhase == RunPhase.Failed && GameplayInteractionLock.IsLocked, "Failed guard was delayed until presentation.");
                run.EnterEventPhase(); run.EnterTurnPhase(); run.EndTurn(); flow.EndEvent();
                Require(run.CurrentPhase == RunPhase.Failed, "Gameplay advanced after collapse.");
                yield return WaitUntil(() => !host.IsPresenting, "collapse report presentation");
                var collapse = SampleE1E2IntegrationSetupUtility.InScene<CollapseReportView>(scene).Single();
                Require(collapse.IsVisible, "Collapse report did not take over.");
                string reportText = Read<TMP_Text>(new SerializedObject(collapse), "reportText").text;
                Require(reportText.Contains("食物短缺") && reportText.Contains("殭屍漏防"), "Collapse report lost a source.");
                Require(GameplayInteractionLock.IsLocked, "Failed run released gameplay lock.");
                Record("Immediate Failed, no further phase advancement, complete two-source collapse report and persistent failure lock.");
            }
            finally { if (host != null) host.SetReducedMotion(reduced); }
        }
        private static int ChooseAvailable(EventFlowController flow)
        {
            var choices = flow.CurrentResolvedEvent.Choices;
            int fallback = -1;
            for (int i = 0; i < choices.Count; i++)
            {
                if (!flow.GetChoiceAvailability(i).IsAvailable) continue;
                if (fallback < 0) fallback = i;
                if (choices[i].Effects.All(effect => effect?.Source != null && effect.Source.Category == GameEffectCategory.Value)) return i;
            }
            if (fallback < 0) throw new InvalidOperationException("No available event option.");
            return fallback;
        }
        private static IEnumerator WaitUntil(Func<bool> ready, string operation)
        {
            float end = Time.realtimeSinceStartup + 20;
            do
            {
                yield return null;
                if (ready()) yield break;
            } while (Time.realtimeSinceStartup < end);
            throw new InvalidOperationException("Timed out waiting for " + operation);
        }
        private static T Read<T>(SerializedObject source, string name) where T : UnityEngine.Object
            => source.FindProperty(name).objectReferenceValue as T;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Record(string message) { report.checks.Add(message); SaveProgress(); }
        private static void SaveProgress() => SessionState.SetString(SampleE1E2IntegrationSetupUtility.ProbeResultKey, JsonUtility.ToJson(report));
        private static void Finish(bool success, string failure)
        {
            if (report == null || report.status != "running") return;
            report.status = success ? "passed" : "failed"; report.failure = failure; SaveProgress();
            if (success) SessionState.SetString(SampleE1E2IntegrationSetupUtility.PassedSmokeHashKey, SampleE1E2IntegrationSetupUtility.FileHash(report.scene));
            StopDriving();
        }
        private static void StopDriving()
        {
            if (drive != null) EditorApplication.update -= drive;
            drive = null;
            Application.runInBackground = previousBackground;
        }
    }
}
#endif
