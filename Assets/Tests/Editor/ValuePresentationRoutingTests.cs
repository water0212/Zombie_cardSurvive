#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;
using ZombieCardSurvive.UI.Settlement;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class ValuePresentationRoutingTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void SetUp()
        {
            ResourceReservationService.Reset(); ResourceSystem.Reset(10); FoodSystem.Reset(10);
            FoodSystem.SetStorageCapacity(50); EnergySystem.Reset(3); CombatSystem.Reset();
            MoralePyramidSystem.Reset(); MoraleSystem.Reset(startingMorale: 80);
            GameplayInteractionLock.Reset(); ValueTransactions.EnsureInitialized();
        }
        [TearDown] public void TearDown() { GameplayInteractionLock.Reset(); ResourceReservationService.Reset(); MoralePyramidSystem.Reset(); }

        [TestCase(ValueTransactionKind.Card)] [TestCase(ValueTransactionKind.Event)] [TestCase(ValueTransactionKind.System)]
        public void OrdinaryValueChanges_DoNotRequireBlocking(ValueTransactionKind kind)
            => Assert.That(ValuePresentationPolicy.RequiresBlocking(Resource(2, kind)), Is.False);

        [Test] public void MoraleChangeWithoutCrossing_IsLocalOnly()
            => Assert.That(ValuePresentationPolicy.RequiresBlocking(Morale(-5, ValueTransactionKind.Card)), Is.False);

        [TestCase(ValueTransactionKind.Card)] [TestCase(ValueTransactionKind.Event)] [TestCase(ValueTransactionKind.System)]
        public void StageCrossing_ImmediatelyRequiresBlocking_ForEverySource(ValueTransactionKind kind)
        {
            var result = Morale(-20, kind);
            Assert.That(result.Morale.ChangeResult.StageChanged, Is.True);
            Assert.That(ValuePresentationPolicy.RequiresBlocking(result), Is.True);
        }
        [Test] public void MaximumClampThatCrossesStage_AlsoRequiresBlocking()
        {
            using (var tx = new ValueTransaction("Maximum clamp", ValueTransactionKind.Event))
            {
                tx.Execute(() => MoraleSystem.CommitChange("Maximum loss", 0, -40));
                Assert.That(ValuePresentationPolicy.RequiresBlocking(tx.Complete()), Is.True);
            }
        }
        [Test] public void FixedRoundEnd_BlocksEvenWithoutChanges()
        {
            using (var tx = new ValueTransaction("Empty round", ValueTransactionKind.RoundEnd))
                Assert.That(ValuePresentationPolicy.RequiresBlocking(tx.Complete()), Is.True);
        }
        [Test] public void RoundEndWithCrossing_StillUsesOneBlockingRoute()
            => Assert.That(ValuePresentationPolicy.RequiresBlocking(Morale(-20, ValueTransactionKind.RoundEnd)), Is.True);

        [Test] public void CollapseWithoutNewStage_KeepsFailureTakeover()
        {
            MoraleSystem.CommitChange("Already stage two", -70);
            var result = Morale(-10, ValueTransactionKind.Card);
            Assert.That(result.Morale.ChangeResult.StageChanged, Is.False);
            Assert.That(result.IsCollapsed, Is.True);
            Assert.That(ValuePresentationPolicy.RequiresBlocking(result), Is.True);
        }
        [Test] public void Reset_DirectlySynchronizesEvenIfItsFixtureCrossesStage()
            => Assert.That(ValuePresentationPolicy.RequiresBlocking(Morale(-20, ValueTransactionKind.Reset)), Is.False);

        [Test] public void ContinuousChanges_RetargetFromVisibleNumber_AndFinishAtLatestValue()
        {
            var live = new LiveValueAnimation();
            live.Submit(Resource(10), 1f); live.Tick(.4f);
            Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(14));
            live.Submit(Resource(5), 1f);
            Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(14));
            Assert.That(live.Delta(ValueId.Resource), Is.EqualTo(15));
            live.Tick(2f);
            Assert.That(live.IsActive, Is.False);
            Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(25));
            Assert.That(ResourceSystem.Resource, Is.EqualTo(25));
        }
        [Test] public void NewDecrease_ReplacesAnUnfinishedIncrease()
        {
            var live = new LiveValueAnimation(); live.Submit(Resource(10), 1); live.Tick(.5f);
            Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(15));
            live.Submit(Resource(-20), 1); live.Tick(2);
            Assert.That(live.Displayed(ValueId.Resource), Is.Zero);
            Assert.That(live.Delta(ValueId.Resource), Is.EqualTo(-10));
        }
        [Test] public void ZeroNetTransaction_HasNoFalseGainAnimation()
        {
            using (var tx = new ValueTransaction("Pay and gain", ValueTransactionKind.Card))
            {
                GameValues.Resource.Change(-2, tx); GameValues.Resource.Change(2, tx);
                var live = new LiveValueAnimation(); live.Submit(tx.Complete(), 1);
                Assert.That(live.IsActive, Is.False); Assert.That(live.Delta(ValueId.Resource), Is.Zero);
                Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(10));
            }
        }
        [Test] public void Clear_PreventsOldAnimationFromReturningAfterReset()
        {
            var live = new LiveValueAnimation(); live.Submit(Resource(10), 1); live.Tick(.2f); live.Clear(); live.Tick(10);
            Assert.That(live.IsActive, Is.False);
            ResourceSystem.Reset(4); live.Submit(Resource(1), 1);
            Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(4));
            live.Tick(2); Assert.That(live.Displayed(ValueId.Resource), Is.EqualTo(5));
        }
        [Test] public void ReducedMotion_ChangesDurationButNotFinalState()
        {
            var result = Resource(3); var normal = new LiveValueAnimation(); var reduced = new LiveValueAnimation();
            normal.Submit(result, .32f); reduced.Submit(result, .08f); normal.Tick(.1f); reduced.Tick(.1f);
            Assert.That(normal.IsActive, Is.True); Assert.That(reduced.IsActive, Is.False);
            normal.Tick(1); Assert.That(normal.Displayed(ValueId.Resource), Is.EqualTo(reduced.Displayed(ValueId.Resource)));
        }
        [Test] public void TemporaryFoodAndPermanentFood_AnimateTogetherWithoutFalseNetGain()
        {
            using (var fixture = new PresenterFixture(ValueId.Food))
            using (var tx = new ValueTransaction("Food transfer", ValueTransactionKind.Card))
            {
                tx.Execute(() => { FoodSystem.SpendFood(2); FoodSystem.AddTemporaryFood(2); });
                fixture.Submit(tx.Complete()); fixture.Live.Tick(.16f); fixture.Invoke("RenderLiveFrame");
                Assert.That(fixture.Number.text, Does.StartWith("9/50"));
                StringAssert.Contains("+1臨時", fixture.Number.text);
                Assert.That(fixture.DeltaText.text, Is.Empty);
                Assert.That(GameplayInteractionLock.IsLocked, Is.False);
            }
        }
        [Test] public void ActualPresenter_OrdinaryChangesNeverAcquireLockOrShowOverlay()
        {
            using (var fixture = new PresenterFixture())
            {
                fixture.Submit(Resource(2));
                Assert.That(fixture.Presenter.IsLiveAnimating, Is.True);
                Assert.That(fixture.Presenter.IsBlockingPresentation, Is.False);
                Assert.That(GameplayInteractionLock.IsLocked, Is.False);
                Assert.That(fixture.Blocker.activeSelf, Is.False); Assert.That(fixture.Stage.activeSelf, Is.False);
                fixture.Submit(Resource(3));
                Assert.That(GameValues.Resource.CanSpend(15), Is.True, "Authoritative values are ready while the local animation is still running.");
                Assert.That(fixture.Live.Delta(ValueId.Resource), Is.EqualTo(5));
                fixture.Live.Tick(1); fixture.Invoke("RenderLiveFrame");
                Assert.That(fixture.Number.text, Is.EqualTo("15"));
                Assert.That(GameplayInteractionLock.IsLocked, Is.False);
            }
        }
        [Test] public void DuplicateCompletion_DoesNotDoubleTheLiveDelta()
        {
            using (var fixture = new PresenterFixture())
            {
                var result = Resource(2); fixture.Submit(result); fixture.Submit(result);
                Assert.That(fixture.Live.Delta(ValueId.Resource), Is.EqualTo(2));
            }
        }
        [Test] public void Reset_CancelsLocalPresentation_AndCannotOverwriteNewSnapshot()
        {
            using (var fixture = new PresenterFixture())
            {
                fixture.Submit(Resource(5)); ResourceSystem.Reset(4); fixture.Invoke("OnReset");
                Assert.That(fixture.Presenter.IsPresenting, Is.False);
                Assert.That(fixture.Number.text, Is.EqualTo("4"));
                fixture.Invoke("Update"); Assert.That(fixture.Number.text, Is.EqualTo("4"));
            }
        }
        [Test] public void LocalPresentation_DoesNotReleaseAnotherOwnersModalLock()
        {
            var owner = new object(); GameplayInteractionLock.Acquire(owner);
            try
            {
                using (var fixture = new PresenterFixture())
                { fixture.Submit(Resource(1)); fixture.Invoke("OnReset"); Assert.That(GameplayInteractionLock.IsLocked, Is.True); }
            }
            finally { GameplayInteractionLock.Release(owner); }
        }
        [Test] public void BlockingOwner_PreventsLocalFrameFromOverwritingRoundPresentation()
        {
            using (var fixture = new PresenterFixture())
            using (var round = new ValueTransaction("Round fixture", ValueTransactionKind.RoundEnd))
            {
                fixture.Submit(Resource(2));
                typeof(SettlementPresenter).GetField("active", Private).SetValue(fixture.Presenter, round.Complete());
                fixture.Number.text = "round presentation frame";
                fixture.Invoke("Update");
                Assert.That(fixture.Number.text, Is.EqualTo("round presentation frame"));
                Assert.That(fixture.Live.Progress, Is.Zero);
            }
        }
        private static ValueTransactionResult Resource(int delta, ValueTransactionKind kind = ValueTransactionKind.Card)
        {
            using (var tx = new ValueTransaction("Resource fixture", kind))
            { Assert.That(GameValues.Resource.Change(delta, tx), Is.True); return tx.Complete(); }
        }
        private static ValueTransactionResult Morale(int delta, ValueTransactionKind kind)
        {
            using (var tx = new ValueTransaction("Morale fixture", kind))
            { tx.Execute(() => MoraleSystem.CommitChange("Fixture", delta)); return tx.Complete(); }
        }
        private sealed class PresenterFixture : IDisposable
        {
            private readonly GameObject root;
            public readonly SettlementPresenter Presenter;
            public readonly TMP_Text Number, DeltaText;
            public readonly GameObject Blocker, Stage;
            public LiveValueAnimation Live => (LiveValueAnimation)typeof(SettlementPresenter).GetField("live", Private).GetValue(Presenter);
            public PresenterFixture(ValueId id = ValueId.Resource)
            {
                root = new GameObject("Local presenter fixture"); root.SetActive(false);
                var number = new GameObject("Number", typeof(RectTransform)); number.transform.SetParent(root.transform, false);
                Number = number.AddComponent<TextMeshProUGUI>();
                var delta = new GameObject("Delta", typeof(RectTransform)); delta.transform.SetParent(root.transform, false);
                DeltaText = delta.AddComponent<TextMeshProUGUI>();
                var view = number.AddComponent<ResourceValuePresenter>(); view.Configure(id, Number, DeltaText);
                Blocker = new GameObject("Blocker"); Blocker.transform.SetParent(root.transform, false); Blocker.SetActive(false);
                Stage = new GameObject("Stage"); Stage.transform.SetParent(root.transform, false); Stage.SetActive(false);
                Presenter = root.AddComponent<SettlementPresenter>();
                Presenter.Configure(null, null, null, new[] { view }, Blocker, Stage, null, null, Array.Empty<Image>(), null);
            }
            public void Submit(ValueTransactionResult result) => typeof(SettlementPresenter).GetMethod("OnCompleted", Private).Invoke(Presenter, new object[] { result });
            public void Invoke(string method) => typeof(SettlementPresenter).GetMethod(method, Private).Invoke(Presenter, null);
            public void Dispose() => UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
#endif
