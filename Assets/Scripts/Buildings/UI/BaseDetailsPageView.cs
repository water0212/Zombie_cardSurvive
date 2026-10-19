using TMPro;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Buildings.UI
{
    public sealed class BaseDetailsPageView : BuildingInformationPageView
    {
        [SerializeField] private TMP_Text detailText;
        private bool bound;
        public override void Bind(BuildingSystem owner, BuildingRuntime building)
        {
            Unbind();
            bound = building != null;
            if (bound)
            {
                FoodSystem.StateChanged += Refresh;
                ResourceReservationService.StateChanged += Refresh;
                ResourceSystem.StateChanged += Refresh;
                EnergySystem.StateChanged += Refresh;
                CombatSystem.StateChanged += Refresh;
                MoraleSystem.StateChanged += OnMoraleChanged;
            }
            Refresh();
        }
        public override void Refresh()
        {
            if (!bound || detailText == null) return;
            detailText.text = $"基地詳情（目前）\n\n士氣  {MoraleSystem.CurrentMorale} / {MoraleSystem.MaximumMorale}\n"
                + $"食物  {FoodSystem.Food} / {FoodSystem.StorageCapacity}\n臨時食物  {FoodSystem.TemporaryFood}　保留食物  {FoodSystem.ReservedFood}\n"
                + $"資源  {ResourceSystem.Resource}　保留資源  {ResourceSystem.ReservedResource}\n"
                + $"戰鬥值  {CombatSystem.CombatValue} / {CombatSystem.MaxCombatValue}\n"
                + $"能量  {EnergySystem.CurrentEnergy} / {EnergySystem.MaxEnergy}\n場上殭屍  {CombatSystem.ZombieThreatCount}";
        }
        private void OnMoraleChanged(MoraleChangeResult result) { Refresh(); }
        public override void Unbind()
        {
            if (!bound) return;
            FoodSystem.StateChanged -= Refresh;
            ResourceReservationService.StateChanged -= Refresh;
            ResourceSystem.StateChanged -= Refresh;
            EnergySystem.StateChanged -= Refresh;
            CombatSystem.StateChanged -= Refresh;
            MoraleSystem.StateChanged -= OnMoraleChanged;
            bound = false;
        }
    }
}
