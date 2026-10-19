using NUnit.Framework;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Events.UI;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class EventResultPresentationTests
    {
        [Test]
        public void SnapshotDifference_UsesActualChangesIncludingEnergyClamp()
        {
            var before = new EventValueSnapshot(10, 20, 8, 4);
            var after = new EventValueSnapshot(6, 23, 0, 5);
            var delta = after.DifferenceFrom(before);
            Assert.That(delta.Food, Is.EqualTo(-4));
            Assert.That(delta.Resource, Is.EqualTo(3));
            Assert.That(delta.Combat, Is.EqualTo(-8));
            Assert.That(delta.Energy, Is.EqualTo(1));
        }

        [Test]
        public void ActualResult_DoesNotCountPaymentTwice_AndIconsSuppressOnlyCoreText()
        {
            var availability = new EventChoiceAvailability(true, EventChoiceBlockReason.None, 4, 10, 0, 20);
            var result = EventChoiceExecutionResult.Success(availability, null, null, "", "",
                valueChanges: new EventValueSnapshot(-4, 3, -8, 1));
            Assert.That(EventPresentationFormatter.BuildResultSummary(null, result),
                Is.EqualTo("食物 -4　｜　資源 +3　｜　戰鬥值 -8　｜　能量 +1"));
            Assert.That(EventPresentationFormatter.BuildResultSummary(null, result, true), Is.Empty);
        }

        [Test]
        public void MissingSnapshot_FallsBackToPaymentText_AndFailureHasNoReward()
        {
            var availability = new EventChoiceAvailability(true, EventChoiceBlockReason.None, 4, 10, 0, 20);
            var result = EventChoiceExecutionResult.Success(availability, null, null, "", "");
            Assert.That(EventPresentationFormatter.BuildResultSummary(null, result, true), Is.EqualTo("食物 -4"));
            Assert.That(EventPresentationFormatter.BuildResultSummary(null,
                EventChoiceExecutionResult.Failure(availability)), Is.Empty);
        }
    }
}
