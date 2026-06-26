using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    public abstract class MasterDatabase<TData> : ScriptableObject where TData : MasterDataAsset
    {
        [SerializeField] private List<TData> entries = new();

        private Dictionary<string, TData> byId;

        public IReadOnlyList<TData> Entries => entries;

        public bool TryGetById(string id, out TData data)
        {
            EnsureIndex();
            return byId.TryGetValue(id, out data);
        }

        public TData GetById(string id)
        {
            if (TryGetById(id, out var data))
            {
                return data;
            }

            throw new KeyNotFoundException($"{typeof(TData).Name} id '{id}' is not registered.");
        }

        public List<MasterDataValidationIssue> ValidateEntries()
        {
            var issues = new List<MasterDataValidationIssue>();
            var seenIds = new HashSet<string>();

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    issues.Add(new MasterDataValidationIssue(string.Empty, $"{typeof(TData).Name} has a null entry."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    issues.Add(new MasterDataValidationIssue(string.Empty, $"{entry.name} has an empty id."));
                    continue;
                }

                if (!seenIds.Add(entry.Id))
                {
                    issues.Add(new MasterDataValidationIssue(entry.Id, $"{typeof(TData).Name} id '{entry.Id}' is duplicated."));
                }
            }

            return issues;
        }

        protected virtual void OnValidate()
        {
            byId = null;
        }

        private void EnsureIndex()
        {
            if (byId != null)
            {
                return;
            }

            byId = new Dictionary<string, TData>();

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                if (!byId.ContainsKey(entry.Id))
                {
                    byId.Add(entry.Id, entry);
                }
            }
        }
    }
}
