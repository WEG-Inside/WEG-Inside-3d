using UnityEngine;

namespace WegInside
{
    public sealed class PartHighlightController : MonoBehaviour
    {
        [SerializeField] Material highlightMaterial;
        MaterialPropertyBlock properties;

        internal void Apply(MotorPart part, MotorPart selected, MotorStateManager state, XRayController xray)
        {
            properties ??= new MaterialPropertyBlock();
            bool transparent = state.XRay && !state.Isolated && part.IsExterior && xray.SupportsTransparency;
            part.SetAppearance(xray.TransparentMaterial,transparent,xray.Opacity,highlightMaterial,part == selected,properties);
        }
    }
}
