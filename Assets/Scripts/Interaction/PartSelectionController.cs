using UnityEngine;

namespace WegInside
{
    public sealed class PartSelectionController : MonoBehaviour
    {
        [SerializeField] LayerMask selectableLayers = ~0;
        readonly RaycastHit[] hits = new RaycastHit[96];

        public MotorPart Pick(Ray ray, bool preferInterior)
        {
            int count = Physics.RaycastNonAlloc(ray,hits,100f,selectableLayers,QueryTriggerInteraction.Ignore);
            MotorPart nearest = null, interior = null;
            float nearestDistance = float.PositiveInfinity, interiorDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var part = hit.collider.GetComponentInParent<MotorPart>();
                if (!part || !part.Data || !part.IsVisible) continue;
                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearest = part;
                }
                if (!part.IsExterior && hit.distance < interiorDistance)
                {
                    interiorDistance = hit.distance;
                    interior = part;
                }
            }
            return preferInterior && interior ? interior : nearest;
        }
    }
}
