using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Parts;
using TuiskuAI.FixedEvolution.Train;
using TuiskuAI.FixedEvolution.Operators;
using TuiskuAI.FixedEvolution.Core;

namespace TuiskuAI.nUnitTests
{
    internal class FixedEvolutionTests
    {
        [Test]
        public void MainTest()
        {
            string trainingPath = "D:\\FixedEvolutionTests\\TrainingTest1";

            if (Directory.Exists(trainingPath)) Directory.Delete(trainingPath, recursive: true);
            TestContext.Out.WriteLine("Started");

            //var crossover = new Crossover("uniform", new Dictionary<string, float>());
            var crossover = new Crossover("blend", new Dictionary<string, float>() { { "alpha", 0.3f } });
            var mutation = new Mutation("classic", new Dictionary<string, float>()
            {
                { "mutationRate", 0.002f },
                { "maxVariationPercentage", 0.002f }
            });
            var selection = new Selection(
                fitnessFactors: new Selection.FitnessFactor[]
                {
                    new Selection.FitnessFactor()
                    {
                        Key = "totalOutput",
                        BaseValue = 0f,
                        Direction = 1,
                        Multiplier = 1f
                    }
                },
                surviveAmount: 50
            );

            var trainingSettings = new TrainingSettings(
                population: 100,
                crossover: crossover,
                mutation: mutation,
                selection: selection
            );

            var generationStorageSettings = new GenerationStorageSettings()
            {
                BatchSize = 10,
                SaveInterval = 3,
                GensPerFile = 25
            };

            var genomeStructure = new GenomeStructure()
            {
                LayerSizes = new int[] { 28, 64, 32, 4 },
                WeightRange = 4f,
                BiasRange = 1f,
                PrecisionMode = GenomeStructure.GenePrecisionMode.Fast
            };

            Training training = new Training(
                trainingPath: trainingPath,
                settings: trainingSettings,
                generationStorageSettings: generationStorageSettings,
                genomeStructure: genomeStructure
            );

            TestContext.Out.WriteLine("Setup finished");

            try
            {
                // Run gens
                for (int i = 0; i < 200; i++)
                {
                    Brain[] brains = training.NextGeneration();

                    // Run brains
                    foreach (Brain brain in brains)
                    {
                        float[] inputs = new float[brain.Genome.InputCount];
                        for (int j = 0; j < inputs.Length; j++) inputs[j] = Random.Shared.NextSingle();
                        float[] outputs = brain.Update(inputs);

                        float totalOutput = 0f;
                        for (int j = 0; j < outputs.Length; j++) totalOutput += outputs[j];

                        brain.Evaluation.Data["totalOutput"] = totalOutput;
                    }

                    training.FinalizeGeneration();
                    float averageFitness = training.GenerationStorage.GetLastGeneration()!.GetAverageFitness();
                    TestContext.Out.WriteLine($"Generation {training.CurrentGenerationNumber} finalized with average fitness: {averageFitness:F2}");
                }
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}
