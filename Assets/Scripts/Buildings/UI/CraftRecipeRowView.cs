using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
namespace ZombieCardSurvive.Buildings.UI
{
    public class CraftRecipeRowView : MonoBehaviour
    {
        [SerializeField] private Button expandButton, addButton;
        [SerializeField] private TMP_Text titleText, detailText, reasonText;
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image artwork;
        private CardCraftRecipeData recipe;
        private Action<CardCraftRecipeData> expand, enqueue;
        public void Bind(CardCraftRecipeData value, Action<CardCraftRecipeData> onExpand, Action<CardCraftRecipeData> onEnqueue)
        {
            if (expandButton != null) expandButton.onClick.RemoveListener(Expand);
            if (addButton != null) addButton.onClick.RemoveListener(Add);
            recipe = value; expand = onExpand; enqueue = onEnqueue;
            if (expandButton != null) expandButton.onClick.AddListener(Expand);
            if (addButton != null) addButton.onClick.AddListener(Add);
        }
        public void Refresh(bool expanded, string reason)
        {
            if (recipe == null) return;
            if (titleText != null) titleText.text = (expanded ? "▼ " : "▶ ") + recipe.DisplayName;
            if (detailRoot != null) detailRoot.SetActive(expanded);
            if (detailText != null) detailText.text = $"產出：{(recipe.OutputCard != null ? recipe.OutputCard.DisplayName : "未設定卡牌")} ×{recipe.OutputCount}\n{recipe.OutputCard?.Description}\n食物 {recipe.Cost.Food}　資源 {recipe.Cost.Resource}\n所需製造力 {recipe.RequiredProduction}";
            if (reasonText != null) { reasonText.text = reason; reasonText.gameObject.SetActive(!string.IsNullOrEmpty(reason)); }
            if (addButton != null) addButton.interactable = string.IsNullOrEmpty(reason);
            if (artwork != null) { artwork.sprite = recipe.Artwork != null ? recipe.Artwork : recipe.OutputCard?.Artwork; artwork.gameObject.SetActive(artwork.sprite != null); }
        }
        private void Expand() { expand?.Invoke(recipe); }
        private void Add() { enqueue?.Invoke(recipe); }
        private void OnDestroy()
        { if (expandButton != null) expandButton.onClick.RemoveListener(Expand); if (addButton != null) addButton.onClick.RemoveListener(Add); }
    }
}
