using System;
using UnityEngine;

namespace ZombieCardSurvive.Buildings.Data
{
    [Serializable]
    public struct ResourceCost
    {
        [SerializeField] private int food;
        [SerializeField] private int resource;

        public ResourceCost(int food, int resource)
        {
            this.food = Mathf.Max(0, food);
            this.resource = Mathf.Max(0, resource);
        }

        public int Food => Mathf.Max(0, food);
        public int Resource => Mathf.Max(0, resource);
        public bool IsEmpty => Food == 0 && Resource == 0;
    }
}
