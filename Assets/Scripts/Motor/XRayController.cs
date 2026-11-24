using UnityEngine;

namespace WegInside
{
    public sealed class XRayController : MonoBehaviour
    {
        [SerializeField] Material transparentHousing;
        [SerializeField, Range(.05f,.55f)] float opacity = .18f;
        public float Opacity => opacity;
        public bool HideExterior { get; private set; }
        public bool ExposeRotor { get; private set; }
        internal Material TransparentMaterial => transparentHousing;
        public bool SupportsTransparency => transparentHousing && transparentHousing.shader.isSupported;

        public void SetOpacity(float value) => opacity = Mathf.Clamp(value,.05f,.55f);
        public void SetHideExterior(bool hidden) => HideExterior = hidden;
        public void SetExposeRotor(bool exposed) => ExposeRotor = exposed;

        public void ResetOptions()
        {
            opacity = .18f;
            HideExterior = ExposeRotor = false;
        }

        public bool Hides(MotorPart part) => part.IsExterior ? HideExterior || !SupportsTransparency
            : ExposeRotor && part.Data.Id == "stator";
    }
}
