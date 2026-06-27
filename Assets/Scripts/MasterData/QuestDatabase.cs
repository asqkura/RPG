using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Database/Quest Database", fileName = "QuestDatabase")]
    public sealed class QuestDatabase : MasterDatabase<QuestData>
    {
    }
}
