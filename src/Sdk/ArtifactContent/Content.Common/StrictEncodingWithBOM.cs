using System.Text;

namespace GitHub.Services.Content.Common
{
    public static class StrictEncodingWithBOM
    {
        // UTF8 encoding that emit byte order marker when encoding a character stream into a
        // byte stream.
        //
        // When used for decoding with a StreamReader the actual encoding, if it can be detected,
        // has precedence over the encoding provided by the user. Hence, it is safe to use this
        // encoding even if the input stream has no BOM.
        public static readonly Encoding UTF8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: true,
            throwOnInvalidBytes: true);
    }
}
