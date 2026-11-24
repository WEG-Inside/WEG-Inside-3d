using UnityEngine;

namespace WegInside
{
    public enum AssemblyState { Assembled, Exploding, Exploded, Assembling }
    public enum OperationState { Paused, Running }

    public sealed class MotorStateManager
    {
        public float Separation { get; private set; }
        public float Target { get; private set; }
        public bool XRay { get; private set; }
        public bool Isolated { get; private set; }
        public OperationState Operation { get; private set; }
        public bool CanRun => Assembly == AssemblyState.Assembled;

        public AssemblyState Assembly => Target > Separation ? AssemblyState.Exploding
            : Target < Separation ? AssemblyState.Assembling
            : Separation <= 0 ? AssemblyState.Assembled : AssemblyState.Exploded;

        public bool RequestSeparation(float target, bool immediate = false)
        {
            target = Mathf.Clamp01(target);
            if (Target == target && (!immediate || Separation == target)) return false;
            Target = target;
            if (immediate) Separation = target;
            if (!CanRun) Stop();
            return true;
        }

        public bool Advance(float delta)
        {
            float next = Mathf.MoveTowards(Separation,Target,Mathf.Max(0,delta));
            if (next == Separation) return false;
            Separation = next;
            return true;
        }

        public bool TryRun()
        {
            if (!CanRun) return false;
            Operation = OperationState.Running;
            return true;
        }

        public void Stop() => Operation = OperationState.Paused;
        public void SetXRay(bool active) => XRay = active;
        public void SetIsolation(bool active) => Isolated = active;

        public void Reset()
        {
            Separation = Target = 0;
            XRay = Isolated = false;
            Stop();
        }
    }
}
