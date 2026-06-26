using System;
using UnityEngine;

namespace RPG.MasterData
{
    public interface IMasterData
    {
        string Id { get; }
    }

    public abstract class MasterDataAsset : ScriptableObject, IMasterData
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [TextArea]
        [SerializeField] private string description = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;

        protected virtual void OnValidate()
        {
            id = NormalizeId(id);
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public struct MasterDataValidationIssue
    {
        [SerializeField] private string id;
        [SerializeField] private string message;

        public MasterDataValidationIssue(string id, string message)
        {
            this.id = id;
            this.message = message;
        }

        public string Id => id;
        public string Message => message;
    }
}
