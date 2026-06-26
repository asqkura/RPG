using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Database/Skill Database", fileName = "SkillDatabase")]
    public sealed class SkillDatabase : MasterDatabase<SkillData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : MasterDatabase<ItemData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Equipment Database", fileName = "EquipmentDatabase")]
    public sealed class EquipmentDatabase : MasterDatabase<EquipmentData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Enemy Database", fileName = "EnemyDatabase")]
    public sealed class EnemyDatabase : MasterDatabase<EnemyData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Battle Database", fileName = "BattleDatabase")]
    public sealed class BattleDatabase : MasterDatabase<BattleData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Quest Database", fileName = "QuestDatabase")]
    public sealed class QuestDatabase : MasterDatabase<QuestData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Event Database", fileName = "EventDatabase")]
    public sealed class EventDatabase : MasterDatabase<EventData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Wander Location Database", fileName = "WanderLocationDatabase")]
    public sealed class WanderLocationDatabase : MasterDatabase<WanderLocationData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Character Database", fileName = "CharacterDatabase")]
    public sealed class CharacterDatabase : MasterDatabase<CharacterData>
    {
    }

    [CreateAssetMenu(menuName = "RPG/Master Database/Recipe Database", fileName = "RecipeDatabase")]
    public sealed class RecipeDatabase : MasterDatabase<RecipeData>
    {
    }
}
