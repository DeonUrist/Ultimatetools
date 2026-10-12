using System.Collections.Generic;

namespace PartAdjustment
{
    internal static class ToolSlotPolicy
    {
        internal static string AttachState(IEnumerable<string> states)
        {
            bool dispatch = false, typedRecipe = false;
            foreach (var state in states)
            {
                if (state == "Attach") return "Attach";
                if (state == "check type") dispatch = true;
                if (state == "Attach turbo" || state == "Attach supercharger") typedRecipe = true;
            }
            if (dispatch && typedRecipe) return "check type";
            return null;
        }
    }
}
