using UnityEngine;

namespace WegInside
{
    public sealed class PartIsolationController : MonoBehaviour
    {
        internal void Apply(MotorPart part, MotorPart selected, MotorStateManager state, XRayController xray)
        {
            bool visible = state.Isolated ? part == selected : !state.XRay || !xray.Hides(part);
            part.SetVisible(visible);
        }
    }
}
