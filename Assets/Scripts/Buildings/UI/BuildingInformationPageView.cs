using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Buildings.UI
{
    public abstract class BuildingInformationPageView : MonoBehaviour
    {
        public abstract void Bind(BuildingSystem owner, BuildingRuntime building);
        public abstract void Refresh();
        public abstract void Unbind();
        protected virtual void OnDestroy() { Unbind(); }
    }
}
