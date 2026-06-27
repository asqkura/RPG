using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Event", fileName = "EventData")]
    public sealed class EventData : MasterDataAsset
    {
        [SerializeField] private EventType eventType;
        [SerializeField] private ConditionGroup conditions = new();
        [SerializeField] private EventPriorityCategory priorityCategory = EventPriorityCategory.Normal;
        [Min(0)]
        [SerializeField] private int weight = 1;
        [SerializeField] private List<EventStep> steps = new();
        [SerializeField] private List<EventEffect> effects = new();
        [SerializeField] private string nextEventId = string.Empty;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private string bgmId = string.Empty;

        public string EventId => Id;
        public EventType EventType => eventType;
        public ConditionGroup Conditions => conditions;
        public EventPriorityCategory PriorityCategory => priorityCategory;
        public int Weight => weight;
        public IReadOnlyList<EventStep> Steps => steps;
        public IReadOnlyList<EventEffect> Effects => effects;
        public string NextEventId => nextEventId;
        public Sprite BackgroundSprite => backgroundSprite;
        public string BgmId => bgmId;
    }
}
