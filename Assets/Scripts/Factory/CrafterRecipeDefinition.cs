using System;
using System.Collections.Generic;

namespace com.Plapamaru.TownCrafter.Factory
{
    [Serializable]
    public class CrafterRecipeDefinition
    {
        public List<CrafterRecipeInput> inputs;
        public ResourceItemData output;
    }

    [Serializable]
    public class CrafterRecipeInput
    {
        public ResourceItemData resourceItem;
        public int amount = 1;
    }
}