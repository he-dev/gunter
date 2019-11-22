namespace Gunter.Extensions
{
    internal static class StringExtensions
    {
        public static string FormatPartialName(this string input)
        {
            return $"_{input.TrimStart('_')}";
        }        
    }
}
