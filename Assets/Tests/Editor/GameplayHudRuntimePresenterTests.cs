using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class GameplayHudRuntimePresenterTests
    {
        [Test]
        public void ConfigureWhileEnabled_RebindsCardControllerEvents()
        {
            GameObject presenterObject = new GameObject("HudPresenterTest");
            GameObject firstControllerObject = new GameObject("FirstCardController");
            GameObject secondControllerObject = new GameObject("SecondCardController");
            GameObject drawTextObject = new GameObject("DrawText", typeof(RectTransform));
            presenterObject.SetActive(false);
            GameplayHudRuntimePresenter presenter = presenterObject.AddComponent<GameplayHudRuntimePresenter>();
            CardController first = firstControllerObject.AddComponent<CardController>();
            CardController second = secondControllerObject.AddComponent<CardController>();
            TMP_Text drawText = drawTextObject.AddComponent<TextMeshProUGUI>();

            try
            {
                presenter.Configure(
                    null,
                    first,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    drawText,
                    null,
                    null,
                    null,
                    null,
                    null);
                presenterObject.SetActive(true);
                presenter.Configure(
                    null,
                    second,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    drawText,
                    null,
                    null,
                    null,
                    null,
                    null);

                Assert.That(CountSubscriptions(first, presenter), Is.Zero);
                Assert.That(CountSubscriptions(second, presenter), Is.EqualTo(1));

                drawText.text = "sentinel";
                InvokeStateChanged(first);
                Assert.That(drawText.text, Is.EqualTo("sentinel"));

                InvokeStateChanged(second);
                Assert.That(drawText.text, Is.EqualTo("抽牌堆 0"));
            }
            finally
            {
                Object.DestroyImmediate(drawTextObject);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(secondControllerObject);
                Object.DestroyImmediate(firstControllerObject);
            }
        }

        [Test]
        public void ReservedResource_DisplaysAvailableAmountAndLockedAmount()
        {
            GameObject presenterObject = new GameObject("HudPresenterResourceTest");
            GameObject resourceTextObject = new GameObject("ResourceText", typeof(RectTransform));
            presenterObject.SetActive(false);
            GameplayHudRuntimePresenter presenter = presenterObject.AddComponent<GameplayHudRuntimePresenter>();
            TMP_Text resourceText = resourceTextObject.AddComponent<TextMeshProUGUI>();
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            ResourceSystem.Reset(17);

            try
            {
                Assert.That(
                    ResourceReservationService.TryReserve(
                        new ResourceCost(0, 3),
                        out _),
                    Is.True);
                presenter.Configure(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    resourceText,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);

                presenter.RefreshAll();

                Assert.That(resourceText.text, Is.EqualTo("資源 14  鎖3"));
            }
            finally
            {
                ResourceReservationService.Reset();
                Object.DestroyImmediate(resourceTextObject);
                Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void MissingCardController_HidesExistingHandRoots()
        {
            GameObject presenterObject = new GameObject("HudPresenterMissingCardsTest");
            GameObject cardRootObject = new GameObject("CardRoot", typeof(RectTransform));
            presenterObject.SetActive(false);
            GameplayHudRuntimePresenter presenter = presenterObject.AddComponent<GameplayHudRuntimePresenter>();
            RectTransform cardRoot = cardRootObject.GetComponent<RectTransform>();
            cardRoot.gameObject.SetActive(true);

            try
            {
                presenter.Configure(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    new[] { cardRoot },
                    null,
                    null,
                    null);
                presenter.RefreshAll();

                Assert.That(cardRoot.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cardRootObject);
                Object.DestroyImmediate(presenterObject);
            }
        }

        private static void InvokeStateChanged(CardController controller)
        {
            MethodInfo method = typeof(CardController).GetMethod(
                "NotifyStateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, null);
        }

        private static int CountSubscriptions(
            CardController controller,
            GameplayHudRuntimePresenter presenter)
        {
            FieldInfo eventField = typeof(CardController).GetField(
                "StateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(eventField, Is.Not.Null);
            System.Delegate handlers = eventField.GetValue(controller) as System.Delegate;
            if (handlers == null)
            {
                return 0;
            }

            int count = 0;
            foreach (System.Delegate handler in handlers.GetInvocationList())
            {
                if (ReferenceEquals(handler.Target, presenter))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
