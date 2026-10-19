using TMPro;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>殭屍世界圖像；只顯示最大血量與攻擊，點擊血量區切換擊殺配置。</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ZombieThreatSpriteView : MonoBehaviour, IWorldInteractionTarget
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer selection;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text killMarkText;
        [SerializeField] private TMP_Text detailText;
        private ZombieThreatRuntime runtime;

        public int InteractionPriority => 20;
        public bool CanInteract => runtime != null && isActiveAndEnabled;
        public GameObject InteractionObject => gameObject;
        public ZombieThreatRuntime Runtime => runtime;

        public void Configure(SpriteRenderer bodyRenderer, SpriteRenderer selectionRenderer, TMP_Text health, TMP_Text attack, TMP_Text killMark, TMP_Text details)
        {
            body = bodyRenderer;
            selection = selectionRenderer;
            healthText = health;
            attackText = attack;
            killMarkText = killMark;
            detailText = details;
            SetHovered(false);
        }

        public void Bind(ZombieThreatRuntime value)
        {
            runtime = value;
            Refresh();
        }

        public void Bind(ZombieThreatRuntime value, ZombieWorldVisualProfile profile)
        {
            runtime = value;
            if (profile != null)
            {
                if (body != null && profile.WorldSprite != null)
                {
                    body.sprite = profile.WorldSprite;
                }
                transform.localScale = Vector3.one * profile.WorldScale;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (runtime == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (runtime.Artwork != null && body != null)
            {
                body.sprite = runtime.Artwork;
            }
            if (healthText != null)
            {
                healthText.text = $"HP {runtime.MaxHealth}";
            }
            if (attackText != null)
            {
                attackText.text = $"攻 {runtime.Attack}";
            }
            if (killMarkText != null)
            {
                killMarkText.text = runtime.IsMarkedForKill ? "✓ 擊殺" : string.Empty;
            }
        }

        public void SetHovered(bool hovered)
        {
            if (selection != null)
            {
                selection.enabled = hovered;
            }
        }

        public void OnPrimaryInteract()
        {
            if (runtime == null)
            {
                return;
            }

            bool changed = CombatSystem.TryToggleZombieKillMark(runtime);
            Refresh();
            if (!changed && detailText != null)
            {
                detailText.text = $"戰鬥值不足：擊殺 {runtime.DisplayName} 需要 {runtime.MaxHealth}";
            }
        }

        public void OnSecondaryInteract()
        {
            if (runtime != null && detailText != null)
            {
                detailText.text = $"{runtime.DisplayName}\n最大血量 {runtime.MaxHealth}｜攻擊 {runtime.Attack}\n{runtime.NegativeEffectDescription}";
            }
        }
    }
}
