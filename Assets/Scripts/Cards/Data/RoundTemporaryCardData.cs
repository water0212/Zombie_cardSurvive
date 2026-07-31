using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "RoundTemporaryCard", menuName = "Zombie Card Survive/Cards/Round Temporary Card")]
    public class RoundTemporaryCardData : CardBase
    {
        [Header("Round Temporary Card Settings")]
        [SerializeField] private bool expiresBeforeNextRound = true;
        [SerializeField] private bool createsExhaustionRefillRequest = false;

        public override bool ExpiresBeforeNextRound => expiresBeforeNextRound;
        public override bool CreatesExhaustionRefillRequest => createsExhaustionRefillRequest;

    }
}