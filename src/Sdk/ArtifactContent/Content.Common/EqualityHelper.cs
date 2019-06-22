namespace GitHub.Services.Content.Common
{
    public static class EqualityHelper
    {
        public static int GetCombinedHashCode(params object[] objs)
        {
            var result = 23;
            unchecked
            {
                foreach (var x in objs)
                {
                    result += object.ReferenceEquals(x, null) ? 0 : x.GetHashCode() * 17;
                }
            }
            return result;
        }
    }
}
