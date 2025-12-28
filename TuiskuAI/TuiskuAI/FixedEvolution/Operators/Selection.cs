using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Core;
using TuiskuAI.FixedEvolution.Train;

namespace TuiskuAI.FixedEvolution.Operators
{
    public class Selection
    {
        #region Parts
        public struct FitnessFactor
        {
            public string Key;
            public float BaseValue;
            public int Direction;
            public float Multiplier;
        }
        #endregion

        // Variables
        public FitnessFactor[] FitnessFactors;
        public int SurviveAmount;

        [JsonConstructor]
        public Selection(FitnessFactor[] fitnessFactors, int surviveAmount)
        {
            FitnessFactors = fitnessFactors;
            SurviveAmount = surviveAmount;
        }



        public void Evaluate(Brain[] brains)
        {
            // Calculate fitness
            foreach (Brain brain in brains)
            {
                Evaluation evaluation = brain.Evaluation;
                evaluation.Survived = false;
                evaluation.Fitness = 0;

                foreach (var factor in FitnessFactors)
                {
                    if (!evaluation.Data.ContainsKey(factor.Key)) continue;

                    // Calculate the distance from base, and adjust by factor direction.
                    float value = evaluation.Data[factor.Key];
                    float distanceFromBase = (value - factor.BaseValue) * factor.Direction;
                    if (distanceFromBase < 0) continue;

                    // Add the distance * multiplier to fitness
                    evaluation.Fitness += distanceFromBase * factor.Multiplier;
                }
            }

            // Let SurviveAmount of brains survive, starting from the one with the best fitness.
            Array.Sort(brains, (a, b) => b.Evaluation.Fitness.CompareTo(a.Evaluation.Fitness));
            for (int i = 0; i < SurviveAmount; i++)
            {
                brains[i].Evaluation.Survived = true;
            }
        }
    }
}
