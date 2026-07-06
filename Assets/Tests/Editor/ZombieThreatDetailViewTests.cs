using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.UI.Zombies;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public class ZombieThreatDetailViewTests
    {
        [Test]
        public void Open_ActivatesViewThatHasNeverBeenEnabled()
        {
            GameObject detailObject = new GameObject(
                "ZombieDetailView_Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Image backgroundImage = detailObject.GetComponent<Image>();
            backgroundImage.type = Image.Type.Tiled;
            detailObject.SetActive(false);
            ZombieThreatDetailView detailView = detailObject.AddComponent<ZombieThreatDetailView>();

            try
            {
                detailView.Open(new ZombieThreatRuntime(1, null));

                Assert.That(detailObject.activeSelf, Is.True);
                Assert.That(backgroundImage.type, Is.EqualTo(Image.Type.Simple));

                detailView.Close();
                Assert.That(detailObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(detailObject);
            }
        }
    }
}
