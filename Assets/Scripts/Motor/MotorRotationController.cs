using System.Collections.Generic;
using UnityEngine;

namespace WegInside
{
    public sealed class MotorRotationController : MonoBehaviour
    {
        [SerializeField, Range(30,540)] float degreesPerSecond = 220;
        public float Speed { get; private set; } = .48f;
        public float Angle { get; private set; }

        public void SetSpeed(float amount) => Speed = Mathf.Clamp01(amount);

        public void Advance(IReadOnlyList<MotorPart> parts, float delta)
        {
            Angle = Mathf.Repeat(Angle + (Mathf.Lerp(16,Mathf.Clamp(degreesPerSecond,30,540),Speed)*delta),360);
            Apply(parts);
        }

        public void ResetRotation(IReadOnlyList<MotorPart> parts)
        {
            Angle = 0;
            Apply(parts);
        }

        void Apply(IReadOnlyList<MotorPart> parts)
        {
            var spin = Quaternion.AngleAxis(Angle,Vector3.right);
            foreach (var part in parts)
                if (part.Rotates) part.transform.localRotation = part.HomeRotation * spin;
        }
    }
}
