internal sealed class UniqueKeyGenerator<TKey>(Func<TKey> generator)
    where TKey : notnull
{
    /// <summary>
    /// Generate a unique key.
    /// </summary>
    /// <returns>The unique key that was generated.</returns>
    public TKey GenerateKey() => generator();
}
