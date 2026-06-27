using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Database/Battle Database", fileName = "BattleDatabase")]
    public sealed class BattleDatabase : MasterDatabase<BattleData>
    {
    }
}
