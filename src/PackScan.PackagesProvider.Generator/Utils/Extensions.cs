namespace PackScan.PackagesProvider.Generator.Utils;

internal static class Extensions
{
    public static string[] SplitByLine(this string s, StringSplitOptions options)
        => s.Split(new string[] { "\r\n", "\r", "\n" }, options);

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        => (key, value) = (kvp.Key, kvp.Value);
}
