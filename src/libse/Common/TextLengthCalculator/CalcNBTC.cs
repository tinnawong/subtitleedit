using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Core.Common.TextLengthCalculator
{
    public class CalcNBTC : ICalcLength
    {
        /// <summary>
        /// Calculate length according to Office of The National Broadcasting and Telecommunications Commission (NBTC).
        /// </summary>
        public decimal CountCharacters(string text, bool forCps)
        {
            int length = text.Length;
            string pattern = @"\p{Mn}";
            RegexOptions options = RegexOptions.Multiline;
            int lengthChar = Regex.Matches(text, pattern, options).Count;
            return length - lengthChar;
        }
    }
}

