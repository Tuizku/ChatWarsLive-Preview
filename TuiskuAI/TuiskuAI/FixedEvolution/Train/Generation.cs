using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Core;
using TuiskuAI.FixedEvolution.Parts;

namespace TuiskuAI.FixedEvolution.Train
{
    public class Generation
    {
        public int GenerationNumber;
        public int Population;

        public Genome[] Genomes;
        public Evaluation[] Evaluations;


        #region Constructors

        /// <summary>
        /// Creates a new Generation instance with the gen number and takes genomes and evaluations from the given brains.
        /// </summary>
        /// <param name="generationNumber">Starts with 0</param>
        public Generation(int generationNumber, Brain[] brains)
        {
            GenerationNumber = generationNumber;
            Population = brains.Length;

            Genomes = new Genome[brains.Length];
            Evaluations = new Evaluation[brains.Length];
            for (int i = 0; i < brains.Length; i++)
            {
                Genomes[i] = brains[i].Genome;
                Evaluations[i] = brains[i].Evaluation;
            }
        }

        [JsonConstructor]
        public Generation(int generationNumber, int population, Genome[] genomes, Evaluation[] evaluations)
        {
            GenerationNumber = generationNumber;
            Population = population;
            Genomes = genomes;
            Evaluations = evaluations;
        }

        #endregion



        public Genome[] GetSurvivedGenomes()
        {
            List<Genome> survived = new List<Genome>(Population);
            for (int i = 0; i < Population; i++)
            {
                if (Genomes[i] == null || Evaluations[i] == null)
                    throw new Exception("Genomes and Evaluations can't have null values when trying to get survived ones...");

                if (Evaluations[i].Survived) survived.Add(Genomes[i]);
            }

            return survived.ToArray();
        }

        public float GetAverageFitness()
        {
            float totalFitness = 0;
            for (int i = 0; i < Population; i++)
            {
                if (Evaluations[i] == null)
                    throw new Exception("Evaluations can't have null values when trying to get average fitness...");
                totalFitness += Evaluations[i].Fitness;
            }
            return totalFitness / Population;
        }
    }
}
