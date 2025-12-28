using System;
using System.Collections.Generic;
using System.Text;

namespace TuiskuAI.FixedEvolution.Parts
{
    public struct GenomeStructure
    {
        public enum GenePrecisionMode
        {
            Fast,
            Precise
        }

        public int[] LayerSizes;
        public float WeightRange;
        public float BiasRange;
        public GenePrecisionMode PrecisionMode;
    }
}
