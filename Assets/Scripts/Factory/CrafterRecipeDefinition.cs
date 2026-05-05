using System;
using System.Collections.Generic;

namespace com.Plapamaru.TownCrafter.Factory
{
    [Serializable]
    public class CrafterRecipeDefinition
    {
        public List<CrafterRecipeInput> inputs;
        public ResourceItemType output;
    }

    [Serializable]
    public class CrafterRecipeInput
    {
        public ResourceItemType resourceType;
        public int amount = 1;
    }
}