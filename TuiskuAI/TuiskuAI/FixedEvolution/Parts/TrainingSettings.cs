using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Operators;

namespace TuiskuAI.FixedEvolution.Parts
{
    public class TrainingSettings
    {
        public int Population;
        public Crossover Crossover;
        public Mutation Mutation;
        public Selection Selection;

        public TrainingSettings(int population, Crossover crossover, Mutation mutation, Selection selection)
        {
            Population = population;
            Crossover = crossover;
            Mutation = mutation;
            Selection = selection;
        }
    }
}
