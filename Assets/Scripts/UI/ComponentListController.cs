using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WegInside
{
    sealed class ComponentListController
    {
        readonly Dictionary<MotorPart, Button> rows = new();
        readonly Dictionary<MotorPart, Button> groupRows = new();
        readonly Dictionary<MotorPart, VisualElement> groupContents = new();
        readonly Dictionary<Button, string> groupTitles = new();

        public ComponentListController(VisualElement host, MotorController motor)
        {
            var groups = new Dictionary<string,List<MotorPart>>();
            foreach (var part in motor.Parts)
            {
                string group = part.Data.Group;
                if (!groups.TryGetValue(group,out var members))
                {
                    members = new List<MotorPart>();
                    groups.Add(group,members);
                }
                members.Add(part);
            }
            foreach (var group in groups)
            {
                if (group.Value.Count == 1)
                {
                    AddPart(host,motor,group.Value[0],false);
                    continue;
                }
                var members = group.Value;
                var groupButton = new Button(() =>
                {
                    MotorPart next = motor.Selected && members.Contains(motor.Selected) ? null : members[0];
                    motor.Select(next);
                }) { text = group.Key+"   +" };
                groupButton.AddToClassList("component");
                host.Add(groupButton);
                var itemHost = new VisualElement();
                itemHost.AddToClassList("subcomponents");
                host.Add(itemHost);
                groupTitles.Add(groupButton,group.Key);
                foreach (var part in members)
                {
                    groupRows.Add(part,groupButton);
                    groupContents.Add(part,itemHost);
                    AddPart(itemHost,motor,part,true);
                }
            }
        }

        void AddPart(VisualElement host, MotorController motor, MotorPart part, bool nested)
        {
            var button = new Button(() => motor.Select(part)) { text = part.Data.Title, name = "part-"+part.Data.Id };
            button.AddToClassList("component");
            if (nested) button.AddToClassList("subcomponent");
            rows.Add(part,button);
            host.Add(button);
        }

        public void Render(MotorPart selected)
        {
            foreach (var row in rows) row.Value.EnableInClassList("selected",row.Key == selected);
            foreach (var title in groupTitles)
            {
                bool open = selected && groupRows.TryGetValue(selected,out var row) && row == title.Key;
                title.Key.EnableInClassList("group-open",open);
                title.Key.text = title.Value + (open ? "   −" : "   +");
            }
            foreach (var entry in groupContents)
                entry.Value.style.display = selected && selected.Data.Group == entry.Key.Data.Group ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
