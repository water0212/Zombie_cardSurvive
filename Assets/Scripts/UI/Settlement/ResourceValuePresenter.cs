using System;
using TMPro;
using UnityEngine;
using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.UI.Settlement
{
    public interface IValuePresentation
    {
        ValueId Id { get; }
        void Present(ValueTransactionResult transaction, float progress);
        void Present(ValueChangeResult result, float progress, ValueSnapshot context);
        void Synchronize(ValueSnapshot snapshot);
    }

    /// <summary>The same local numeric animation is used by cards, events and round end.</summary>
    public sealed class ResourceValuePresenter : MonoBehaviour, IValuePresentation
    {
        [SerializeField] private ValueId valueId;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text deltaText;
        [Header("Delta readability")]
        [SerializeField, Min(12f)] private float deltaFontSize = 34f;
        [SerializeField] private Vector2 deltaSize = new Vector2(180f, 54f);
        [SerializeField] private Vector2 deltaOffset = new Vector2(0f, 4f);
        private Color normalColour;
        private bool hasNormalColour;
        private bool deltaStyleApplied;
        private void Awake() { CaptureNormalColour(); ApplyDeltaStyle(); }
        private void ApplyDeltaStyle()
        {
            if (deltaStyleApplied || deltaText == null) return;
            deltaText.enableAutoSizing = false;
            deltaText.fontSize = deltaFontSize;
            deltaText.fontStyle |= FontStyles.Bold;
            deltaText.enableWordWrapping = false;
            deltaText.alignment = TextAlignmentOptions.Center;
            deltaText.overflowMode = TextOverflowModes.Overflow;
            var rect = deltaText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = deltaOffset;
            rect.sizeDelta = deltaSize;
            if (deltaText.font != null && deltaText.fontSharedMaterial != null)
            {
                deltaText.outlineColor = new Color32(20, 22, 18, 255);
                deltaText.outlineWidth = .18f;
            }
            deltaStyleApplied = true;
        }
        private void CaptureNormalColour()
        {
            if (hasNormalColour || valueText == null) return;
            normalColour = valueText.color;
            hasNormalColour = true;
        }
        public ValueId Id => valueId;
        public void Configure(ValueId id, TMP_Text text, TMP_Text delta)
        {
            valueId = id; valueText = text; deltaText = delta;
            hasNormalColour = false;
            CaptureNormalColour();
            deltaStyleApplied = false;
            ApplyDeltaStyle();
        }
        public void Present(ValueTransactionResult transaction, float progress)
        {
            var result=transaction.Net(Id);
            Present(result,progress,transaction.After);
            if(Id != ValueId.Food)return;
            var temporary=transaction.Net(ValueId.TemporaryFood);
            int current=progress>=1f ? result.After : (int)Math.Round(result.Before+result.Delta*(double)Mathf.Clamp01(progress));
            int currentTemporary=progress>=1f ? temporary.After : (int)Math.Round(temporary.Before+temporary.Delta*(double)Mathf.Clamp01(progress));
            Show(current,transaction.After,currentTemporary);
            ShowDelta(result.Delta+temporary.Delta);
        }
        public void Present(ValueChangeResult result, float progress, ValueSnapshot context)
        {
            int display = Id == ValueId.Energy|| progress >= 1f ? result.After : (int)Math.Round(result.Before + result.Delta * (double)Mathf.Clamp01(progress));
            Show(display, context);
            ShowDelta(result.Delta);
        }
        private void ShowDelta(long delta)
        {
            if (deltaText != null)
            {
                ApplyDeltaStyle();
                deltaText.text = delta == 0 ? string.Empty : delta.ToString("+#;-#;0");
                deltaText.color = delta < 0 ? new Color32(255, 124, 108, 255) : new Color32(158, 228, 160, 255);
            }
        }
        public void PresentLive(int displayedValue, int displayedTemporaryFood, ValueSnapshot target, long delta, float progress)
        {
            int value = Id == ValueId.Energy? target.Get(Id): displayedValue;

            Show(value, target, displayedTemporaryFood);
            ShowDelta(delta);
            if (deltaText != null)
            {
                var colour = deltaText.color;
                colour.a = 1f - Mathf.Clamp01(progress);
                deltaText.color = colour;
            }
        }
        public void Synchronize(ValueSnapshot snapshot)
        { Show(snapshot.Get(Id), snapshot); if (deltaText != null) deltaText.text = string.Empty; }
        private void Show(int value, ValueSnapshot snapshot, int? temporaryOverride = null)
        {
            if (valueText == null) return;
            // A different presenter's OnEnable may synchronize us before our own Awake.
            CaptureNormalColour();
            valueText.color = normalColour;
            switch (Id)
            {
                case ValueId.Food:
                    valueText.text = $"{value}/{snapshot.Get(ValueId.FoodCapacity)}"
                        + ((temporaryOverride ?? snapshot.Get(ValueId.TemporaryFood)) > 0 ? $" +{temporaryOverride ?? snapshot.Get(ValueId.TemporaryFood)}臨時" : "")
                        + (snapshot.Get(ValueId.ReservedFood) > 0 ? $" 鎖{snapshot.Get(ValueId.ReservedFood)}" : ""); break;
                case ValueId.Resource:
                    valueText.text = $"{Math.Max(0, value - snapshot.Get(ValueId.ReservedResource))}"
                        + (snapshot.Get(ValueId.ReservedResource) > 0 ? $" 鎖{snapshot.Get(ValueId.ReservedResource)}" : ""); break;
                case ValueId.Energy: valueText.text = $"{value}/{snapshot.Get(ValueId.MaximumEnergy)}"; break;
                case ValueId.Combat: valueText.text = $"{value}/{snapshot.Get(ValueId.MaximumCombat)}"; break;
                default: valueText.text = value.ToString(); break;
            }
        }
        [ContextMenu("Update Style")]
        private void UpdateStyle()
        {
            deltaStyleApplied = false;
            ApplyDeltaStyle();
        }
    }
}
