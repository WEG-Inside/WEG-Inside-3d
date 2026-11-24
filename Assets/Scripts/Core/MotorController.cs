using System;
using System.Collections.Generic;
using UnityEngine;

namespace WegInside
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ExplodedViewController),typeof(MotorRotationController),typeof(XRayController))]
    [RequireComponent(typeof(PartSelectionController),typeof(PartIsolationController),typeof(PartHighlightController))]
    public sealed class MotorController : MonoBehaviour
    {
        [SerializeField] Transform motorRoot;
        [SerializeField] OrbitCameraController orbit;
        ExplodedViewController exploded;
        MotorRotationController rotation;
        XRayController xray;
        PartSelectionController selection;
        PartIsolationController isolation;
        PartHighlightController highlight;
        MotorPart[] parts = Array.Empty<MotorPart>();
        bool ready;
        MotorPart terminalBox;

        public MotorStateManager State { get; } = new();
        public IReadOnlyList<MotorPart> Parts => parts;
        public MotorPart Selected { get; private set; }
        public XRayController XRay => xray;
        public MotorRotationController Rotation => rotation;
        public string Notice { get; private set; }
        public bool IsReady => ready;
        public bool HasOperated { get; private set; }
        public event Action Changed;

        void Awake()
        {
            exploded = GetComponent<ExplodedViewController>();
            rotation = GetComponent<MotorRotationController>();
            xray = GetComponent<XRayController>();
            selection = GetComponent<PartSelectionController>();
            isolation = GetComponent<PartIsolationController>();
            highlight = GetComponent<PartHighlightController>();
            if (!motorRoot || !orbit)
            {
                Debug.LogError("WEG Inside: configure a referência ao motor e à câmera orbital.",this);
                enabled = false;
                return;
            }
            parts = motorRoot.GetComponentsInChildren<MotorPart>(true);
            ready = parts.Length > 0;
            foreach (var part in parts)
            {
                if (!part.Initialize()) ready = false;
                if (part.Data && part.Data.Id == "terminal-box") terminalBox = part;
            }
            if (!ready)
            {
                Debug.LogError("WEG Inside: o motor precisa de peças com dados, malhas e colisores.",this);
                enabled = false;
            }
        }

        void Update()
        {
            if (!ready) return;
            float delta = Time.unscaledDeltaTime;
            if (exploded.Advance(State,delta))
            {
                exploded.Apply(parts,State.Separation);
                Changed?.Invoke();
            }
            if (State.Operation == OperationState.Running) rotation.Advance(parts,delta);
            if (terminalBox)
            {
                float opening = State.Isolated && terminalBox == Selected ? 1f : Mathf.SmoothStep(0,1,State.Separation);
                terminalBox.SetCoverOpening(opening,delta * 3);
            }
        }

        public Bounds VisibleBounds()
        {
            var bounds = new Bounds(motorRoot ? motorRoot.position : Vector3.zero,Vector3.one);
            bool initialized = false;
            foreach (var part in parts)
            {
                if (!part.IsVisible) continue;
                if (initialized) bounds.Encapsulate(part.WorldBounds());
                else bounds = part.WorldBounds();
                initialized = true;
            }
            return bounds;
        }

        public MotorPart Pick(Ray ray)
        {
            if (!ready) return null;
            Physics.SyncTransforms();
            return selection.Pick(ray,State.XRay && !State.Isolated);
        }

        public void Select(MotorPart part)
        {
            if (!ready || (part && Array.IndexOf(parts,part) < 0)) return;
            Selected = part;
            if (!Selected && State.Isolated)
            {
                State.SetIsolation(false);
                orbit.FrameMotor();
            }
            if (Selected && State.XRay && xray.Hides(Selected))
            {
                if (Selected.IsExterior) xray.SetHideExterior(false);
                else xray.SetExposeRotor(false);
            }
            RefreshAppearance();
        }

        public void ToggleIsolation()
        {
            if (!ready || !Selected) return;
            State.SetIsolation(!State.Isolated);
            if (State.Isolated) orbit.Focus(Selected);
            else orbit.FrameMotor();
            RefreshAppearance();
        }

        public void FocusSelection()
        {
            if (Selected && Selected.IsVisible) orbit.Focus(Selected);
        }

        public void SetSeparation(float value, bool immediate = false)
        {
            if (!ready || !State.RequestSeparation(value,immediate)) return;
            if (State.Target > 0)
            {
                rotation.ResetRotation(parts);
                Notice = "Remonte o motor para iniciar a simulação.";
            }
            else Notice = null;
            if (!State.Isolated && orbit.HasFocus) orbit.FrameMotor();
            if (immediate) exploded.Apply(parts,State.Separation);
            Changed?.Invoke();
        }

        public void SetXRay(bool active)
        {
            if (!ready) return;
            State.SetXRay(active);
            RefreshAppearance();
        }

        public void SetOpacity(float opacity)
        {
            if (!ready) return;
            xray.SetOpacity(opacity);
            RefreshAppearance();
        }

        public void SetHideExterior(bool hidden)
        {
            if (!ready) return;
            xray.SetHideExterior(hidden);
            RefreshAppearance();
        }

        public void SetExposeRotor(bool exposed)
        {
            if (!ready) return;
            xray.SetExposeRotor(exposed);
            RefreshAppearance();
        }

        public void ToggleSimulation()
        {
            if (!ready) return;
            if (State.Operation == OperationState.Running) State.Stop();
            else if (!State.TryRun())
            {
                Notice = "Remonte o motor para iniciar a simulação.";
                Changed?.Invoke();
                return;
            }
            HasOperated = true;
            Notice = null;
            Changed?.Invoke();
        }

        public void SetSpeed(float speed)
        {
            if (!ready) return;
            rotation.SetSpeed(speed);
            Changed?.Invoke();
        }

        public void ResetSimulation()
        {
            if (!ready) return;
            State.Stop();
            rotation.ResetRotation(parts);
            Notice = null;
            Changed?.Invoke();
        }

        public void Restore()
        {
            if (!ready) return;
            State.Reset();
            HasOperated = false;
            Selected = null;
            Notice = null;
            xray.ResetOptions();
            rotation.SetSpeed(.48f);
            rotation.ResetRotation(parts);
            exploded.Apply(parts,0);
            if (terminalBox) terminalBox.SetCoverOpening(0,1f);
            orbit.RestoreView();
            RefreshAppearance();
        }

        void RefreshAppearance()
        {
            foreach (var part in parts)
            {
                isolation.Apply(part,Selected,State,xray);
                highlight.Apply(part,Selected,State,xray);
            }
            Changed?.Invoke();
        }
    }
}
