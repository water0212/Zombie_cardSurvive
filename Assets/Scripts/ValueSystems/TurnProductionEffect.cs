using System;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    [Serializable]
    public class TurnProductionEffect
    {
        [SerializeField] private string id;
        [SerializeField] private int amountPerTurn;
        [SerializeField] private int remainingTurns;
        [SerializeField] private bool isPermanent;

        public TurnProductionEffect(string id, int amountPerTurn, bool isPermanent = true, int remainingTurns = 0)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            this.amountPerTurn = amountPerTurn;
            this.isPermanent = isPermanent;
            this.remainingTurns = Mathf.Max(0, remainingTurns);
        }

        public string Id => id;
        public int AmountPerTurn => amountPerTurn;
        public int RemainingTurns => remainingTurns;
        public bool IsPermanent => isPermanent;
        public bool IsExpired => !isPermanent && remainingTurns <= 0;

        public void Tick()
        {
            if (isPermanent)
            {
                return;
            }

            remainingTurns = Mathf.Max(0, remainingTurns - 1);
        }
    }
}
