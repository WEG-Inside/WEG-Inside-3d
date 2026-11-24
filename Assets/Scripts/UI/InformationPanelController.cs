using UnityEngine.UIElements;

namespace WegInside
{
    sealed class InformationPanelController
    {
        readonly VisualElement intro, details;
        readonly Label name, category, description, function, importance;
        readonly Button isolation, focus;
        MotorPart current;
        bool initialized;

        public InformationPanelController(VisualElement root, MotorController motor)
        {
            intro = root.Q("intro");
            details = root.Q("details");
            name = root.Q<Label>("part-name");
            category = root.Q<Label>("part-category");
            description = root.Q<Label>("part-description");
            function = root.Q<Label>("part-function");
            importance = root.Q<Label>("part-importance");
            isolation = root.Q<Button>("isolate");
            focus = root.Q<Button>("focus");
            isolation.clicked += motor.ToggleIsolation;
            focus.clicked += motor.FocusSelection;
            root.Q<Button>("clear").clicked += () => motor.Select(null);
        }

        public void Render(MotorPart part, bool isolated)
        {
            if (!initialized || current != part)
            {
                initialized = true;
                current = part;
                intro.style.display = part ? DisplayStyle.None : DisplayStyle.Flex;
                details.style.display = part ? DisplayStyle.Flex : DisplayStyle.None;
                if (part)
                {
                    var data = part.Data;
                    name.text = data.Title;
                    category.text = data.Category;
                    description.text = data.Description;
                    function.text = data.Function;
                    importance.text = data.Importance;
                }
            }
            if (part)
            {
                isolation.text = isolated ? "Sair do isolamento" : "Isolar componente";
                isolation.EnableInClassList("active-control",isolated);
                focus.SetEnabled(part.IsVisible);
            }
        }
    }
}
