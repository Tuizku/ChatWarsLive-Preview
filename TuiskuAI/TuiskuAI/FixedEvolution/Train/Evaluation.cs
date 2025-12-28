using System;
using System.Collections.Generic;
using System.Text;

namespace TuiskuAI.FixedEvolution.Train
{
    public class Evaluation
    {
        public bool Survived = false;
        public float Fitness = 0f;
        public Dictionary<string, float> Data = new Dictionary<string, float>();

        public void AddToOrSetData(string key, float value)
        {
            if (Data.ContainsKey(key)) Data[key] += value;
            else Data.Add(key, value);
        }
    }
}
