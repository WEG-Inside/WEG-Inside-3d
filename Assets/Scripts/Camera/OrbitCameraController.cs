using UnityEngine;
using UnityEngine.InputSystem;

namespace WegInside
{
    [RequireComponent(typeof(Camera))]
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] MotorController motor;
        [SerializeField] ExperienceUIController ui;
        [SerializeField, Range(.08f,.6f)] float orbitSensitivity = .24f;
        [SerializeField, Range(.002f,.015f)] float panSensitivity = .006f;
        [SerializeField, Range(.05f,.45f)] float zoomSensitivity = .19f;
        [SerializeField, Range(.02f,.3f)] float smoothing = .11f;
        [SerializeField, Range(2,14)] float dragThreshold = 5f;
        [SerializeField] Vector2 homeAngles = new(36f,23f);
        [SerializeField, Range(0,30)] float explodedYawAdjustment = 22;
        [SerializeField] Vector2 zoomLimits = new(.40f,2.20f);

        Camera sceneCamera;
        MotorPart focus;
        float yaw, pitch, viewYaw, viewPitch;
        float zoom, drawDistance, distanceVelocity, yawVelocity, pitchVelocity;
        Vector3 pan, drawTarget, targetVelocity;
        Vector2 pressPosition;
        bool leftPressed, rightPressed, dragged, orbitGesture, initialized;

        public bool HasFocus => focus;
        public Camera SceneCamera => sceneCamera;

        void Awake()
        {
            sceneCamera = GetComponent<Camera>();
            RestoreView();
        }

        public void RestoreView()
        {
            yaw = homeAngles.x;
            pitch = homeAngles.y;
            FrameMotor();
        }

        public void FrameMotor()
        {
            focus = null;
            pan = Vector3.zero;
            zoom = 1;
        }

        public void Focus(MotorPart part)
        {
            focus = part;
            pan = Vector3.zero;
            zoom = 1;
        }

        void Update()
        {
            if (!motor) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) RestoreView();
                if (keyboard.escapeKey.wasPressedThisFrame) motor.Select(null);
            }
            var mouse = Mouse.current;
            if (mouse == null)
            {
                leftPressed = rightPressed = false;
                return;
            }
            Vector2 point = mouse.position.ReadValue();
            Vector2 delta = mouse.delta.ReadValue();
            bool blocked = ui && ui.BlocksPointer(point);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                leftPressed = !blocked;
                pressPosition = point;
                dragged = false;
                orbitGesture = leftPressed && !motor.Pick(sceneCamera.ScreenPointToRay(point));
            }
            if (leftPressed && mouse.leftButton.isPressed)
            {
                bool crossedThreshold = (point-pressPosition).sqrMagnitude > dragThreshold*dragThreshold;
                dragged |= crossedThreshold;
                if (dragged && orbitGesture && !blocked)
                {
                    yaw -= delta.x * orbitSensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity,2,78);
                }
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (leftPressed && !blocked && !dragged && (point-pressPosition).sqrMagnitude <= dragThreshold*dragThreshold)
                    motor.Select(motor.Pick(sceneCamera.ScreenPointToRay(point)));
                leftPressed = false;
            }
            if (mouse.rightButton.wasPressedThisFrame) rightPressed = !blocked;
            if (mouse.rightButton.wasReleasedThisFrame) rightPressed = false;
            if (rightPressed && !blocked)
            {
                float factor = Mathf.Max(1,drawDistance) * panSensitivity * 150f / Mathf.Max(250,Screen.height);
                pan = Vector3.ClampMagnitude(pan - factor*(sceneCamera.transform.right*delta.x + sceneCamera.transform.up*delta.y),8);
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (!blocked && !Mathf.Approximately(scroll,0))
            {
                float notches = Mathf.Clamp(Mathf.Abs(scroll) >= 10 ? scroll/120f : scroll,-3,3);
                zoom = Mathf.Clamp(zoom * Mathf.Exp(-notches*zoomSensitivity),
                    Mathf.Clamp(zoomLimits.x,.25f,1),Mathf.Clamp(zoomLimits.y,1,3));
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) leftPressed = rightPressed = false;
        }

        void LateUpdate()
        {
            if (motor) RenderView(false);
        }

        public void RenderView(bool snap)
        {
            if (!sceneCamera) sceneCamera = GetComponent<Camera>();
            if (!motor) return;
            if (focus && !focus.IsVisible) FrameMotor();
            Bounds bounds = focus ? focus.WorldBounds() : motor.VisibleBounds();
            Rect region = ui ? ui.ViewportRegion : Rect.MinMaxRect(.16f,.14f,.80f,.90f);
            float dt = Time.unscaledDeltaTime;
            float yawTarget = yaw - (focus ? 0 : explodedYawAdjustment*Mathf.SmoothStep(0,1,motor.State.Separation));
            snap |= !initialized;
            if (snap)
            {
                viewYaw = yawTarget;
                viewPitch = pitch;
                drawTarget = bounds.center + pan;
                targetVelocity = Vector3.zero;
                distanceVelocity = yawVelocity = pitchVelocity = 0;
            }
            else
            {
                viewYaw = Mathf.SmoothDampAngle(viewYaw,yawTarget,ref yawVelocity,smoothing,Mathf.Infinity,dt);
                viewPitch = Mathf.SmoothDamp(viewPitch,pitch,ref pitchVelocity,smoothing,Mathf.Infinity,dt);
                drawTarget = Vector3.SmoothDamp(drawTarget,bounds.center+pan,ref targetVelocity,smoothing,Mathf.Infinity,dt);
            }
            Quaternion angle = Quaternion.Euler(viewPitch,viewYaw,0);
            Vector3 right = angle * Vector3.right, up = angle * Vector3.up, forward = angle * Vector3.forward;
            float tanV = Mathf.Tan(sceneCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            float tanH = tanV * Mathf.Max(.2f,sceneCamera.aspect);
            float centerX = region.center.x*2-1, centerY = region.center.y*2-1;
            float safeWidth = Mathf.Max(.14f,region.width*.86f), safeHeight = Mathf.Max(.22f,region.height*.88f);
            float distance = FitDistance(bounds,right,up,forward,tanH,tanV,centerX,centerY,safeWidth,safeHeight);
            Vector3 extent = bounds.extents;
            float near = Mathf.Abs(forward.x)*extent.x + Mathf.Abs(forward.y)*extent.y + Mathf.Abs(forward.z)*extent.z + .30f;
            distance = Mathf.Clamp(distance*zoom,Mathf.Clamp(near,.85f,48f),48f);
            drawDistance = snap ? distance : Mathf.SmoothDamp(drawDistance,distance,ref distanceVelocity,smoothing,Mathf.Infinity,dt);
            initialized = true;
            Vector3 position = drawTarget - drawDistance*(forward + right*tanH*centerX + up*tanV*centerY);
            position.y = Mathf.Max(.12f,position.y);
            transform.SetPositionAndRotation(position,angle);
        }

        static float FitDistance(Bounds bounds, Vector3 right, Vector3 up, Vector3 forward,
            float tanH, float tanV, float centerX, float centerY, float width, float height)
        {
            float distance = 2f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = Vector3.Scale(bounds.extents,new Vector3((i&1)==0 ? -1 : 1,(i&2)==0 ? -1 : 1,(i&4)==0 ? -1 : 1));
                float z = Vector3.Dot(p,forward);
                float x = Vector3.Dot(p,right)/tanH - centerX*z;
                float y = Vector3.Dot(p,up)/tanV - centerY*z;
                distance = Mathf.Max(distance,Mathf.Max(Mathf.Abs(x)/width,Mathf.Abs(y)/height)-z);
            }
            return distance;
        }
    }
}
