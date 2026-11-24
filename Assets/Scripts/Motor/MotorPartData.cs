using UnityEngine;

namespace WegInside
{
    [CreateAssetMenu(menuName = "WEG Inside/Informações de componente")]
    public sealed class MotorPartData : ScriptableObject
    {
        [SerializeField] string id, group, title, category;
        [SerializeField, TextArea] string description, function, importance;
        public string Id => id;
        public string Group => group;
        public string Title => title;
        public string Category => category;
        public string Description => description;
        public string Function => function;
        public string Importance => importance;

        public void Configure(string identifier, string groupName, string displayName, string family,
            string summary, string purpose, string significance)
        {
            id = identifier;
            group = groupName;
            title = displayName;
            category = family;
            description = summary;
            function = purpose;
            importance = significance;
        }
    }
}
