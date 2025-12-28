using System;
using System.Collections.Generic;
using System.Text;

using TuiskuAI.FixedEvolution.Train;

namespace TuiskuAI.FixedEvolution.Core
{
    public class Brain
    {
        #region Structs
        private struct CompiledInstruction
        {
            public enum InstructionType : byte
            {
                Connection,
                Finalize,
                Reset
            }

            public InstructionType Type;
            public int NeuronIndex;
            public int SourceIndex;
            public float Value;
        }
        #endregion


        public readonly Genome Genome;
        public readonly Evaluation Evaluation;

        private float[] activations;
        private CompiledInstruction[] compiledInstructions;

        private readonly float[] outputs;
        private readonly int firstOutputIndex;

        /// <summary>
        /// Creates a new Brain instance with the given genome and compiles it.
        /// </summary>
        public Brain(Genome genome)
        {
            Genome = genome;
            Evaluation = new Evaluation();

            outputs = new float[Genome.OutputCount];
            firstOutputIndex = Genome.TotalCount - Genome.OutputCount;

            activations = new float[Genome.TotalCount];
            compiledInstructions = new CompiledInstruction[CalculateInstructionCount()];

            Compile();
        }



        private void Compile()
        {
            int[] layerSizes = Genome.LayerSizes;

            int instructionIndex = 0;
            int weightIndex = Genome.BiasGeneCount;
            int biasIndex = 0;
            int layerFirstNeuron = layerSizes[0];
            int lastLayerFirstNeuron = 0;

            for (int layer = 1; layer < layerSizes.Length; layer++)
            {
                for (int neuron = layerFirstNeuron; neuron < layerFirstNeuron + layerSizes[layer]; neuron++)
                {
                    // Add a reset instruction
                    compiledInstructions[instructionIndex++] = new CompiledInstruction()
                    {
                        Type = CompiledInstruction.InstructionType.Reset,
                        NeuronIndex = neuron,
                        SourceIndex = -1,
                        Value = 0f
                    };

                    // Add connection instructions
                    for (int source = lastLayerFirstNeuron; source < layerFirstNeuron; source++)
                    {
                        compiledInstructions[instructionIndex++] = new CompiledInstruction()
                        {
                            Type = CompiledInstruction.InstructionType.Connection,
                            NeuronIndex = neuron,
                            SourceIndex = source,
                            Value = Genome.GetGeneAsFloat(weightIndex++)
                        };
                    }

                    // Add a finalize instruction
                    compiledInstructions[instructionIndex++] = new CompiledInstruction()
                    {
                        Type = CompiledInstruction.InstructionType.Finalize,
                        NeuronIndex = neuron,
                        SourceIndex = -1,
                        Value = Genome.GetGeneAsFloat(biasIndex++)
                    };
                }

                layerFirstNeuron += layerSizes[layer];
                lastLayerFirstNeuron += layerSizes[layer - 1];
            }
        }

        private int CalculateInstructionCount()
        {
            int result = 0;
            int[] layerSizes = Genome.LayerSizes;
            for (int layer = 1; layer < layerSizes.Length; layer++)
            {
                result += layerSizes[layer - 1] * layerSizes[layer]; // conns commands
                result += layerSizes[layer] * 2; // reset + finalize commands
            }
            return result;
        }



        public float[] Update(float[] inputs)
        {
            // Apply inputs
            if (inputs.Length != Genome.InputCount)
                throw new ArgumentException($"Wrong amount of inputs... ({inputs.Length} / {Genome.InputCount})");
            for (int i = 0; i < inputs.Length; i++) activations[i] = inputs[i];



            // Execute compiled instructions
            // --------------------------------------------------------------------------

            CompiledInstruction[] instructions = compiledInstructions;
            int instructionCount = instructions.Length;
            float[] a = activations;

            for (int i = 0; i < instructionCount; i++)
            {
                ref readonly CompiledInstruction instruction = ref instructions[i];

                // Connection -> Add weighted activation from source neuron to target neuron
                if (instruction.Type == CompiledInstruction.InstructionType.Connection)
                {
                    a[instruction.NeuronIndex] += a[instruction.SourceIndex] * instruction.Value;
                }

                // Reset -> Set target neuron activation to zero
                else if (instruction.Type == CompiledInstruction.InstructionType.Reset)
                {
                    a[instruction.NeuronIndex] = 0f;
                }

                // Finalize -> Apply bias and activation function (tanh) to target neuron
                else if (instruction.Type == CompiledInstruction.InstructionType.Finalize)
                {
                    a[instruction.NeuronIndex] = MathF.Tanh(a[instruction.NeuronIndex] + instruction.Value);
                }
            }

            // --------------------------------------------------------------------------



            // Set outputs and return
            for (int i = 0; i < outputs.Length; i++)
            {
                outputs[i] = activations[firstOutputIndex + i];
            }
            return outputs;
        }
    }
}
