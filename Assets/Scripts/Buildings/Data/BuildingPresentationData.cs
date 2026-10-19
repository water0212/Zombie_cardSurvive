using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Buildings.UI;

namespace ZombieCardSurvive.Buildings.Data
{
    [Serializable]
    public sealed class BuildingPageDefinition
    {
        [SerializeField] private string pageId;
        [SerializeField] private string title;
        [SerializeField] private int order;
        [SerializeField] private bool isPublic = true;
        [SerializeField] private BuildingInteractionData interaction;
        [SerializeField] private BuildingInformationPageView informationPagePrefab;
        public string PageId => pageId;
        public string Title => title;
        public int Order => order;
        public bool IsPublic => isPublic;
        public BuildingInteractionData Interaction => interaction;
        public BuildingInformationPageView InformationPagePrefab => informationPagePrefab;
        public bool HasSingleSource => (interaction != null) != (informationPagePrefab != null);
    }

    // Presentation metadata only; never owns price, production, progress or unlock state.
    [CreateAssetMenu(fileName = "BuildingPresentation", menuName = "Zombie Card Survive/Buildings/Presentation")]
    public sealed class BuildingPresentationData : ScriptableObject
    {
        [SerializeField] private List<BuildingPageDefinition> pages = new List<BuildingPageDefinition>();
        public IReadOnlyList<BuildingPageDefinition> Pages => pages;

        private void OnValidate()
        {
            var ids = new HashSet<string>();
            if (pages == null) return;
            foreach (var page in pages)
                if (page == null || string.IsNullOrWhiteSpace(page.PageId) || !ids.Add(page.PageId) || !page.HasSingleSource)
                    Debug.LogError("建築頁面配置需要唯一且非空的 Page ID，並且只指定一個互動或資訊頁。", this);
        }
    }
}
