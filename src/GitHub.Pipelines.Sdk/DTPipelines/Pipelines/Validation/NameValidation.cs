using System;
using System.ComponentModel;
using System.Text;

namespace GitHub.DistributedTask.Pipelines.Validation
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class NameValidation
    {
        public static Boolean IsValid(
            String name,
            Boolean allowHyphens = false)
        {
            var result = true;
            for (Int32 i = 0; i < name.Length; i++)
            {
                if ((name[i] >= 'a' && name[i] <= 'z') ||
                    (name[i] >= 'A' && name[i] <= 'Z') ||
                    (name[i] >= '0' && name[i] <= '9' && i > 0) ||
                    (name[i] == '_') ||
                    (allowHyphens && name[i] == '-' && i > 0))
                {
                    continue;
                }
                else
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public static String Sanitize(
            String name,
            Boolean allowHyphens = false)
        {
            if (name == null)
            {
                return String.Empty;
            }

            var sb = new StringBuilder();
            for (Int32 i = 0; i < name.Length; i++)
            {
                if ((name[i] >= 'a' && name[i] <= 'z') ||
                    (name[i] >= 'A' && name[i] <= 'Z') ||
                    (name[i] >= '0' && name[i] <= '9' && sb.Length > 0) ||
                    (name[i] == '_') ||
                    (allowHyphens && name[i] == '-' && sb.Length > 0))
                {
                    sb.Append(name[i]);
                }
            }
            return sb.ToString();
        }
    }
}
