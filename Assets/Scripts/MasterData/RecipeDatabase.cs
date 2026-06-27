using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Database/Recipe Database", fileName = "RecipeDatabase")]
    public sealed class RecipeDatabase : MasterDatabase<RecipeData>
    {
    }
}
