using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Core;

namespace TuiskuAI.FixedEvolution.Operators
{
    public class Mutation
    {
        public string UsedMutation;
        public Dictionary<string, float> Values;

        #region Constructors

        /// <summary>
        /// Creates a new instance of a Mutation class, with set mutation function name and it's values.
        /// (classic = mutationRate [0.0 - 1.0] and maxVariationPercentage [0.0+] values)
        /// </summary>
        /// <param name="usedMutation"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentException"></exception>
        [JsonConstructor]
        public Mutation(string usedMutation, Dictionary<string, float> values)
        {
            // VALIDATE
            switch (usedMutation)
            {
                case "classic":
                    if (values.ContainsKey("mutationRate") == false)
                        throw new ArgumentException("Classic Mutation requires a mutationRate value, which was not given");
                    if (values.ContainsKey("maxVariationPercentage") == false)
                        throw new ArgumentException("Classic Mutation requires a maxVariationPercentage value, which was not given");
                    if (values["mutationRate"] < 0f || values["mutationRate"] > 1f)
                        throw new ArgumentException("mutationRate must be between 0.0 and 1.0 in Classic Mutation!");
                    if (values["maxVariationPercentage"] < 0) 
                        throw new ArgumentException("maxVariationPercentage must be 0.0 or above in Classic Mutation!");
                    break;
                default:
                    throw new ArgumentException($"No mutation exists with the name: {usedMutation}...");
            }

            UsedMutation = usedMutation;
            Values = values;
        }

        #endregion



        public void Perform(Genome[] input)
        {
            switch (UsedMutation)
            {
                case "classic":
                    ClassicMutation(input, Values["mutationRate"], Values["maxVariationPercentage"]);
                    break;
                default:
                    throw new Exception($"This mutation function doesn't exist: {UsedMutation}...");
            }
        }



        #region Mutations

        /// <summary>
        /// Applies random bitwise mutations to the gene data of each genome in the inputted array, using the given
        /// mutation rate and variation percentage.
        /// </summary>
        /// <remarks>For each genome, the actual mutation rate is randomly adjusted within the range
        /// defined by the mutation rate and the maximum variation percentage. Each gene byte may have at most one bit
        /// flipped per mutation. This method modifies the input genomes in place.</remarks>
        /// <param name="genomes">An array of genomes whose gene data will be mutated. Each genome in the array will be processed
        /// independently.</param>
        /// <param name="mutationRate">The base probability, between 0.0 and 1.0, that a mutation will occur for each gene byte in a genome.</param>
        /// <param name="maxVariationPercentage">The maximum percentage by which the mutation rate can vary randomly for each genome. Must be 0.0 or above</param>
        public void ClassicMutation(Genome[] genomes, float mutationRate, float maxVariationPercentage)
        {
            // ARGUMENT ERROR CHECKS
            if (genomes.Length == 0) throw new ArgumentException("Inputted genomes must include at least one genome! Now 0");
            if (mutationRate < 0f || mutationRate > 1f)
                throw new ArgumentException($"MutationRate must be between 0.0 and 1.0! Now = {mutationRate}");
            if (maxVariationPercentage < 0f)
                throw new ArgumentException($"MaxVariationPercentage must be 0.0 or above! Now = {maxVariationPercentage}");


            foreach (Genome genome in genomes)
            {
                // Calculate the variation and final mutation rate for this genome
                float variation = (((float)RNG.Shared.NextDouble() * 2f) - 1f) * maxVariationPercentage;
                float finalMutationRate = Math.Clamp(mutationRate + (mutationRate * variation), 0f, 1f);

                // Loop all gene bytes and mutate if that happens to happen
                for (int geneIndex = 0; geneIndex < genome.GeneData.Length; geneIndex++)
                {
                    // Creates a mutation mask, there will be 1 bit randomly on if a mutation will happen
                    byte mask = 0;
                    if (RNG.Shared.NextDouble() < finalMutationRate)
                    {
                        mask = (byte)(1 << RNG.Shared.Next(8));
                    }

                    // Flip that one bit in the genome if a mutation happened.
                    genome.GeneData[geneIndex] ^= mask;
                }
            }
        }

        #endregion
    }
}
