using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Effects.Editor;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class GameEffectDataDrawerTests
    {
        [TestCase(GameEffectCarrier.Event, GameEffectCategory.Value, true)]
        [TestCase(GameEffectCarrier.Event, GameEffectCategory.CardFlow, true)]
        [TestCase(GameEffectCarrier.Event, GameEffectCategory.EventState, true)]
        [TestCase(GameEffectCarrier.Event, GameEffectCategory.Threat, false)]
        [TestCase(GameEffectCarrier.Event, GameEffectCategory.NextRoundEffect, false)]
        [TestCase(GameEffectCarrier.Card, GameEffectCategory.Value, false)]
        [TestCase(GameEffectCarrier.Other, GameEffectCategory.Value, false)]
        public void EventScalingVisibilityMatchesTheDrawerHeightRule(
            GameEffectCarrier carrier,
            GameEffectCategory category,
            bool expected)
        {
            Assert.That(
                GameEffectDataDrawer.ShouldShowEventScalingFields(carrier, category),
                Is.EqualTo(expected));
        }

        [Test]
        public void SpawnHordeEffectReservesOnlyItsVisibleNineRows()
        {
            GameEffectDataHolder holder = ScriptableObject.CreateInstance<GameEffectDataHolder>();
            try
            {
                SerializedObject serialized = new SerializedObject(holder);
                SerializedProperty effect = serialized.FindProperty("effect");
                effect.FindPropertyRelative("carrier").enumValueIndex = (int)GameEffectCarrier.Event;
                effect.FindPropertyRelative("category").enumValueIndex = (int)GameEffectCategory.Threat;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameEffectDataDrawer drawer = new GameEffectDataDrawer();
                const float expectedHeight = 206f;
                Assert.That(drawer.GetPropertyHeight(effect, GUIContent.none), Is.EqualTo(expectedHeight));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        private sealed class GameEffectDataHolder : ScriptableObject
        {
            [SerializeField] private GameEffectData effect = new GameEffectData();
        }
    }
}
