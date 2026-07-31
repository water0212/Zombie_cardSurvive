using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieCardSurvive.Cards.UI
{
    /// <summary>
    /// 打牌成功後的視覺演出控制器。
    /// 它只處理「複製出來的 UI 替身卡」，不改變真正的牌堆與卡牌 Runtime。
    /// </summary>
    public class CardPlayEffectController : MonoBehaviour
    {
        // 替身卡播放時會被搬到這一層，避免影響原本手牌排列與互動。
        [SerializeField] private RectTransform effectLayer;
        // 替身卡飛向的目標位置，通常設在出牌區或畫面中央。
        [SerializeField] private RectTransform targetPoint;
        // 整段打牌動畫時間，單位為秒。
        [SerializeField] private float duration = 0.45f;
        // 動畫結束時的縮放倍率。
        [SerializeField] private float targetScale = 0.75f;
        // 動畫結束時的 Z 軸旋轉角度。
        [SerializeField] private float targetRotation = 16f;
        // 沒有設定 targetPoint 時，從起點偏移出的備援目標位置。
        [SerializeField] private Vector2 fallbackTargetOffset = new Vector2(0f, 180f);
        // 控制位置、縮放與旋轉的時間曲線。
        [SerializeField] private AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        // 控制 CanvasGroup 透明度的時間曲線。
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Optional Material Effect")]
        // 選填的 UI 材質特效；會在播放時複製成 Runtime Material，避免多張牌共用狀態。
        [SerializeField] private Material effectMaterial;
        // 材質上代表特效進度的 float 屬性名稱，例如 _Fade 或 _DissolveAmount。
        [SerializeField] private string effectProgressProperty = "_Fade";

        /// <summary>
        /// 外部建立替身卡時建議使用的父節點；未指定 effectLayer 時會退回本物件 Transform。
        /// </summary>
        public Transform EffectParent => effectLayer != null ? effectLayer : transform;

        /// <summary>
        /// 播放一張 UI 替身卡的出牌演出。動畫結束後會自動銷毀替身卡。
        /// </summary>
        /// <param name="effectVisual">要播放的卡牌視覺複製物件。</param>
        public void Play(RectTransform effectVisual)
        {
            if (effectVisual == null)
            {
                return;
            }

            if (effectLayer != null && effectVisual.parent != effectLayer)
            {
                effectVisual.SetParent(effectLayer, true);
            }

            StartCoroutine(PlayRoutine(effectVisual));
        }

        // 實際動畫 Coroutine：移動、縮放、旋轉、淡出與材質進度都在這裡逐幀推進。
        private IEnumerator PlayRoutine(RectTransform effectVisual)
        {
            CanvasGroup canvasGroup = effectVisual.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = effectVisual.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            List<Material> runtimeMaterials = ApplyRuntimeMaterial(effectVisual);
            bool canAnimateMaterial = CanAnimateMaterial(runtimeMaterials);

            Vector2 startPosition = effectVisual.anchoredPosition;
            Vector2 endPosition = ResolveTargetPosition(effectVisual, startPosition);
            Vector3 startScale = effectVisual.localScale;
            Vector3 endScale = Vector3.one * Mathf.Max(0.01f, targetScale);
            Quaternion startRotation = effectVisual.localRotation;
            Quaternion endRotation = Quaternion.Euler(0f, 0f, targetRotation);
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration && effectVisual != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = EvaluateCurve(motionCurve, t);

                effectVisual.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                effectVisual.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                effectVisual.localRotation = Quaternion.Lerp(startRotation, endRotation, eased);
                canvasGroup.alpha = EvaluateCurve(alphaCurve, t);

                if (canAnimateMaterial)
                {
                    SetMaterialProgress(runtimeMaterials, t);
                }

                yield return null;
            }

            CleanupMaterials(runtimeMaterials);

            if (effectVisual != null)
            {
                Destroy(effectVisual.gameObject);
            }
        }

        // 將選填材質複製成 Runtime Material 後套到替身卡 Image，避免多張牌互相污染材質進度。
        private List<Material> ApplyRuntimeMaterial(RectTransform effectVisual)
        {
            List<Material> runtimeMaterials = new List<Material>();
            if (effectMaterial == null || effectVisual == null)
            {
                return runtimeMaterials;
            }

            Image[] images = effectVisual.GetComponentsInChildren<Image>(true);
            if (images.Length == 0)
            {
                return runtimeMaterials;
            }

            Material runtimeMaterial = new Material(effectMaterial);
            runtimeMaterials.Add(runtimeMaterial);

            foreach (Image image in images)
            {
                if (image != null)
                {
                    image.material = runtimeMaterial;
                }
            }

            return runtimeMaterials;
        }

        // 確認目前材質是否支援指定的進度欄位，不支援就只播放基礎 UI 動畫。
        private bool CanAnimateMaterial(List<Material> runtimeMaterials)
        {
            if (runtimeMaterials == null || runtimeMaterials.Count == 0 || string.IsNullOrEmpty(effectProgressProperty))
            {
                return false;
            }

            return runtimeMaterials[0] != null && runtimeMaterials[0].HasProperty(effectProgressProperty);
        }

        // 將動畫進度寫進材質，例如控制溶解、燃燒或閃光程度。
        private void SetMaterialProgress(List<Material> runtimeMaterials, float progress)
        {
            foreach (Material material in runtimeMaterials)
            {
                if (material != null)
                {
                    material.SetFloat(effectProgressProperty, progress);
                }
            }
        }

        // 銷毀播放時臨時建立的材質，避免 Play Mode 中累積記憶體。
        private void CleanupMaterials(List<Material> runtimeMaterials)
        {
            foreach (Material material in runtimeMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        // 將 targetPoint 的世界位置轉成 effectLayer 底下的 anchoredPosition。
        private Vector2 ResolveTargetPosition(RectTransform effectVisual, Vector2 startPosition)
        {
            RectTransform parentRect = effectVisual.parent as RectTransform;
            if (parentRect == null || targetPoint == null)
            {
                return startPosition + fallbackTargetOffset;
            }

            Canvas canvas = parentRect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, targetPoint.position);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out Vector2 localPoint))
            {
                return localPoint;
            }

            return startPosition + fallbackTargetOffset;
        }

        // 曲線為空時退回線性進度，讓 Inspector 沒填曲線也能播放。
        private static float EvaluateCurve(AnimationCurve curve, float t)
        {
            return curve != null ? curve.Evaluate(t) : t;
        }
    }
}
