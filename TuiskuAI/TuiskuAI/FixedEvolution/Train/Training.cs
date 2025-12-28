using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using TuiskuAI.FixedEvolution.Core;
using TuiskuAI.FixedEvolution.Parts;

namespace TuiskuAI.FixedEvolution.Train
{
    public class Training
    {
        // SETTINGS
        public TrainingSettings Settings;
        public GenerationStorageSettings GenerationStorageSettings;
        public GenomeStructure GenomeStructure;

        // OTHER
        public GenerationStorage GenerationStorage;
        public int CurrentGenerationNumber;
        private Brain[]? currentBrains;

        // FILES
        public readonly string TrainingPath;
        public readonly string TrainingName;
        public readonly string SettingsPath;


        #region Constructors

        /// <summary>
        /// Creates a new Training instance. The training path directory must not exist yet!
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public Training(
            string trainingPath, 
            TrainingSettings settings, 
            GenerationStorageSettings generationStorageSettings, 
            GenomeStructure genomeStructure)
        {
            // TRAINING DIRECTORY (CREATE)
            if (Directory.Exists(trainingPath)) throw new ArgumentException("A training already exists in this path, failed to create a new one...");
            Directory.CreateDirectory(trainingPath);

            // SET VARIABLES
            TrainingPath = trainingPath;
            TrainingName = Path.GetFileName(trainingPath);
            SettingsPath = Path.Combine(TrainingPath, "settings.json");
            Settings = settings;
            GenerationStorageSettings = generationStorageSettings;
            GenomeStructure = genomeStructure;

            // SETUP GENERATIONS
            GenerationStorage = new GenerationStorage(trainingPath, generationStorageSettings);
            CurrentGenerationNumber = -1;
            currentBrains = null;

            // SAVE
            SaveSettings();
        }

        
        /// <summary>
        /// Creates a new Training instance, by loading a Training directory from file.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public Training(string trainingPath)
        {
            // TRAINING DIRECTORY (ENSURE EXISTENCE)
            if (Directory.Exists(trainingPath) == false) throw new ArgumentException($"Training doesn't exist in path {trainingPath}...");

            // SET VARIABLES (some to default for avoiding warnings)
            TrainingPath = trainingPath;
            TrainingName = Path.GetFileName(trainingPath);
            SettingsPath = Path.Combine(TrainingPath, "settings.json");
            Settings = null!;
            GenerationStorageSettings = null!;
            GenomeStructure = new GenomeStructure();

            // LOAD
            LoadSettings();

            // SETUP GENERATIONS
            GenerationStorage = new GenerationStorage(trainingPath, GenerationStorageSettings);
            CurrentGenerationNumber = GenerationStorage.GetLastGenerationNumber() ?? -1;
            currentBrains = null;
        }

        #endregion

        /// <summary>
        /// Creates a new generation and it's brains. If the last generation was not finalized, it will do it here as well.
        /// </summary>
        /// <returns>The brains of the new generation</returns>
        public Brain[] NextGeneration()
        {
            Generation? lastSavedGeneration = GenerationStorage.GetLastGeneration();

            // Case 1: Create random brains for the first generation
            if (currentBrains == null && lastSavedGeneration == null)
            {
                Genome[] genomes = new Genome[Settings.Population];
                for (int i = 0; i < Settings.Population; i++) genomes[i] = new Genome(GenomeStructure);

                currentBrains = new Brain[Settings.Population];
                for (int i = 0; i < Settings.Population; i++) currentBrains[i] = new Brain(genomes[i]);
            }

            // Case 2: Use last generation from storage to create the new generation.
            else if (currentBrains == null)
            {
                // Crossover + Mutation
                Genome[] newGenomes = Settings.Crossover.Perform(lastSavedGeneration!.GetSurvivedGenomes(), Settings.Population);
                Settings.Mutation.Perform(newGenomes);

                // Create new brains with the new genomes
                currentBrains = new Brain[Settings.Population];
                for (int i = 0; i < Settings.Population; i++) currentBrains[i] = new Brain(newGenomes[i]);
            }

            // Case 3: Turn brains into a Generation and run the genetic operators for new brains.
            else
            {
                // Evaluate + Turn to Generation and add to storage
                Settings.Selection.Evaluate(currentBrains);
                Generation finishedGeneration = new Generation(CurrentGenerationNumber, currentBrains);
                GenerationStorage.AddGeneration(finishedGeneration);

                // Crossover + Mutation
                Genome[] newGenomes = Settings.Crossover.Perform(finishedGeneration.GetSurvivedGenomes(), Settings.Population);
                Settings.Mutation.Perform(newGenomes);

                // Create new brains with the new genomes
                currentBrains = new Brain[Settings.Population];
                for (int i = 0; i < Settings.Population; i++) currentBrains[i] = new Brain(newGenomes[i]);
            }

            CurrentGenerationNumber++;
            return currentBrains;
        }

        /// <summary>
        /// Finalizes the current generation by turning the current brains into a Generation instance and evaluating.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void FinalizeGeneration()
        {
            if (currentBrains == null) throw new Exception("No current brains exist, cannot finalize generation...");

            // Evaluate + Turn to Generation and add to storage
            Settings.Selection.Evaluate(currentBrains);
            Generation finishedGeneration = new Generation(CurrentGenerationNumber, currentBrains);
            GenerationStorage.AddGeneration(finishedGeneration);

            // Clear current brains
            currentBrains = null;
        }

        public void Save()
        {
            GenerationStorage.SaveBatch();
        }

        #region Save and Load Settings

        private struct SettingsPack
        {
            public TrainingSettings Settings;
            public GenerationStorageSettings GenerationStorageSettings;
            public GenomeStructure GenomeStructure;
        }
        private void SaveSettings()
        {
            // turn settings into a pack and save that as json
            SettingsPack settingsPack = new SettingsPack()
            {
                Settings = Settings,
                GenerationStorageSettings = GenerationStorageSettings,
                GenomeStructure = GenomeStructure
            };
            string json = JsonConvert.SerializeObject(settingsPack, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
        private void LoadSettings()
        {
            // load
            string json = File.ReadAllText(SettingsPath);
            SettingsPack settingsPack = JsonConvert.DeserializeObject<SettingsPack>(json);

            // set
            Settings = settingsPack.Settings;
            GenerationStorageSettings = settingsPack.GenerationStorageSettings;
            GenomeStructure = settingsPack.GenomeStructure;
        }

        #endregion
    }
}
