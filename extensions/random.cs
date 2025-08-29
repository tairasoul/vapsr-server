namespace VapSRServer.Extensions;

// Modified version of https://stackoverflow.com/a/110570

static class RandomExtensions
{
  public static T[] CloneShuffle<T>(this Random rng, T[] array)
  {
    T[] clone = (T[])array.Clone();
    int n = clone.Length;
    while (n > 1)
    {
      int k = rng.Next(n--);
      T temp = clone[n];
      clone[n] = clone[k];
      clone[k] = temp;
    }
    return clone;
  }
}