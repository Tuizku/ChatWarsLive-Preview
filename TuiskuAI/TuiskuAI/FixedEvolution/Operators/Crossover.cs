using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Core;

namespace TuiskuAI.FixedEvolution.Operators
{
    public class Crossover
    {
        public string UsedCrossover;
        public Dictionary<string, float> Values;

        #region Constructor

        /// <summary>
        /// Creates a new instance of a Crossover class, with set crossover function name and it's values.
        /// (uniform = no values), (blend = alpha [0.0 - 1.0] value) 
        /// </summary>
        /// <param name="usedCrossover">the name of the used crossover function</param>
        /// <param name="values">the values that this crossover function expects</param>
        /// <exception cref="ArgumentException"></exception>
        [JsonConstructor]
        public Crossover(string usedCrossover, Dictionary<string, float> values)
        {
            // VALIDATE
            switch (usedCrossover)
            {
                case "uniform":
                    break;
                case "blend":
                    if (values.ContainsKey("alpha") == false)
                        throw new ArgumentException("Uniform crossover requires an alpha value, which was not given...");
                    break;
                default:
                    throw new ArgumentException($"No crossover exists with the name: {usedCrossover}...");
            }

            UsedCrossover = usedCrossover;
            Values = values;
        }

        #endregion



        public Genome[] Perform(Genome[] input, int population)
        {
            switch (UsedCrossover)
            {
                case "uniform":
                    return UniformCrossover(input, population);
                case "blend":
                    return BlendCrossover(input, population, Values["alpha"]);
                default:
                    throw new Exception($"This crossover function doesn't exist: {UsedCrossover}...");
            }
        }



        #region Setup Crossover

        private struct ParentPair
        {
            public Genome ParentA;
            public Genome ParentB;
            public int OffspringCount;
        }

        private void SetupCrossover(Genome[] _input, int _population,
            out Genome[] output, out int outputIndex, out ParentPair[] parentPairs)
        {
            if (_input == null) throw new ArgumentNullException(nameof(_input));
            if (_input.Length <= 1) throw new ArgumentException("Too few genomes for a crossover");

            // Shuffle input genomes
            Genome[] input = Helpers.ShallowCloneArray(_input);
            Helpers.ShuffleArray(input);

            output = new Genome[_population];


            int parentCount = input.Length;
            int population = output.Length;
            int childrenLeft = population;

            int inputIndex = 0;
            outputIndex = 0;


            // Move single parent directly to output if odd number of parents
            if (parentCount % 2 == 1)
            {
                output[outputIndex++] = input[inputIndex++];
                parentCount--;
                childrenLeft--;
            }


            int childrenPerCouple = (int)(childrenLeft / (float)parentCount * 2f);
            childrenLeft -= childrenPerCouple * (parentCount / 2);

            // Create the parent pairs, with assigned number of offspring
            parentPairs = new ParentPair[parentCount / 2];
            for (int i = 0; i < parentPairs.Length; i++)
            {
                int children = childrenPerCouple;
                if (childrenLeft > 0)
                {
                    children++;
                    childrenLeft--;
                }

                parentPairs[i] = new ParentPair()
                {
                    ParentA = input[inputIndex++],
                    ParentB = input[inputIndex++],
                    OffspringCount = children
                };
            }
        }

        #endregion

        #region Crossovers

        private Genome[] UniformCrossover(Genome[] input, int population)
        {
            SetupCrossover(input, population,
                out Genome[] output,
                out int outputIndex,
                out ParentPair[] parentPairs);


            foreach (ParentPair pair in parentPairs)
            {
                for (int i = 0; i < pair.OffspringCount; i++)
                {
                    Genome child = new Genome(pair.ParentA.GetStructure());

                    // Set genes using uniform crossover
                    for (int g = 0; g < child.GeneCount; g++)
                    {
                        // 50% chance to take gene from either parent
                        if (RNG.Shared.NextDouble() < 0.5)
                            child.SetGeneWithRaw(g, pair.ParentA.GetGeneAsRaw(g));
                        else
                            child.SetGeneWithRaw(g, pair.ParentB.GetGeneAsRaw(g));
                    }

                    // Add child to output
                    output[outputIndex++] = child;
                }
            }

            return output;
        }


        private Genome[] BlendCrossover(Genome[] input, int population, float alpha)
        {
            SetupCrossover(input, population,
                out Genome[] output,
                out int outputIndex,
                out ParentPair[] parentPairs);

            foreach (ParentPair pair in parentPairs)
            {
                for (int i = 0; i < pair.OffspringCount; i++)
                {
                    Genome child = new Genome(pair.ParentA.GetStructure());

                    // Create child weight genes using blending (BLX)
                    for (int g = 0; g < child.GeneCount; g++)
                    {
                        uint parentARawGene = pair.ParentA.GetGeneAsRaw(g);
                        uint parentBRawGene = pair.ParentB.GetGeneAsRaw(g);

                        int min = (int)Math.Min(parentARawGene, parentBRawGene);
                        int max = (int)Math.Max(parentARawGene, parentBRawGene);
                        int distance = max - min;

                        int low = min - (int)(alpha * distance);
                        int high = max + (int)(alpha * distance);

                        if (low < 0) low = 0;
                        if (high > child.GeneMaxRawValue) high = child.GeneMaxRawValue;

                        uint newRaw = (uint)RNG.Shared.Next(low, high + 1);
                        child.SetGeneWithRaw(g, newRaw);
                    }

                    output[outputIndex++] = child;
                }
            }

            return output;
        }

        #endregion
    }
}
