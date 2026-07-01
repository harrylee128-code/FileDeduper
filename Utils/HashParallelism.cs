using System;

namespace FileDeduper.Utils
{
    internal static class HashParallelism
    {
        public const int Auto = 0;
        public const int Maximum = 64;

        public static int NormalizeForSettings(int requested)
        {
            if (requested <= 0) return Auto;
            return Math.Min(requested, Maximum);
        }

        public static int Resolve(int requested)
        {
            if (requested <= Auto)
            {
                return Math.Max(1, Math.Min(Environment.ProcessorCount, 4));
            }
            return Math.Max(1, Math.Min(requested, Maximum));
        }

        public static string Describe(int requested)
        {
            int effective = Resolve(requested);
            if (requested <= Auto)
            {
                return "hash parallelism: Auto (" + effective + ")";
            }
            return "hash parallelism: " + effective;
        }
    }
}
