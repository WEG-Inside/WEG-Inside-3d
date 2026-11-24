using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace WegInside.Tests
{
    public sealed class MotorExperienceTests
    {
        const string Scene = "Assets/Scenes/MotorExperience.unity";
        MotorController motor;
        ExperienceUIController ui;

        [UnitySetUp]
        public IEnumerator OpenScene()
        {
            yield return SceneManager.LoadSceneAsync(Scene,LoadSceneMode.Single);
            yield return null;
            motor = Object.FindAnyObjectByType<MotorController>();
            ui = Object.FindAnyObjectByType<ExperienceUIController>();
            Assert.That(motor,Is.Not.Null);
            Assert.That(ui.Root,Is.Not.Null);
        }

        MotorPart Part(string id) => motor.Parts.Single(part => part.Data.Id == id);

        [UnityTest]
        public IEnumerator ShowingSimulationControlsKeepsTheCameraStill()
        {
            var orbit = Object.FindAnyObjectByType<OrbitCameraController>();
            orbit.RenderView(true);
            Vector3 position = orbit.transform.position;

            Submit(ui.Root.Q<Button>("run"));
            yield return null;
            yield return null;
            orbit.RenderView(true);

            Assert.That(motor.State.Operation,Is.EqualTo(OperationState.Running));
            Assert.That(ui.Root.Q("speed-tools").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Vector3.Distance(position,orbit.transform.position),Is.LessThan(.001f));
        }

        [UnityTest]
        public IEnumerator RotatingAnIsolatedComponentKeepsItsFramingStable()
        {
            var orbit = Object.FindAnyObjectByType<OrbitCameraController>();
            motor.ToggleSimulation();
            motor.ResetSimulation();
            yield return null;
            yield return null;

            foreach (var part in motor.Parts.Where(part => part.Rotates))
            {
                motor.Select(part);
                if (!motor.State.Isolated) motor.ToggleIsolation();
                else motor.FocusSelection();
                orbit.RenderView(true);
                Vector3 position = orbit.transform.position;

                float maxDrift = 0f;
                motor.SetSpeed(1);
                motor.ToggleSimulation();
                float deadline = Time.realtimeSinceStartup + 2;
                do
                {
                    yield return null;
                    orbit.RenderView(true);
                    maxDrift = Mathf.Max(maxDrift,Vector3.Distance(position,orbit.transform.position));
                }
                while (motor.Rotation.Angle < 95 && Time.realtimeSinceStartup < deadline);

                Assert.That(motor.Rotation.Angle,Is.GreaterThanOrEqualTo(95f),part.name);
                Assert.That(maxDrift,Is.LessThan(.001f),part.name);
                motor.ResetSimulation();
            }
        }

        [UnityTest]
        public IEnumerator ThreePartsRotateInSyncAndExplosionRestoresTheirHomeTransforms()
        {
            var rotor = Part("rotor");
            var stator = Part("stator");
            motor.ToggleSimulation();
            yield return null;
            yield return null;
            Assert.That(Quaternion.Angle(rotor.transform.localRotation,rotor.HomeRotation),Is.GreaterThan(.01f));
            Assert.That(Quaternion.Angle(rotor.transform.localRotation,Part("shaft").transform.localRotation),Is.LessThan(.01f));
            Assert.That(Quaternion.Angle(rotor.transform.localRotation,Part("fan").transform.localRotation),Is.LessThan(.01f));
            Assert.That(stator.transform.localRotation,Is.EqualTo(stator.HomeRotation));
            motor.SetSeparation(.60f,true);
            Assert.That(stator.transform.localPosition.x,Is.LessThan(-1));
            Assert.That(motor.State.Operation,Is.EqualTo(OperationState.Paused));
            Assert.That(ui.Root.Q<Button>("run").enabledSelf,Is.False);
            motor.ToggleSimulation();
            Assert.That(motor.State.Operation,Is.EqualTo(OperationState.Paused));
            motor.SetSeparation(0);
            float end = Time.realtimeSinceStartup + 4;
            while (!motor.State.CanRun && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(motor.State.CanRun,Is.True);
            foreach (var part in motor.Parts)
            {
                Assert.That(part.transform.localPosition,Is.EqualTo(part.HomePosition));
                Assert.That(part.transform.localRotation,Is.EqualTo(part.HomeRotation));
            }
        }

        [UnityTest]
        public IEnumerator XRaySupportsPickingInteriorAndRestoresOriginalMaterial()
        {
            var housing = Part("housing");
            var renderer = housing.GetComponentInChildren<MeshRenderer>();
            var original = renderer.sharedMaterial;
            var ray = new Ray(new Vector3(0,1.36f,-5),Vector3.forward);
            Assert.That(motor.Pick(ray),Is.EqualTo(housing));
            motor.SetXRay(true);
            motor.SetOpacity(.27f);
            Assert.That(renderer.sharedMaterial.name,Is.EqualTo("Transparent_Housing"));
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_BaseColor").a,Is.EqualTo(.27f).Within(.001f));
            Assert.That(motor.Pick(ray),Is.EqualTo(Part("stator")));
            motor.SetExposeRotor(true);
            Assert.That(motor.Pick(ray),Is.EqualTo(Part("rotor")));
            motor.SetHideExterior(true);
            Assert.That(housing.IsVisible,Is.False);
            motor.Restore();
            yield return null;
            Assert.That(renderer.sharedMaterial,Is.EqualTo(original));
            Assert.That(housing.IsVisible,Is.True);
        }

        [UnityTest]
        public IEnumerator ComponentButtonUpdatesInformationAndIsolationAndRestore()
        {
            Button rotor = ui.Root.Q<Button>("part-rotor");
            Submit(rotor);
            Assert.That(motor.Selected,Is.EqualTo(Part("rotor")));
            Assert.That(ui.Root.Q<Label>("part-name").text,Is.EqualTo("Rotor"));
            Submit(ui.Root.Q<Button>("focus"));
            var camera = Object.FindAnyObjectByType<OrbitCameraController>();
            Assert.That(camera.HasFocus,Is.True);
            Submit(ui.Root.Q<Button>("isolate"));
            Assert.That(motor.Parts.Count(part => part.IsVisible),Is.EqualTo(1));
            ui.Root.Q<Slider>("separation").value = .73f;
            Assert.That(motor.State.Separation,Is.EqualTo(.73f).Within(.001f));
            Submit(ui.Root.Q<Button>("xray"));
            Submit(ui.Root.Q<Button>("restore"));
            yield return null;
            Assert.That(motor.Selected,Is.Null);
            Assert.That(motor.State.CanRun,Is.True);
            Assert.That(motor.State.Isolated || motor.State.XRay,Is.False);
            Assert.That(motor.Parts.All(part => part.IsVisible),Is.True);
            Assert.That(camera.HasFocus,Is.False);
            Assert.That(ui.ViewportRegion.width,Is.GreaterThan(.45f));
        }

        [UnityTest]
        public IEnumerator AnimatedSeparationAndSliderCanReverseDuringTransition()
        {
            Submit(ui.Root.Q<Button>("explode"));
            yield return null;
            yield return null;
            Assert.That(motor.State.Separation,Is.GreaterThan(0));
            ui.Root.Q<Slider>("separation").value = .41f;
            Assert.That(motor.State.Separation,Is.EqualTo(.41f).Within(.001f));
            Submit(ui.Root.Q<Button>("assemble"));
            Assert.That(motor.State.Assembly,Is.EqualTo(AssemblyState.Assembling));
            Submit(ui.Root.Q<Button>("explode"));
            Assert.That(motor.State.Assembly,Is.EqualTo(AssemblyState.Exploding));
            float end = Time.realtimeSinceStartup + 4;
            while (motor.State.Separation < 1 && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(motor.State.Separation,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MouseAndKeyboardDriveTheSceneAndUi()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editorBehavior = settings.editorInputBehaviorInPlayMode;
            bool runInBackground = Application.runInBackground;
            var existingMice = InputSystem.devices.OfType<Mouse>().Where(device => device.enabled).ToArray();
            Mouse mouse = null;
            Keyboard keyboard = null;
            try
            {
                Application.runInBackground = true;
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                foreach (var device in existingMice) InputSystem.DisableDevice(device);
                mouse = InputSystem.AddDevice<Mouse>();
                keyboard = InputSystem.AddDevice<Keyboard>();
                var orbit = Object.FindAnyObjectByType<OrbitCameraController>();
                var area = ui.ViewportRegion;
                var empty = new Vector2(Screen.width*(area.xMin+.075f),Screen.height*(area.yMax-.075f));
                var drag = new Vector2(90,34);
                var initialRotation = orbit.transform.rotation;
                yield return MouseInput(mouse,empty,1);
                yield return MouseInput(mouse,empty+drag,1,drag);
                yield return MouseInput(mouse,empty+drag);
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(Quaternion.Angle(initialRotation,orbit.transform.rotation),Is.GreaterThan(6));
                Assert.That(motor.Selected,Is.Null);

                var previousPosition = orbit.transform.position;
                yield return MouseInput(mouse,empty,2);
                yield return MouseInput(mouse,empty+drag,2,drag);
                yield return MouseInput(mouse,empty+drag);
                Assert.That(Vector3.Distance(previousPosition,orbit.transform.position),Is.GreaterThan(.10f));
                yield return new WaitForSecondsRealtime(.25f);
                previousPosition = orbit.transform.position;
                yield return MouseInput(mouse,empty,scroll:120);
                yield return MouseInput(mouse,empty);
                Assert.That(Vector3.Distance(previousPosition,orbit.transform.position),Is.GreaterThan(.20f));

                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));
                yield return new WaitForSecondsRealtime(.55f);
                Assert.That(Quaternion.Angle(initialRotation,orbit.transform.rotation),Is.LessThan(.15f));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());

                var shaftPosition = Part("shaft").transform.TransformPoint(new Vector3(-2.29f,0,0));
                Vector2 shaftPoint = orbit.SceneCamera.WorldToScreenPoint(shaftPosition);
                yield return MouseInput(mouse,shaftPoint,1);
                yield return MouseInput(mouse,shaftPoint);
                Assert.That(motor.Selected,Is.EqualTo(Part("shaft")));

                var row = ui.Root.Q<Button>("part-rotor");
                Rect full = ui.Root.worldBound;
                Vector2 center = row.worldBound.center;
                var panelPoint = new Vector2((center.x-full.xMin)/full.width*Screen.width,
                    (1-(center.y-full.yMin)/full.height)*Screen.height);
                Assert.That(ui.BlocksPointer(panelPoint),Is.True);
                yield return MouseInput(mouse,panelPoint);
                yield return MouseInput(mouse,panelPoint,1);
                yield return MouseInput(mouse,panelPoint);
                Assert.That(motor.Selected,Is.EqualTo(Part("rotor")));

                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(motor.Selected,Is.Null);
            }
            finally
            {
                if (mouse != null) InputSystem.RemoveDevice(mouse);
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                foreach (var device in existingMice) InputSystem.EnableDevice(device);
                settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = editorBehavior;
                Application.runInBackground = runInBackground;
            }
        }

        static IEnumerator MouseInput(Mouse mouse, Vector2 position, ushort buttons = 0, Vector2 delta = default, float scroll = 0)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState { position = position, buttons = buttons,
                delta = delta, scroll = Vector2.up*scroll });
            yield return new WaitForSecondsRealtime(.095f);
        }

        static void Submit(Button button)
        {
            Assert.That(button,Is.Not.Null);
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }
    }
}
