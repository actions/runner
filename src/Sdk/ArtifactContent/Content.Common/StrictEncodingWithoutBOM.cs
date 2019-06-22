using System.Text;

namespace GitHub.Services.Content.Common
{
    public static class StrictEncodingWithoutBOM
    {
        // http://stackoverflow.com/questions/2437666/write-text-files-without-byte-order-mark-bom
        // When used with a stream reader this fails to read text that has a BOM.
        public static readonly Encoding UTF8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);
    }
}
