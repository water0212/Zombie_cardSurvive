using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class MoraleSystem
    {
        private static int morale = 70;
        private static int defaultMorale = 70;
        private static int maxMorale = 100;
        private static int highMoraleDecayPerRound = 5;

        public static int Morale => morale;
        public static int DefaultMorale => defaultMorale;
        public static int MaxMorale => maxMorale;
        public static int HighMoraleDecayPerRound => highMoraleDecayPerRound;

        public static void Reset(
            int startingMorale = 70,
            int defaultValue = 70,
            int maxValue = 100,
            int highDecayPerRound = 5)
        {
            maxMorale = Mathf.Max(1, maxValue);
            defaultMorale = Mathf.Clamp(defaultValue, 0, maxMorale);
            morale = Mathf.Clamp(startingMorale, 0, maxMorale);
            highMoraleDecayPerRound = Mathf.Max(0, highDecayPerRound);
        }

        public static int GetMorale()
        {
            return morale;
        }

        public static void AddMorale(int amount)
        {
            morale = Mathf.Clamp(morale + Mathf.Max(0, amount), 0, maxMorale);
        }

        public static void ReduceMorale(int amount)
        {
            morale = Mathf.Clamp(morale - Mathf.Max(0, amount), 0, maxMorale);
        }

        public static void ChangeMorale(int amount)
        {
            morale = Mathf.Clamp(morale + amount, 0, maxMorale);
        }

        public static int ApplyRoundEndDrift()
        {
            if (morale <= defaultMorale || highMoraleDecayPerRound <= 0)
            {
                return 0;
            }

            int before = morale;
            morale = Mathf.Max(defaultMorale, morale - highMoraleDecayPerRound);
            return morale - before;
        }
    }
}
