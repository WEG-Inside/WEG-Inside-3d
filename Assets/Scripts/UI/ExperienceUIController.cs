using UnityEngine;
using UnityEngine.UIElements;

namespace WegInside
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ExperienceUIController : MonoBehaviour
    {
        [SerializeField] MotorController motor;
        UIDocument document;
        VisualElement root, viewport, left, context, explodeTools, xrayTools, speedTools;
        Button collapse, explode, assemble, xray, run, restore;
        Slider separation, opacity, speed;
        Toggle hideExterior, exposeRotor;
        Label mode, status, progress, transparency, velocity;
        ComponentListController componentList;
        InformationPanelController information;
        MotorPart lastSelected;
        bool panelOpen = true, initialized, selectionRendered;

        public VisualElement Root => root;

        public Rect ViewportRegion
        {
            get
            {
                if (viewport == null || viewport.worldBound.width <= 1 || root.worldBound.height <= 1)
                    return Rect.MinMaxRect(.16f,.14f,.8f,.91f);
                Rect full = root.worldBound, area = viewport.worldBound;
                return Rect.MinMaxRect((area.xMin-full.xMin)/full.width,1-(area.yMax-full.yMin)/full.height,
                    (area.xMax-full.xMin)/full.width,1-(area.yMin-full.yMin)/full.height);
            }
        }

        public bool BlocksPointer(Vector2 screen)
        {
            if (root?.panel == null) return false;
            var panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel,new Vector2(screen.x,Screen.height-screen.y));
            return !viewport.worldBound.Contains(panelPosition);
        }

        void Start()
        {
            if (!motor || !motor.IsReady)
            {
                Debug.LogError("WEG Inside: conecte a interface ao controlador do motor.",this);
                enabled = false;
                return;
            }
            document = GetComponent<UIDocument>();
            root = document.rootVisualElement;
            viewport = root?.Q("viewport");
            if (viewport == null)
            {
                Debug.LogError("WEG Inside: o documento da interface não contém a área de visualização.",this);
                enabled = false;
                return;
            }
            left = root.Q("left-panel");
            context = root.Q("context");
            explodeTools = root.Q("explode-tools");
            xrayTools = root.Q("xray-tools");
            speedTools = root.Q("speed-tools");
            collapse = root.Q<Button>("collapse");
            explode = root.Q<Button>("explode");
            assemble = root.Q<Button>("assemble");
            xray = root.Q<Button>("xray");
            run = root.Q<Button>("run");
            restore = root.Q<Button>("restore");
            separation = root.Q<Slider>("separation");
            opacity = root.Q<Slider>("opacity");
            speed = root.Q<Slider>("speed");
            hideExterior = root.Q<Toggle>("hide-exterior");
            exposeRotor = root.Q<Toggle>("expose-rotor");
            mode = root.Q<Label>("mode");
            status = root.Q<Label>("status");
            progress = root.Q<Label>("progress");
            transparency = root.Q<Label>("transparency");
            velocity = root.Q<Label>("velocity");
            componentList = new ComponentListController(root.Q("component-list"),motor);
            information = new InformationPanelController(root,motor);
            collapse.clicked += TogglePanel;
            explode.clicked += () => motor.SetSeparation(1);
            assemble.clicked += () => motor.SetSeparation(0);
            xray.clicked += () => motor.SetXRay(!motor.State.XRay);
            run.clicked += motor.ToggleSimulation;
            restore.clicked += () => { SetPanel(true); motor.Restore(); };
            root.Q<Button>("reset-simulation").clicked += motor.ResetSimulation;
            separation.RegisterValueChangedCallback(change => motor.SetSeparation(change.newValue,true));
            opacity.RegisterValueChangedCallback(change => motor.SetOpacity(1-change.newValue));
            speed.RegisterValueChangedCallback(change => motor.SetSpeed(change.newValue));
            hideExterior.RegisterValueChangedCallback(change => motor.SetHideExterior(change.newValue));
            exposeRotor.RegisterValueChangedCallback(change => motor.SetExposeRotor(change.newValue));
            motor.Changed += Render;
            initialized = true;
            Render();
        }

        void OnDestroy()
        {
            if (motor) motor.Changed -= Render;
        }

        void TogglePanel() => SetPanel(!panelOpen);

        void SetPanel(bool expanded)
        {
            panelOpen = expanded;
            left.EnableInClassList("collapsed",!expanded);
            collapse.text = expanded ? "‹" : "›";
            collapse.tooltip = expanded ? "Recolher componentes" : "Mostrar componentes";
        }

        void Render()
        {
            if (!initialized || motor.Rotation == null || motor.XRay == null) return;
            var state = motor.State;
            bool detached = !state.CanRun;
            bool running = state.Operation == OperationState.Running;
            mode.text = state.Assembly switch
            {
                AssemblyState.Exploding => "Separando peças",
                AssemblyState.Assembling => "Remontando",
                AssemblyState.Exploded => "Vista explodida",
                _ => "Motor montado"
            };
            if (state.XRay) mode.text += "  /  Raio-X";
            if (state.Isolated) mode.text += "  /  Isolado";
            status.text = detached ? "Remonte o motor para iniciar a simulação." :
                running ? "Motor em funcionamento" : motor.Notice ?? "Motor parado";
            status.EnableInClassList("running-status",running);
            status.EnableInClassList("blocked-status",detached);
            explode.SetEnabled(state.Target < 1);
            assemble.SetEnabled(state.Target > 0 || state.Separation > 0);
            xray.EnableInClassList("active-control",state.XRay);
            run.SetEnabled(!detached);
            run.text = running ? "Pausar motor" : "Iniciar motor";
            run.EnableInClassList("active-control",running);

            bool showSpeed = motor.HasOperated && !detached;
            context.style.display = detached || state.XRay || showSpeed ? DisplayStyle.Flex : DisplayStyle.None;
            explodeTools.style.display = detached ? DisplayStyle.Flex : DisplayStyle.None;
            xrayTools.style.display = state.XRay ? DisplayStyle.Flex : DisplayStyle.None;
            speedTools.style.display = showSpeed ? DisplayStyle.Flex : DisplayStyle.None;
            separation.SetValueWithoutNotify(state.Separation);
            opacity.SetValueWithoutNotify(1-motor.XRay.Opacity);
            opacity.SetEnabled(!motor.XRay.HideExterior && motor.XRay.SupportsTransparency);
            speed.SetValueWithoutNotify(motor.Rotation.Speed);
            hideExterior.SetValueWithoutNotify(motor.XRay.HideExterior || !motor.XRay.SupportsTransparency);
            exposeRotor.SetValueWithoutNotify(motor.XRay.ExposeRotor);
            hideExterior.SetEnabled(motor.XRay.SupportsTransparency);
            progress.text = $"{Mathf.RoundToInt(state.Separation*100)}%";
            transparency.text = $"{Mathf.RoundToInt((1-motor.XRay.Opacity)*100)}%";
            velocity.text = $"{Mathf.RoundToInt(motor.Rotation.Speed*100)}%";
            if (!selectionRendered || lastSelected != motor.Selected)
            {
                selectionRendered = true;
                lastSelected = motor.Selected;
                componentList.Render(lastSelected);
            }
            information.Render(motor.Selected,state.Isolated);
        }
    }
}
