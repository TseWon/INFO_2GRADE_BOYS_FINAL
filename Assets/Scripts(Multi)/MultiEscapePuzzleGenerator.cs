using System;

namespace FacilityEscape
{
    public static class MultiEscapePuzzleGenerator
    {
        public static void Generate(Random random, MultiEscapeRules previous,
            out LockColor[] colors, out DialSymbol[] symbols)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            colors = new[] { LockColor.Red, LockColor.Blue, LockColor.Yellow, LockColor.Green };
            for (int i = colors.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var swap = colors[i]; colors[i] = colors[j]; colors[j] = swap;
            }
            bool repeated = previous != null && previous.ColorCount == colors.Length;
            for (int i = 0; repeated && i < colors.Length; i++)
                repeated = colors[i] == previous.GetColorAnswer(i);
            if (repeated)
            {
                var swap = colors[0]; colors[0] = colors[1]; colors[1] = swap;
            }

            // 6진수로 기호 조합 선택. 초기 상태 O/O/O와 직전 정답은 제외합니다.
            int previousCode = 0, factor = 1;
            for (int i = 0; previous != null && i < MultiEscapeRules.DialCount; i++)
            {
                previousCode += (int)previous.GetSymbolAnswer(i) * factor;
                factor *= MultiEscapeRules.SymbolCount;
            }
            int combinations = MultiEscapeRules.SymbolCount * MultiEscapeRules.SymbolCount * MultiEscapeRules.SymbolCount;
            int code = random.Next(1, previousCode > 0 ? combinations - 1 : combinations);
            if (previousCode > 0 && code >= previousCode) code++;
            symbols = new DialSymbol[MultiEscapeRules.DialCount];
            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i] = (DialSymbol)(code % MultiEscapeRules.SymbolCount);
                code /= MultiEscapeRules.SymbolCount;
            }
        }
    }
}
