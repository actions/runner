using System;
using System.ComponentModel;
using System.Text;

namespace GitHub.Build.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Security
    {
        /// <summary>
        /// Gets tokenized path from the given path to fit in to build hierarchical security
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static String GetSecurityTokenPath(String path)
        {
            if (String.IsNullOrEmpty(path))
            {
                // return root path by default
                return NamespaceSeparator.ToString();
            }

            String[] components = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (components.Length == 0)
            {
                // for root path
                return NamespaceSeparator.ToString();
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < components.Length; i++)
            {
#if !NETSTANDARD
                // FileSpec isn't available in NetStandard
                String error;
                if (!FileSpec.IsLegalNtfsName(components[i], MaxPathNameLength, true, out error))
                {
                    throw new InvalidPathException(error);
                }
#endif

                sb.AppendFormat("{0}{1}", NamespaceSeparator, components[i]);
            }

            sb.Append(NamespaceSeparator);
            return sb.ToString();
        }

        public static readonly Char NamespaceSeparator = '/';
        public static readonly Int32 MaxPathNameLength = 248;

        public const String BuildNamespaceIdString = "33344D9C-FC72-4d6f-ABA5-FA317101A7E9";
        public static readonly Guid BuildNamespaceId = new Guid(BuildNamespaceIdString);
    }
}
