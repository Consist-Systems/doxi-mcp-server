
namespace ApryseDataExtractor
{
    public static class RtlTextExtensions
    {
        private static readonly char[] Hebrew = Enumerable.Range(0x0590, 0x05FF - 0x0590).Select(i => (char)i).ToArray();
        private static readonly char[] Arabic = Enumerable.Range(0x0600, 0x06FF - 0x0600).Select(i => (char)i).ToArray();

        private static bool IsRtlChar(char c) =>
            Hebrew.Contains(c) || Arabic.Contains(c);

        private static bool IsRtlWord(string w) =>
            w.Any(IsRtlChar);

        public static string FixRtl(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (!text.Any(IsRtlChar))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 1) תקן אותיות הפוכות בתוך כל מילה RTL
            for (int i = 0; i < words.Length; i++)
            {
                if (IsRtlWord(words[i]))
                {
                    char[] arr = words[i].ToCharArray();
                    Array.Reverse(arr);
                    words[i] = new string(arr);
                }
            }

            // 2) אם כל המשפט RTL → הפוך גם סדר מילים
            if (words.All(w => IsRtlWord(w)))
                return string.Join(" ", words.Reverse());

            // 3) במקרים מעורבים, רק לתקן אותיות
            return string.Join(" ", words);
        }
    }
}
