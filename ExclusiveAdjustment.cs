using System;

namespace PartAdjustment
{
    // The active target is retained independently of the current hover target.
    internal sealed class ExclusiveAdjustment<T> where T : class
    {
        private readonly Action<T, bool> transition;
        internal T Active { get; private set; }

        internal ExclusiveAdjustment(Action<T, bool> transition) { this.transition = transition; }

        internal void Begin(T target)
        {
            Stop();
            if (target == null) return;
            Active = target;
            transition(target, true);
        }

        internal void Stop()
        {
            var previous = Active;
            Active = null;
            if (previous != null) transition(previous, false);
        }
    }
}
