using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Database/Synthesis Level Up Requirement Database", fileName = "SynthesisLevelUpRequirementDatabase")]
    public sealed class SynthesisLevelUpRequirementDatabase : MasterDatabase<SynthesisLevelUpRequirementData>
    {
        public bool TryGetByCurrentLevel(int currentLevel, out SynthesisLevelUpRequirementData requirement)
        {
            foreach (var entry in Entries)
            {
                if (entry != null && entry.CurrentLevel == currentLevel)
                {
                    requirement = entry;
                    return true;
                }
            }

            requirement = null;
            return false;
        }
    }
}
