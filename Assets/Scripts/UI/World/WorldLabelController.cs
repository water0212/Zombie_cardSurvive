using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>把所有世界標籤集中投影到同一個 Screen Space Canvas。</summary>
    public sealed class WorldLabelController : MonoBehaviour
    {
        private sealed class Binding
        {
            public Transform Anchor;
            public RectTransform Label;
            public Vector3 WorldOffset;
        }

        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform labelRoot;
        private readonly Dictionary<int, Binding> bindings = new Dictionary<int, Binding>();

        public void Configure(Camera cameraValue, RectTransform root)
        {
            worldCamera = cameraValue;
            labelRoot = root;
        }

        public void Bind(int runtimeId, Transform anchor, RectTransform label, Vector3 worldOffset)
        {
            if (anchor == null || label == null)
            {
                return;
            }

            if (labelRoot != null)
            {
                label.SetParent(labelRoot, false);
            }

            bindings[runtimeId] = new Binding
            {
                Anchor = anchor,
                Label = label,
                WorldOffset = worldOffset
            };
        }

        public void Unbind(int runtimeId)
        {
            bindings.Remove(runtimeId);
        }

        private void LateUpdate()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            foreach (Binding binding in bindings.Values)
            {
                if (binding.Anchor == null || binding.Label == null)
                {
                    continue;
                }

                Vector3 screen = worldCamera.WorldToScreenPoint(binding.Anchor.position + binding.WorldOffset);
                bool visible = screen.z > 0f;
                binding.Label.gameObject.SetActive(visible);
                if (visible)
                {
                    binding.Label.position = screen;
                }
            }
        }
    }
}
