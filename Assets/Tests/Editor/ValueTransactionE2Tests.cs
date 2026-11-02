#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;
using ZombieCardSurvive.UI.Settlement;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;
using System.Collections.Generic;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class ValueTransactionE2Tests
    {
        [SetUp] public void Reset()
        {
            ResourceReservationService.Reset(); FoodSystem.Reset(10); FoodSystem.SetStorageCapacity(50);
            ResourceSystem.Reset(10); EnergySystem.Reset(3); CombatSystem.Reset();
            MoraleSystem.Reset(startingMorale:80); MoralePyramidSystem.Reset(); GameplayInteractionLock.Reset();
            FoodPlanSystem.Reset(); NextRoundEffectSystem.Reset(); ValueTransactions.EnsureInitialized();
        }
        [TearDown] public void Cleanup(){ResourceReservationService.Reset();GameplayInteractionLock.Reset();MoralePyramidSystem.Reset();}
        [Test] public void SpendThenGain_JournalHasTwoChanges_FinalNetZero_CompleteOnce()
        {
            int notifications=0; Action<ValueTransactionResult> callback=_=>notifications++;
            ValueTransactions.TransactionCompleted+=callback;
            try {using(var tx=new ValueTransaction("Card:FoodLoop",ValueTransactionKind.Card))
            {
                Assert.That(GameValues.Food.Change(-2,tx),Is.True);Assert.That(GameValues.Food.Change(2,tx),Is.True);
                Assert.That(notifications,Is.Zero);
                var result=tx.Complete();Assert.That(result.Changes.Count(c=>c.Id==ValueId.Food),Is.EqualTo(2));
                Assert.That(result.Delta(ValueId.Food),Is.Zero);Assert.That(tx.Complete(),Is.SameAs(result));
                Assert.That(notifications,Is.EqualTo(1));
            }}finally{ValueTransactions.TransactionCompleted-=callback;}
        }
        [Test] public void ReservedResources_AreNotSpendable_AndFailureDoesNotMutate()
        {
            Assert.That(ResourceReservationService.TryReserve(new ResourceCost(9,9),out _),Is.True);
            using(var tx=new ValueTransaction("Payment",ValueTransactionKind.Card))
            {Assert.That(GameValues.Food.Change(-2,tx),Is.False);Assert.That(GameValues.Resource.TrySpend(2,tx),Is.False);
             Assert.That(tx.Complete().Changes,Is.Empty);Assert.That(FoodSystem.Food,Is.EqualTo(10));}
        }
        [Test] public void CompletedSnapshot_RemainsIndependentOfNextTransaction()
        {
            ValueTransactionResult first;
            using(var tx=new ValueTransaction("one",ValueTransactionKind.Card)){GameValues.Food.Change(-2,tx);first=tx.Complete();}
            using(var tx=new ValueTransaction("two",ValueTransactionKind.Card)){GameValues.Food.Change(5,tx);tx.Complete();}
            Assert.That(first.After.Get(ValueId.Food),Is.EqualTo(8));Assert.That(first.Changes.Single().After,Is.EqualTo(8));
        }
        [Test] public void Interrupt_PublishesActualMutation_WithoutRollback()
        {
            ValueTransactionResult result=null; Action<ValueTransactionResult> callback=r=>result=r;
            ValueTransactions.TransactionCompleted+=callback;
            try {Assert.Throws<InvalidOperationException>(()=>{using(var tx=new ValueTransaction("Interrupted",ValueTransactionKind.Card))
                tx.Execute(()=>{GameValues.Food.Change(-2,tx);throw new InvalidOperationException();});});
                Assert.That(result.Interrupted,Is.True);Assert.That(result.Delta(ValueId.Food),Is.EqualTo(-2));
                Assert.That(FoodSystem.Food,Is.EqualTo(8));Assert.That(ValueTransactions.IsExecuting,Is.False);
            }finally{ValueTransactions.TransactionCompleted-=callback;}
        }
        [Test] public void NestedMutation_JoinsSameOwner_DifferentOwnerIsRejected()
        {
            using(var tx=new ValueTransaction("owner",ValueTransactionKind.Card))
            {tx.Execute(()=>{Assert.That(GameValues.Food.Change(-1,tx),Is.True);
                using(var other=new ValueTransaction("other",ValueTransactionKind.Card))Assert.Throws<InvalidOperationException>(()=>other.Execute(()=>true));});
             Assert.That(tx.Complete().Delta(ValueId.Food),Is.EqualTo(-1));}
        }
        [Test] public void BatchCollapse_ResultContainsEveryCause_BeforeFailurePresentation()
        {
            var request=new MoraleBatchRequest("RoundEnd");request.Add("FoodShortage",-70);request.Add("ZombieLeak",-15,-5);
            ValueTransactionResult result=null;Action<ValueTransactionResult> callback=r=>result=r;
            ValueTransactions.TransactionCompleted+=callback;
            try{MoraleSystem.CommitBatch(request);Assert.That(result.IsCollapsed,Is.True);
                Assert.That(result.Morale.Contributions.Count,Is.EqualTo(2));Assert.That(result.Morale.ChangeResult.MoraleBefore,Is.EqualTo(80));
                StringAssert.Contains("食物短缺",SettlementResultFormatter.CollapseSummary(result));
                StringAssert.Contains("殭屍漏防",SettlementResultFormatter.CollapseSummary(result));}
            finally{ValueTransactions.TransactionCompleted-=callback;}
        }
        [Test] public void MultipleMoraleBatches_KeepFirstBefore_FinalAfter_AllCauses()
        {
            using(var tx=new ValueTransaction("Card:Nested",ValueTransactionKind.Card))
            {
                tx.Execute(()=>{var first=new MoraleBatchRequest("first");first.Add("a",-10);first.Add("b",-10);MoraleSystem.CommitBatch(first);
                    var second=new MoraleBatchRequest("second");second.Add("c",-60);MoraleSystem.CommitBatch(second);});
                var result=tx.Complete();Assert.That(result.Morale.Contributions.Count,Is.EqualTo(3));
                Assert.That(result.Morale.ChangeResult.MoraleBefore,Is.EqualTo(80));Assert.That(result.Morale.ChangeResult.MoraleAfter,Is.Zero);
            }
        }
        [Test] public void TwoStagesThenRecovery_DoesNotRepairLostLayers()
        {
            using(var tx=new ValueTransaction("two stages",ValueTransactionKind.Card))
            {tx.Execute(()=>MoraleSystem.CommitChange("loss",-55));var r=tx.Complete();Assert.That(r.Morale.ChangeResult.StageTransitions.Count,Is.EqualTo(2));}
            MoraleSystem.AddMorale(50);Assert.That(MoraleSystem.CollapseStage,Is.EqualTo(MoraleCollapseStage.Brink));
        }
        [Test] public void Coordinator_StagesOptional_StaleAndRepeatedCallbacksAreIgnored()
        {
            var coordinator=new SettlementPresentationCoordinator();int finished=0;
            using(var tx=new ValueTransaction("NoStage",ValueTransactionKind.Card))
            {
                var result=tx.Complete();coordinator.Begin(result,()=>finished++);int old=coordinator.Generation;
                coordinator.Cancel();coordinator.Begin(result,()=>finished++);
                Assert.That(coordinator.Advance(old),Is.False);int current=coordinator.Generation;
                Assert.That(coordinator.Current,Is.EqualTo(SettlementStep.Resources));coordinator.Advance(current);
                Assert.That(coordinator.Current,Is.EqualTo(SettlementStep.Morale));coordinator.Advance(current);
                Assert.That(coordinator.Advance(current),Is.False);Assert.That(finished,Is.EqualTo(1));
            }
        }
        [TestCase(ValueTransactionKind.Card)] [TestCase(ValueTransactionKind.Event)] [TestCase(ValueTransactionKind.RoundEnd)]
        public void FailedGuard_IsImmediate_ForAllOperationKinds(ValueTransactionKind kind)
        {
            var go=new GameObject("E2 test run");go.SetActive(false);
            var run=go.AddComponent<RunPhaseController>();var serialized=new SerializedObject(run);
            serialized.FindProperty("startRunOnAwake").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();go.SetActive(true);
            typeof(RunPhaseController).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,null);
            int failed=0;run.RunFailed+=_=>failed++;
            try{using(var tx=new ValueTransaction(kind.ToString(),kind))
                {tx.Execute(()=>{MoraleSystem.CommitChange("fatal",-100);Assert.That(run.CurrentPhase,Is.EqualTo(RunPhase.Failed));
                    Assert.That(GameplayInteractionLock.IsLocked,Is.True);run.EnterEventPhase();run.EnterTurnPhase();});tx.Complete();}
                MoraleSystem.CommitChange("duplicate",-1);Assert.That(failed,Is.EqualTo(1));Assert.That(run.CurrentPhase,Is.EqualTo(RunPhase.Failed));}
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void NoPresenter_RoundEndsAtEvent_AndDuplicateCompletionDoesNotAdvance()
        {
            var go=new GameObject("E2 fallback run");go.SetActive(false);var run=go.AddComponent<RunPhaseController>();
            var serialized=new SerializedObject(run);serialized.FindProperty("startRunOnAwake").boolValue=false;
            serialized.ApplyModifiedPropertiesWithoutUndo();go.SetActive(true);int events=0;
            run.PhaseChanged+=phase=>{if(phase==RunPhase.Event)events++;};
            try{run.EnterRoundEndPhase();Assert.That(run.CurrentPhase,Is.EqualTo(RunPhase.Event));Assert.That(run.IsSettlementPending,Is.False);
                run.CompleteSettlementPresentation(long.MaxValue);Assert.That(events,Is.EqualTo(1));Assert.That(GameplayInteractionLock.IsLocked,Is.False);}
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void RestartRequest_IsOneShot_AndDoesNotStartRun()
        {
            var go=new GameObject("Report test");var report=go.AddComponent<CollapseReportView>();int requests=0;
            report.RestartRequested+=()=>requests++;
            try{report.RequestRestart();report.RequestRestart();report.RequestLeave();Assert.That(requests,Is.EqualTo(1));}
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void RealCardController_PublishesOneCompletedFatalCard_WithAllCauses()
        {
            var go=new GameObject("E2 card controller");go.SetActive(false);var controller=go.AddComponent<CardController>();
            var data=ScriptableObject.CreateInstance<CardBase>();data.name="E2 fatal card";
            var serialized=new SerializedObject(data);var effects=serialized.FindProperty("effects");effects.arraySize=2;
            for(int i=0;i<2;i++) {var effect=effects.GetArrayElementAtIndex(i);effect.FindPropertyRelative("carrier").enumValueIndex=(int)GameEffectCarrier.Card;
                effect.FindPropertyRelative("category").enumValueIndex=(int)GameEffectCategory.Value;
                effect.FindPropertyRelative("valueEffectType").enumValueIndex=(int)ValueEffectType.ReduceMorale;effect.FindPropertyRelative("amount").intValue=45;}
            serialized.ApplyModifiedPropertiesWithoutUndo();var card=new CardRuntime(data);
            var hand=(List<CardRuntime>)typeof(CardController).GetField("handCards",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(controller);
            hand.Add(card);int count=0;ValueTransactionResult result=null;Action<ValueTransactionResult> capture=r=>{count++;result=r;};
            ValueTransactions.TransactionCompleted+=capture;
            try{Assert.That(controller.RequestPlayCard(card),Is.True);Assert.That(count,Is.EqualTo(1));
                Assert.That(result.Kind,Is.EqualTo(ValueTransactionKind.Card));Assert.That(result.IsCollapsed,Is.True);Assert.That(result.Morale.Contributions.Count,Is.EqualTo(2));
                Assert.That(controller.RequestPlayCard(card),Is.False);}
            finally{ValueTransactions.TransactionCompleted-=capture;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(data);}
        }
        [Test] public void RealEventChoice_PublishesOneCompletedFatalEvent()
        {
            var effect=new GameEffectData();SetField(effect,"carrier",GameEffectCarrier.Event);SetField(effect,"category",GameEffectCategory.Value);
            SetField(effect,"valueEffectType",ValueEffectType.ReduceMorale);SetField(effect,"amount",100);
            var choice=new EventChoiceData();SetField(choice,"choiceText","Fatal event option");SetField(choice,"effects",new List<GameEffectData>{effect});
            var resolved=ResolvedEventChoiceRuntime.Create(choice,null);ValueTransactionResult result=null;int count=0;
            Action<ValueTransactionResult> capture=r=>{result=r;count++;};ValueTransactions.TransactionCompleted+=capture;
            try{Assert.That(resolved.TryExecute(GameEffectContext.ForEvent(),out _),Is.True);Assert.That(count,Is.EqualTo(1));
                Assert.That(result.Kind,Is.EqualTo(ValueTransactionKind.Event));Assert.That(result.IsCollapsed,Is.True);StringAssert.Contains("Fatal event option",result.Source);}
            finally{ValueTransactions.TransactionCompleted-=capture;}
        }
        [Test] public void RealRoundEnd_CollapsesBeforeAnyEvent_AndPublishesFullRoundResult()
        {
            var go=new GameObject("E2 real round");go.SetActive(false);var resolver=go.AddComponent<RoundEndResolver>();
            var run=go.AddComponent<RunPhaseController>();SetField(run,"startRunOnAwake",false);SetField(run,"roundEndResolver",resolver);
            typeof(RunPhaseController).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,null);
            SetField(run,"cardController",null);SetField(run,"eventFlowController",null);FoodSystem.Reset(0);MoraleSystem.Reset(startingMorale:1);
            int events=0;run.PhaseChanged+=phase=>{if(phase==RunPhase.Event)events++;};ValueTransactionResult result=null;
            Action<ValueTransactionResult> capture=r=>result=r;ValueTransactions.TransactionCompleted+=capture;
            try{run.EnterRoundEndPhase();Assert.That(run.CurrentPhase,Is.EqualTo(RunPhase.Failed));Assert.That(events,Is.Zero);
                Assert.That(result.Kind,Is.EqualTo(ValueTransactionKind.RoundEnd));Assert.That(result.IsCollapsed,Is.True);
                Assert.That(result.Morale.Contributions.Any(c=>c.Source=="FoodShortage"),Is.True);Assert.That(run.IsSettlementPending,Is.False);}
            finally{ValueTransactions.TransactionCompleted-=capture;UnityEngine.Object.DestroyImmediate(go);}
        }
        private static void SetField(object target,string name,object value)
            => target.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(target,value);
    }
}
#endif
