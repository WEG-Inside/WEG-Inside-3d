using System.Collections.Generic;
using UnityEngine;

namespace WegInside
{
    public sealed class ExplodedViewController : MonoBehaviour
    {
        [SerializeField, Range(.2f,4f)] float travelTime = 1.35f;
        public bool Advance(MotorStateManager state, float delta) => state.Advance(delta / Mathf.Max(.2f,travelTime));

        public void Apply(IReadOnlyList<MotorPart> parts, float separation)
        {
            float eased = Mathf.SmoothStep(0,1,separation);
            foreach (var part in parts) part.Move(eased);
        }
    }
}
