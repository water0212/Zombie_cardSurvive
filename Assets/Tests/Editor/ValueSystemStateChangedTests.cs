using NUnit.Framework;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class ValueSystemStateChangedTests
    {
        [SetUp]
        public void SetUp()
        {
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
            MoraleSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
            MoraleSystem.Reset();
        }

        [Test]
        public void FoodTurnStartProduction_PublishesOneFinalState()
        {
            FoodSystem.AddProductionEffect("food-a", 2);
            FoodSystem.AddProductionEffect("food-b", 3, isPermanent: false, remainingTurns: 1);
            int notificationCount = 0;
            int observedFood = -1;
            int observedEffectCount = -1;
            void Capture()
            {
                notificationCount++;
                observedFood = FoodSystem.Food;
                observedEffectCount = FoodSystem.ProductionEffects.Count;
            }

            FoodSystem.StateChanged += Capture;
            try
            {
                FoodSystem.ApplyTurnStartProduction();
            }
            finally
            {
                FoodSystem.StateChanged -= Capture;
            }

            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(observedFood, Is.EqualTo(5));
            Assert.That(observedEffectCount, Is.EqualTo(1));
        }

        [Test]
        public void ResourceTurnStartProduction_PublishesOneFinalState()
        {
            ResourceSystem.AddProductionEffect("resource-a", 4);
            ResourceSystem.AddProductionEffect("resource-b", -1, isPermanent: false, remainingTurns: 1);
            int notificationCount = 0;
            int observedResource = -1;
            int observedEffectCount = -1;
            void Capture()
            {
                notificationCount++;
                observedResource = ResourceSystem.Resource;
                observedEffectCount = ResourceSystem.ProductionEffects.Count;
            }

            ResourceSystem.StateChanged += Capture;
            try
            {
                ResourceSystem.ApplyTurnStartProduction();
            }
            finally
            {
                ResourceSystem.StateChanged -= Capture;
            }

            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(observedResource, Is.EqualTo(4));
            Assert.That(observedEffectCount, Is.EqualTo(1));
        }

        [Test]
        public void MoraleReset_PublishesFinalResetSnapshot()
        {
            MoraleSystem.Reset(20, maxValue: 60);
            MoraleChangeResult observed = null;
            void Capture(MoraleChangeResult result) => observed = result;

            MoraleSystem.StateChanged += Capture;
            try
            {
                MoraleSystem.Reset();
            }
            finally
            {
                MoraleSystem.StateChanged -= Capture;
            }

            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.Source, Is.EqualTo("Reset"));
            Assert.That(observed.MoraleAfter, Is.EqualTo(100));
            Assert.That(observed.MaximumMoraleAfter, Is.EqualTo(100));
            Assert.That(observed.StageAfter, Is.EqualTo(MoraleCollapseStage.Stable));
            Assert.That(observed.IsCollapsed, Is.False);
        }
    }
}
