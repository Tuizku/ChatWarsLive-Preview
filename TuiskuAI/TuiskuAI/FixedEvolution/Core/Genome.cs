using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;

using TuiskuAI.FixedEvolution.Parts;

namespace TuiskuAI.FixedEvolution.Core
{
    public class Genome
    {
        // FROM STRUCTURE
        public int[] LayerSizes;
        public float WeightRange;
        public float BiasRange;
        public GenomeStructure.GenePrecisionMode PrecisionMode;

        // GENES
        public byte[] GeneData;

        // INFORMATION
        [JsonIgnore] public readonly int InputCount;
        [JsonIgnore] public readonly int OutputCount;
        [JsonIgnore] public readonly int TotalCount;
        [JsonIgnore] public readonly int WeightGeneCount;
        [JsonIgnore] public readonly int BiasGeneCount;
        [JsonIgnore] public readonly int GeneCount;
        [JsonIgnore] public readonly int GeneMaxRawValue;
        [JsonIgnore] public readonly int GeneByteSize;


        #region Constructors

        /// <summary>
        /// Initializes a new Genome with the given structure and either randomized genes or zero-valued genes.
        /// </summary>
        public Genome(GenomeStructure structure, bool randomizeGenes = true)
        {
            // CALCULATE INFORMATION
            InputCount = structure.LayerSizes[0];
            OutputCount = structure.LayerSizes[structure.LayerSizes.Length - 1];
            TotalCount = structure.LayerSizes.Sum();
            for (int i = 1; i < structure.LayerSizes.Length; i++) WeightGeneCount += structure.LayerSizes[i] * structure.LayerSizes[i - 1];
            BiasGeneCount = TotalCount - InputCount; // biases for all but input neurons
            GeneCount = BiasGeneCount + WeightGeneCount;
            GeneMaxRawValue = (structure.PrecisionMode == GenomeStructure.GenePrecisionMode.Fast) ? byte.MaxValue : ushort.MaxValue;
            GeneByteSize = (structure.PrecisionMode == GenomeStructure.GenePrecisionMode.Fast) ? 1 : 2;

            // SET SAVEABLE VARIABLES
            LayerSizes = structure.LayerSizes;
            WeightRange = structure.WeightRange;
            BiasRange = structure.BiasRange;
            PrecisionMode = structure.PrecisionMode;
            GeneData = new byte[GeneCount * GeneByteSize];

            // OPTIONALLY RANDOMIZE GENES
            if (randomizeGenes) RandomizeGenes();
        }

        [JsonConstructor]
        public Genome(int[] layerSizes, float weightRange, float biasRange, GenomeStructure.GenePrecisionMode precisionMode, byte[] geneData)
        {
            // CALCULATE INFORMATION
            InputCount = layerSizes[0];
            OutputCount = layerSizes[layerSizes.Length - 1];
            TotalCount = layerSizes.Sum();
            for (int i = 1; i <layerSizes.Length; i++) WeightGeneCount += layerSizes[i] * layerSizes[i - 1];
            BiasGeneCount = TotalCount - InputCount; // biases for all but input neurons
            GeneCount = BiasGeneCount + WeightGeneCount;
            GeneMaxRawValue = (precisionMode == GenomeStructure.GenePrecisionMode.Fast) ? byte.MaxValue : ushort.MaxValue;
            GeneByteSize = (precisionMode == GenomeStructure.GenePrecisionMode.Fast) ? 1 : 2;

            // SET SAVEABLE VARIABLES
            LayerSizes = layerSizes;
            WeightRange = weightRange;
            BiasRange = biasRange;
            PrecisionMode = precisionMode;
            GeneData = geneData;
        }

        #endregion


        public void RandomizeGenes()
        {
            RNG.Shared.NextBytes(GeneData);
        }

        public GenomeStructure GetStructure()
        {
            GenomeStructure result = new GenomeStructure()
            {
                LayerSizes = LayerSizes,
                WeightRange = WeightRange,
                BiasRange = BiasRange,
                PrecisionMode = PrecisionMode
            };
            return result;
        }


        #region Get and Set Gene

        public float GetGeneAsFloat(int index)
        {
            // Setup
            float geneRange = index < BiasGeneCount ? BiasRange : WeightRange;
            int offset = index * GeneByteSize;

            // Decode from little-endian bytes to a raw uint.
            uint raw = 0;
            for (int b = 0; b < GeneByteSize; b++)
            {
                raw |= (uint)(GeneData[offset + b] << (8 * b));
            }

            // Normalize and scale to desired range.
            float normalized = (float)raw / GeneMaxRawValue;
            return (normalized * geneRange * 2) - geneRange;
        }

        public void SetGeneWithFloat(int index, float value)
        {
            // Setup
            float geneRange = index < BiasGeneCount ? BiasRange : WeightRange;
            int offset = index * GeneByteSize;

            // Normalize gene value from [-geneRange, geneRange] to [0,1]
            float normalized = (value + geneRange) / (geneRange * 2);

            // Convert to raw integer in [0, GeneMaxRawValue]
            uint raw = (uint)(normalized * GeneMaxRawValue);
            if (raw > GeneMaxRawValue)
                throw new Exception($"Gene raw value is over the maximum value ({raw} / {GeneMaxRawValue})");

            // Write little-endian bytes
            for (int b = 0; b < GeneByteSize; b++)
            {
                GeneData[offset + b] = (byte)((raw >> (8 * b)) & 0xFFu);
            }
        }

        public uint GetGeneAsRaw(int index)
        {
            // Setup
            int offset = index * GeneByteSize;

            // Decode from little-endian bytes to a raw uint.
            uint raw = 0;
            for (int b = 0; b < GeneByteSize; b++)
            {
                raw |= (uint)(GeneData[offset + b] << (8 * b));
            }

            return raw;
        }

        public void SetGeneWithRaw(int index, uint raw)
        {
            // Setup
            int offset = index * GeneByteSize;

            // Write little-endian bytes
            for (int b = 0; b < GeneByteSize; b++)
            {
                GeneData[offset + b] = (byte)((raw >> (8 * b)) & 0xFFu);
            }
        }

        #endregion
    }
}
