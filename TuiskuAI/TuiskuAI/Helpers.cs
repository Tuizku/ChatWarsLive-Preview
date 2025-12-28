using System;
using System.Collections.Generic;
using System.Text;

namespace TuiskuAI
{
    internal static class Helpers
    {

        public static void ShuffleArray<T>(T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = RNG.Shared.Next(0, n + 1);
                (array[n], array[k]) = (array[k], array[n]); // swap
            }
        }

        public static T[] ShallowCloneArray<T>(T[] array)
        {
            T[] result = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[i];
            }
            return result;
        }

    }
}
