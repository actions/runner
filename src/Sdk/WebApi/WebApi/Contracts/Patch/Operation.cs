namespace GitHub.Services.WebApi.Patch
{
    // See RFC 6902 - JSON Patch for more details.
    // http://www.faqs.org/rfcs/rfc6902.html
    public enum Operation
    {
        Add,
        Remove,
        Replace,
        Move,
        Copy,
        Test
    }
}
