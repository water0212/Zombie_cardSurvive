using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;
using ZombieCardSurvive.UI.Morale;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.UI.Settlement
{
    public sealed class SettlementPresenter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float InTurnfadeDuration = .75f;

        [SerializeField] private RunPhaseController run;
        [SerializeField] private GameplayHudRuntimePresenter hud;
        [SerializeField] private MoraleHudPresenter moraleHud;
        [SerializeField] private ResourceValuePresenter[] resourceViews;
        [SerializeField] private GameObject inputBlocker, stagePanel;
        [SerializeField] private TMP_Text stageText, summaryText;
        [SerializeField] private Image[] centralLayers;
        [SerializeField] private CollapseReportView collapseView;
        [SerializeField] private bool reducedMotion;
        private readonly Queue<ValueTransactionResult> queue = new Queue<ValueTransactionResult>();
        private readonly SettlementPresentationCoordinator coordinator = new SettlementPresentationCoordinator();
        private readonly LiveValueAnimation live = new LiveValueAnimation();
        private Coroutine routine;
        private ValueTransactionResult active;
        private bool skip, resetting;
        private long resetBarrier;
        private readonly HashSet<long> seenResults = new HashSet<long>();
        public bool IsBlockingPresentation => active != null || queue.Count > 0;
        public bool IsLiveAnimating => live.IsActive;
        public bool IsPresenting => IsBlockingPresentation || IsLiveAnimating;
        public void Configure(RunPhaseController owner, GameplayHudRuntimePresenter baseHud, MoraleHudPresenter morale,
            ResourceValuePresenter[] resources, GameObject blocker, GameObject stage, TMP_Text stageBody,
            TMP_Text summary, Image[] layers, CollapseReportView collapse)
        {
            run = owner; hud = baseHud; moraleHud = morale; resourceViews = resources;
            inputBlocker = blocker; stagePanel = stage; stageText = stageBody; summaryText = summary;
            centralLayers = layers; collapseView = collapse;
        }
        private void OnEnable()
        {
            ValueTransactions.EnsureInitialized();
            ValueTransactions.TransactionCompleted += OnCompleted;
            ValueTransactions.ResetRequested += OnReset;
            if (hud != null) hud.SetValuesPresentationOwnedExternally(true);
            if (moraleHud != null) moraleHud.SetSettlementPresentationOwned(true);
            if (run != null) run.RegisterSettlementPresenter(this);
            Synchronize(ValueSnapshot.Capture());
        }
        private void OnDisable()
        {
            ValueTransactions.TransactionCompleted -= OnCompleted;
            ValueTransactions.ResetRequested -= OnReset;
            if (routine != null) StopCoroutine(routine);
            live.Clear();
            routine = null; coordinator.Cancel(); queue.Clear();
            var pending = active; active = null;
            Synchronize(ValueSnapshot.Capture());
            if (inputBlocker != null) inputBlocker.SetActive(false);
            if (stagePanel != null) stagePanel.SetActive(false);
            if (hud != null) hud.SetValuesPresentationOwnedExternally(false);
            if (moraleHud != null) moraleHud.SetSettlementPresentationOwned(false);
            if (run != null) { run.RegisterSettlementPresenter(null); if (!resetting && pending != null) run.CompleteSettlementPresentation(pending.Id); }
            if (run == null || !run.IsRunEnded) GameplayInteractionLock.Release(this);
        }
        private void OnReset()
        {
            resetting = true;
            resetBarrier = ValueTransactions.CurrentSequence; seenResults.Clear();
            if (routine != null) StopCoroutine(routine);
            live.Clear();
            routine = null; active = null; queue.Clear(); coordinator.Cancel();
            if (collapseView != null) collapseView.Hide();
            if (inputBlocker != null) inputBlocker.SetActive(false);
            if (stagePanel != null) stagePanel.SetActive(false);
            if (summaryText != null) summaryText.text = string.Empty;
            GameplayInteractionLock.Release(this); Synchronize(ValueSnapshot.Capture()); resetting = false;
        }
        private void Update()
        {
            if (!IsBlockingPresentation && live.IsActive)
            {
                live.Tick(Time.unscaledDeltaTime);
                RenderLiveFrame();
                if (!live.IsActive) Synchronize(live.Target);
            }
            if (active != null && ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame))) skip = true;
        }
        public void SetReducedMotion(bool enabled) { reducedMotion = enabled; moraleHud?.SetReducedMotion(enabled); }
        public void SkipCurrentStep() { if (active != null) skip = true; }
        private void OnCompleted(ValueTransactionResult result)
        {
            if (!seenResults.Add(result.Id)) return;
            if (result.Kind == ValueTransactionKind.Reset) { live.Clear(); Synchronize(result.After); return; }
            if (result.Id <= resetBarrier) return;
            if (result.Changes.Count == 0 && !result.IsCollapsed && result.Kind != ValueTransactionKind.RoundEnd)
            { if (!IsPresenting) Synchronize(result.After); return; }
            if (collapseView != null && collapseView.IsVisible) return;
            if (!ValuePresentationPolicy.RequiresBlocking(result))
            {
                live.Submit(result, reducedMotion ? .08f : InTurnfadeDuration);
                // A new transaction retargets the same local animation, so no older coroutine can overwrite it.
                // If a blocking presentation already owns the HUD, keep this frame deferred until it finishes.
                if (!IsBlockingPresentation) RenderLiveFrame();
                return;
            }
            live.Clear();
            if (!IsBlockingPresentation) Synchronize(result.Before);
            if (result.IsCollapsed) queue.Clear();
            queue.Enqueue(result);
            GameplayInteractionLock.Acquire(this);
            if (routine == null) routine = StartCoroutine(DrainSafely());
        }
        private IEnumerator DrainSafely()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Drain());
            while (stack.Count > 0)
            {
                object current = null; bool next = false; Exception failure = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null)
                {
                    live.Clear();
                    Debug.LogException(failure, this); queue.Clear(); active = null; coordinator.Cancel(); routine = null;
                    if (inputBlocker != null) inputBlocker.SetActive(false);
                    if (stagePanel != null) stagePanel.SetActive(false);
                    Synchronize(ValueSnapshot.Capture());
                    run?.RegisterSettlementPresenter(null);
                    GameplayInteractionLock.Release(this);
                    yield break;
                }
                if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
        }
        private IEnumerator Drain()
        {
            // Wait until the submitting call stack has produced all reports and set phase guards.
            yield return null;
            while (queue.Count > 0)
            {
                active = queue.Dequeue();
                moraleHud?.SetSettlementPresentationOwned(true);
                if (inputBlocker != null) inputBlocker.SetActive(true);
                moraleHud?.CloseDetail();
                coordinator.Begin(active, null);
                int generation = coordinator.Generation;
                yield return PlayStep(SettlementStep.Resources, active.Before.Get(ValueId.Morale), active.After.Get(ValueId.Morale), .35f);
                coordinator.Advance(generation);
                var change = active.Morale?.ChangeResult;
                int from = active.Before.Get(ValueId.Morale), to = active.After.Get(ValueId.Morale);
                if (change != null && change.StageChanged)
                {
                    // Use committed transition records, never recompute a Stage from an interpolated number.
                    foreach (var transition in change.StageTransitions)
                    {
                        int boundary = transition.To == MoraleCollapseStage.Shaken ? active.StableThreshold - 1 : active.CriticalThreshold - 1;
                        yield return PlayStep(SettlementStep.Morale, from, Math.Max(to, boundary), .35f);
                        from = Math.Max(to, boundary);
                        yield return WaitStep(.15f);
                    }
                }
                yield return PlayStep(SettlementStep.Morale, from, to, .45f);
                coordinator.Advance(generation);
                if (change != null && change.StageChanged)
                {
                    if (stagePanel != null) stagePanel.SetActive(true);
                    string text = SettlementResultFormatter.StageSummary(active);
                    if (stageText != null) stageText.text = text;
                    if (summaryText != null) summaryText.text = text;
                    for (int i = 0; centralLayers != null && i < centralLayers.Length; i++)
                        if (centralLayers[i] != null)
                        {
                            bool lost = i < (int)change.StageAfter;
                            centralLayers[i].color = lost ? new Color32(95,92,86,255) : new Color32(226,184,105,255);
                            (centralLayers[i] as PyramidLayerImage)?.SetDamaged(lost);
                            var mark=centralLayers[i].GetComponentInChildren<TMP_Text>();
                            if(mark!=null)mark.text=lost ? "╱ 已損毀 ╲" : "";
                        }
                    yield return PlayStep(SettlementStep.Stage, to, to, 2.2f);
                    if (stagePanel != null) stagePanel.SetActive(false);
                    coordinator.Advance(generation);
                }
                Synchronize(active.After);
                // Input which skipped the last step cannot activate a subsequent flow.
                yield return ReleaseInput();
                var completed = active; active = null;
                if (completed.IsCollapsed)
                {
                    queue.Clear();
                    if (collapseView != null) collapseView.Show(completed);
                    if (inputBlocker != null) inputBlocker.SetActive(false);
                    run?.CompleteSettlementPresentation(completed.Id);
                    break;
                }
                if (inputBlocker != null) inputBlocker.SetActive(false);
                run?.CompleteSettlementPresentation(completed.Id);
            }
            routine = null;
            if (run == null || !run.IsRunEnded) GameplayInteractionLock.Release(this);
        }
        private void RenderLiveFrame()
        {
            if (resourceViews != null)
                foreach (var view in resourceViews)
                {
                    if (view == null) continue;
                    long delta = live.Delta(view.Id);
                    if (view.Id == ValueId.Food) delta += live.Delta(ValueId.TemporaryFood);
                    view.PresentLive(live.Displayed(view.Id), live.Displayed(ValueId.TemporaryFood), live.Target, delta, live.Progress);
                }
            moraleHud?.ShowPresentedMorale(live.Displayed(ValueId.Morale),
                (MoraleCollapseStage)live.Target.Get(ValueId.MoraleStage));
        }
        private IEnumerator PlayStep(SettlementStep step, int from, int to, float seconds)
        {
            skip = false;
            float elapsed = 0f, duration = reducedMotion ? .08f : seconds;
            var beforeStage = active.Morale?.ChangeResult.StageBefore ?? MoraleSystem.CollapseStage;
            do
            {
                float t = skip ? 1f : Mathf.Clamp01(elapsed / duration);
                if (step == SettlementStep.Resources && resourceViews != null)
                    foreach (var view in resourceViews) if (view != null) view.Present(active, t);
                if (step == SettlementStep.Morale)
                    moraleHud?.ShowPresentedMorale(t >= 1 ? to : (int)Math.Round(from + ((long)to - from) * t), beforeStage);
                if (step == SettlementStep.Stage && stagePanel != null)
                {
                    var rect = stagePanel.transform as RectTransform;
                    if (rect != null) rect.localScale = reducedMotion ? Vector3.one : Vector3.one * Mathf.Lerp(.9f, 1f, Mathf.Min(1f, t * 4));
                }
                if (t >= 1f) break;
                elapsed += Time.unscaledDeltaTime; yield return null;
            } while (true);
            yield return ReleaseInput();
        }
        private IEnumerator WaitStep(float seconds)
        { yield return new WaitForSecondsRealtime(reducedMotion ? .01f : seconds); }
        private static IEnumerator ReleaseInput()
        {
            yield return null;
            while ((Mouse.current != null && Mouse.current.leftButton.isPressed)
                || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)) yield return null;
            yield return null;
        }
        private void Synchronize(ValueSnapshot state)
        {
            if (resourceViews != null) foreach (var view in resourceViews) if (view != null) view.Synchronize(state);
            moraleHud?.ShowPresentedMorale(state.Get(ValueId.Morale), (MoraleCollapseStage)state.Get(ValueId.MoraleStage));
        }
    }
}
