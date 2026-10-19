using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Events.UI
{
    /// <summary>只顯示已結算差值；四個 Image 與四個 TMP 數字各自獨立。</summary>
    public sealed class EventResultValuesView : MonoBehaviour
    {
        [SerializeField] private Image[] icons = new Image[4];
        [SerializeField] private TMP_Text[] values = new TMP_Text[4];
        [SerializeField] private TMP_Text heading;

        public bool Show(EventChoiceExecutionResult result)
        {
            bool ready = result != null && result.Succeeded && result.ValueChanges != null
                && icons != null && values != null && icons.Length == 4 && values.Length == 4;
            for (int i = 0; ready && i < 4; i++)
                ready = icons[i] != null && icons[i].sprite != null && values[i] != null;
            gameObject.SetActive(ready);
            if (!ready) return false;

            var delta = result.ValueChanges;
            long[] amounts = { delta.Food, delta.Resource, delta.Combat, delta.Energy };
            int visibleCount = 0;
            for (int i = 0; i < amounts.Length; i++)
            {
                bool visible = amounts[i] != 0;
                icons[i].gameObject.SetActive(visible);
                values[i].gameObject.SetActive(visible);
                if (!visible) continue;
                // 只把有變化的項目往左排列，避免留下空洞。
                icons[i].rectTransform.anchoredPosition = new Vector2(32f + visibleCount * 205f, -83f);
                values[i].rectTransform.anchoredPosition = new Vector2(126f + visibleCount * 205f, -83f);
                values[i].text = amounts[i] > 0 ? $"+{amounts[i]}" : $"−{-amounts[i]}";
                values[i].color = amounts[i] > 0 ? new Color32(60, 90, 48, 255) : new Color32(137, 42, 35, 255);
                visibleCount++;
            }
            if (heading != null) heading.text = visibleCount == 0
                ? "本次核心數值沒有淨變化" : "本次淨變化（含支付）";
            return true;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
