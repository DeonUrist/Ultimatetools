namespace PartAdjustment
{
    internal enum SuspensionUpdate { None, AdjustKit, RestoreStock }

    // A stock car stays under native control. Only undo transforms that this mod owned.
    internal sealed class SuspensionOwnership
    {
        private bool modified;

        internal SuspensionUpdate BeginApply(bool kitFitted, bool savedAdjustment)
        {
            if (kitFitted)
            {
                modified = true;
                return SuspensionUpdate.AdjustKit;
            }
            bool restore = modified || savedAdjustment;
            modified = false;
            return restore ? SuspensionUpdate.RestoreStock : SuspensionUpdate.None;
        }
    }
}
