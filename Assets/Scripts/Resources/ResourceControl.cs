using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Resources
{
    public static class ResourceControl
    {
        public static int Food => FoodSystem.Food;
        public static int Resource => ResourceSystem.Resource;

        public static int GetFood()
        {
            return FoodSystem.GetFood();
        }

        public static int GetResource()
        {
            return ResourceSystem.GetResource();
        }

        public static void AddFood(int amount)
        {
            FoodSystem.AddFood(Mathf.Max(0, amount));
        }

        public static bool SpendFood(int amount)
        {
            return FoodSystem.SpendFood(amount);
        }

        public static void AddResource(int amount)
        {
            ResourceSystem.AddResource(Mathf.Max(0, amount));
        }

        public static bool SpendResource(int amount)
        {
            return ResourceSystem.SpendResource(amount);
        }

        public static void ResetResources(int food = 0, int resource = 0)
        {
            FoodSystem.Reset(food);
            ResourceSystem.Reset(resource);
        }
    }
}
