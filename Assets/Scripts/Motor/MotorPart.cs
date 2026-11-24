using System.Collections.Generic;
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
        Bounds spinBounds;
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
            if (rotates && renderers.Length > 0) spinBounds = CalculateSpinBounds();
            return data && renderers.Length > 0 && colliders.Length > 0;
        }

        public Bounds WorldBounds()
        {
            if (renderers == null || renderers.Length == 0) return new Bounds(transform.position,Vector3.one);
            if (rotates) return SpinBoundsInWorld();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        Bounds CalculateSpinBounds()
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity, radiusSquared = 0f;
            var vertices = new List<Vector3>();
            Matrix4x4 toSpinSpace = Matrix4x4.Scale(transform.localScale) * transform.worldToLocalMatrix;

            // Sweep the geometry around the local X axis to keep the framing independent of the spin angle.
            void Include(Vector3 point)
            {
                minX = Mathf.Min(minX,point.x);
                maxX = Mathf.Max(maxX,point.x);
                radiusSquared = Mathf.Max(radiusSquared,point.y*point.y + point.z*point.z);
            }

            foreach (var renderer in renderers)
            {
                Matrix4x4 toPart = toSpinSpace * renderer.localToWorldMatrix;
                if (renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh &&
                    filter.sharedMesh.isReadable && filter.sharedMesh.vertexCount > 0)
                {
                    filter.sharedMesh.GetVertices(vertices);
                    foreach (var vertex in vertices) Include(toPart.MultiplyPoint3x4(vertex));
                    continue;
                }

                Bounds bounds = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new((i&1)==0 ? bounds.min.x : bounds.max.x,
                        (i&2)==0 ? bounds.min.y : bounds.max.y,(i&4)==0 ? bounds.min.z : bounds.max.z);
                    Include(toPart.MultiplyPoint3x4(corner));
                }
            }

            float diameter = 2f*Mathf.Sqrt(radiusSquared);
            return new Bounds(Vector3.right*((minX+maxX)*.5f),new Vector3(maxX-minX,diameter,diameter));
        }

        Bounds SpinBoundsInWorld()
        {
            var toWorld = Matrix4x4.TRS(transform.localPosition,homeRotation,Vector3.one);
            var parent = transform.parent;
            if (parent) toWorld = parent.localToWorldMatrix * toWorld;

            var x = toWorld.MultiplyVector(Vector3.right);
            var y = toWorld.MultiplyVector(Vector3.up);
            var z = toWorld.MultiplyVector(Vector3.forward);
            Vector3 localExtents = spinBounds.extents;
            Vector3 extents = Vector3.zero;
            for (int i = 0; i < 3; i++)
                extents[i] = Mathf.Abs(x[i])*localExtents.x + Mathf.Sqrt(y[i]*y[i]+z[i]*z[i])*localExtents.y;
            return new Bounds(toWorld.MultiplyPoint3x4(spinBounds.center),2f*extents);
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
