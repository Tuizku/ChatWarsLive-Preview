using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using TuiskuAI.FixedEvolution.Parts;

namespace TuiskuAI.FixedEvolution.Train
{
    public class GenerationStorage
    {
        #region GenerationFile class
        public class GenerationFile
        {
            public string FilePath;
            public int Min;
            public int Max;

            public GenerationFile(string filePath)
            {
                FilePath = filePath;
                Min = -1;
                Max = -1;

                List<Generation> generations = Load();
                UpdateMinAndMax(generations);
            }

            public List<Generation> Load()
            {
                // Makes sure that the file exists, if not, return an empty list
                if (File.Exists(FilePath) == false) return new List<Generation>();

                // Read from file and deserialize
                string json = File.ReadAllText(FilePath);
                List<Generation> result = JsonConvert.DeserializeObject<List<Generation>>(json)!;

                // Update and return
                UpdateMinAndMax(result);
                return result;
            }

            public void Save(List<Generation> generations)
            {
                // Makes sure the parent directory exists
                string dir = Path.GetDirectoryName(FilePath);
                if (Directory.Exists(dir) == false) throw new DirectoryNotFoundException(dir);

                // Serialize and write to file
                string json = JsonConvert.SerializeObject(generations);
                File.WriteAllText(FilePath, json);

                // Update
                UpdateMinAndMax(generations);
            }

            public void Delete()
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                FilePath = string.Empty;
                Min = -1;
                Max = -1;
            }

            private void UpdateMinAndMax(List<Generation> generations)
            {
                // Min gets -1 if the file has no generations, else int.MaxValue
                Min = generations.Count != 0 ? int.MaxValue : -1;
                Max = -1;

                // Finds the Min and Max by looping all gens.
                foreach (Generation gen in generations)
                {
                    if (gen.GenerationNumber < Min) Min = gen.GenerationNumber;
                    if (gen.GenerationNumber > Max) Max = gen.GenerationNumber;
                }
            }
        }
        #endregion

        private string GetGenFilePath(int index) { return Path.Combine(StoragePath, $"genfile_{index}.json"); }

        public string StoragePath;
        public GenerationStorageSettings Settings;
        public List<Generation> GenerationBatch;
        public List<GenerationFile> GenerationFiles;

        public GenerationStorage(string storagePath, GenerationStorageSettings settings)
        {
            StoragePath = storagePath;
            Settings = settings;
            GenerationBatch = new List<Generation>();
            GenerationFiles = new List<GenerationFile>();

            // Load the generation files
            int index = 0;
            while (File.Exists(GetGenFilePath(index)))
            {
                string path = GetGenFilePath(index);
                GenerationFiles.Add(new GenerationFile(path));
                index++;
            }
        }



        
        /// <summary>
        /// Loads the generations from the last file and returns the last generation in the list.
        /// If there are no generation files or there were 0 generations in the last file, returns null.
        /// </summary>
        public Generation? GetLastGeneration()
        {
            // Try to get the last generation from batch first
            if (GenerationBatch.Count > 0) return GenerationBatch[GenerationBatch.Count - 1];

            // Get the last generation from file
            if (GenerationFiles.Count == 0) return null;
            List<Generation> generations = GenerationFiles[GenerationFiles.Count - 1].Load();
            if (generations.Count == 0) return null;
            return generations[generations.Count - 1];
        }

        /// <summary>
        /// Loads the generations from the last file and returns the last generation's number in the list.
        /// If there are no generation files or there were 0 generations in the last file, returns null.
        /// </summary>
        public int? GetLastGenerationNumber()
        {
            // Get the last generation number from batch first
            if (GenerationBatch.Count > 0) return GenerationBatch[GenerationBatch.Count - 1].GenerationNumber;

            // Get the last generation number from file
            if (GenerationFiles.Count == 0) return null;
            List<Generation> generations = GenerationFiles[GenerationFiles.Count - 1].Load();
            if (generations.Count == 0) return null;
            return generations[generations.Count - 1].GenerationNumber;
        }

        public void AddGeneration(Generation generation)
        {
            GenerationBatch.Add(generation);
            if (GenerationBatch.Count >= Settings.BatchSize) SaveBatch();
        }

        public void RevertToGeneration(int generationNumber)
        {
            throw new NotImplementedException();
        }

        public void SaveBatch()
        {
            GenerationFile? genFile = null;
            List<Generation> genFileGens = new List<Generation>();

            for (int i = 0; i < GenerationBatch.Count; i++)
            {
                Generation gen = GenerationBatch[i];

                // Save based on the interval, and if it's the last gen in batch
                if (gen.GenerationNumber % Settings.SaveInterval == 0 ||
                    i == GenerationBatch.Count - 1)
                {

                    // LOAD GEN FILE OR CREATE NEW
                    if (genFile == null && GenerationFiles.Count == 0)
                    {
                        genFile = new GenerationFile(GetGenFilePath(0));
                        genFileGens = genFile.Load();
                        GenerationFiles.Add(genFile);
                    }
                    else if (genFile == null)
                    {
                        genFile = GenerationFiles[GenerationFiles.Count - 1];
                        genFileGens = genFile.Load();
                    }
                    
                    // SAVE AND CREATE NEW GEN FILE IF IT REACHES MAX SIZE
                    if (genFileGens.Count >= Settings.GensPerFile)
                    {
                        genFile.Save(genFileGens);
                        genFile = new GenerationFile(GetGenFilePath(GenerationFiles.Count));
                        genFileGens = genFile.Load();
                        GenerationFiles.Add(genFile);
                    }

                    genFileGens.Add(gen);
                }
            }

            // Saves the lastly used genfile!
            if (genFile != null) genFile.Save(genFileGens);
            
            GenerationBatch.Clear();
        }
    }
}
