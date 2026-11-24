using UnityEngine;
using UnityEngine.Rendering;

namespace WegInside
{
    [DisallowMultipleComponent]
    public sealed class MotorPart : MonoBehaviour
    {
        [SerializeField] MotorPartData data;
        [SerializeField] Vector3 explosionOffset;
        [SerializeField] bool exterior, rotates;
        [SerializeField] Transform serviceCover;
        [SerializeField] Vector3 coverTravel = new(.04f,.34f,0);
        Renderer[] renderers;
        Collider[] colliders;
        Material[] originalMaterials;
        Vector3 homePosition, coverHome;
        Quaternion homeRotation;
        float coverAmount;

        public MotorPartData Data => data;
        public bool IsExterior => exterior;
        public bool Rotates => rotates;
        public Vector3 ExplosionOffset => Vector3.ClampMagnitude(explosionOffset,8f);
        public Vector3 HomePosition => homePosition;
        public Quaternion HomeRotation => homeRotation;
        public bool IsVisible { get; private set; } = true;

        public void Configure(MotorPartData information, Vector3 offset, bool isExterior, bool rotatesInOperation)
        {
            data = information;
            explosionOffset = offset;
            exterior = isExterior;
            rotates = rotatesInOperation;
        }

        public void SetServiceCover(Transform cover) => serviceCover = cover;

        public bool Initialize()
        {
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            if (serviceCover) coverHome = serviceCover.localPosition;
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalMaterials[i] = renderers[i].sharedMaterial;
            return data && renderers.Length > 0 && colliders.Length > 0;
        }

        public Bounds WorldBounds()
        {
            if (renderers == null || renderers.Length == 0) return new Bounds(transform.position,Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        internal void Move(float progress) => transform.localPosition = homePosition + ExplosionOffset * progress;

        internal void SetCoverOpening(float target, float delta)
        {
            if (!serviceCover || coverAmount == target) return;
            coverAmount = Mathf.MoveTowards(coverAmount,target,delta);
            serviceCover.localPosition = coverHome + coverTravel * Mathf.SmoothStep(0,1,coverAmount);
        }

        internal void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            IsVisible = visible;
            foreach (var renderer in renderers) renderer.enabled = visible;
            foreach (var collider in colliders) collider.enabled = visible;
        }

        internal void SetAppearance(Material transparent, bool applyXRay, float opacity, Material highlight, bool selected,
            MaterialPropertyBlock properties)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material material = applyXRay ? transparent : originalMaterials[i];
                if (!material) continue;
                if (renderer.sharedMaterial != material) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = applyXRay ? ShadowCastingMode.Off : ShadowCastingMode.On;
                properties.Clear();
                if (applyXRay || selected)
                {
                    Color color = material.GetColor("_BaseColor");
                    if (selected && highlight)
                        color = Color.Lerp(color,highlight.GetColor("_BaseColor"),applyXRay ? .50f : .28f);
                    if (applyXRay) color.a = opacity;
                    properties.SetColor("_BaseColor",color);
                    if (selected && !applyXRay) properties.SetColor("_EmissionColor",color * .14f);
                }
                renderer.SetPropertyBlock(applyXRay || selected ? properties : null);
            }
        }
    }
}
