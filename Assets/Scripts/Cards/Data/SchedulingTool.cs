using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "SchedulingToolCard", menuName = "Zombie Card Survive/Cards/Scheduling Tool Card")]
    public class SchedulingTool : CardBase
    {
        [Header("Scheduling")]
        [SerializeField] private int schedulingToolAmount = 1;
        [SerializeField] private CardBase restCardData;
        [SerializeField] private CardSelectionFilterData selectionFilter = new CardSelectionFilterData();

        public override void Resolve(CardPlayContext context)
        {

            context.Controller.BeginDispatchValidDrawPileCard(
                restCardData,
                schedulingToolAmount,
                selectionFilter);

            base.Resolve(context);
        }
    }
}
