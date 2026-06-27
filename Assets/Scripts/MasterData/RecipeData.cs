using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Recipe", fileName = "RecipeData")]
    public sealed class RecipeData : MasterDataAsset
    {
        [SerializeField] private RecipeDataType recipeType;
        [Min(1)]
        [SerializeField] private int requiredSynthesisLevel = 1;
        [SerializeField] private List<RecipeIngredientData> ingredients = new();
        [Min(0)]
        [SerializeField] private int cost;
        [SerializeField] private RecipeResultDataType resultType;
        [SerializeField] private string resultItemId = string.Empty;
        [SerializeField] private int sortOrder;

        public string RecipeId => Id;
        public RecipeDataType RecipeType => recipeType;
        public int RequiredSynthesisLevel => requiredSynthesisLevel;
        public IReadOnlyList<RecipeIngredientData> Ingredients => ingredients;
        public int Cost => cost;
        public RecipeResultDataType ResultType => resultType;
        public string ResultItemId => resultItemId;
        public int SortOrder => sortOrder;
    }
}
