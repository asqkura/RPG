using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Database/Character Database", fileName = "CharacterDatabase")]
    public sealed class CharacterDatabase : MasterDatabase<CharacterData>
    {
    }
}
