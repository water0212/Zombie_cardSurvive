using NUnit.Framework;
using UnityEngine;
using ZombieCardSurvive.Cards.UI.Zombies;

namespace ZombieCardSurvive.Tests.Editor
{
    public class ZombieThreatLayoutCalculatorTests
    {
        [Test]
        public void FirstSlotStaysAtOrigin()
        {
            ZombieThreatLayoutSlot slot = ZombieThreatLayoutCalculator.CalculateSlot(
                0,
                5,
                Vector2.zero,
                new Vector2(110f, -86f),
                0.85f);

            Assert.AreEqual(Vector2.zero, slot.AnchoredPosition);
            Assert.AreEqual(1f, slot.Scale);
            Assert.AreEqual(0, slot.Row);
            Assert.AreEqual(0, slot.ColumnOffset);
        }

        [Test]
        public void FirstRowFillsFromCenterOut()
        {
            Vector2[] expectedPositions =
            {
                new Vector2(0f, 0f),
                new Vector2(-110f, 0f),
                new Vector2(110f, 0f),
                new Vector2(-220f, 0f),
                new Vector2(220f, 0f)
            };

            for (int index = 0; index < expectedPositions.Length; index++)
            {
                ZombieThreatLayoutSlot slot = ZombieThreatLayoutCalculator.CalculateSlot(
                    index,
                    5,
                    Vector2.zero,
                    new Vector2(110f, -86f),
                    0.85f);

                Assert.AreEqual(expectedPositions[index], slot.AnchoredPosition);
                Assert.AreEqual(1f, slot.Scale);
                Assert.AreEqual(0, slot.Row);
            }
        }

        [Test]
        public void OverflowRowsMoveDownAndScaleDown()
        {
            ZombieThreatLayoutSlot slot = ZombieThreatLayoutCalculator.CalculateSlot(
                5,
                5,
                Vector2.zero,
                new Vector2(110f, -86f),
                0.85f);

            Assert.AreEqual(new Vector2(0f, -86f), slot.AnchoredPosition);
            Assert.AreEqual(0.85f, slot.Scale);
            Assert.AreEqual(1, slot.Row);
            Assert.IsTrue(slot.IsOverflowRow);
        }

        [Test]
        public void ColumnCountIsClampedToOne()
        {
            ZombieThreatLayoutSlot slot = ZombieThreatLayoutCalculator.CalculateSlot(
                2,
                0,
                Vector2.zero,
                new Vector2(110f, -86f),
                0.85f);

            Assert.AreEqual(new Vector2(0f, -172f), slot.AnchoredPosition);
            Assert.AreEqual(0.85f, slot.Scale);
            Assert.AreEqual(2, slot.Row);
        }
    }
}
