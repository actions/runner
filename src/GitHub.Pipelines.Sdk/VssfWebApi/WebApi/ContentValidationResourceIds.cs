using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.WebApi
{
    public static class ContentValidationResourceIds
    {
        public const string Area = "contentValidation";
        public const string ContentViolationArea = "contentViolation";

        public const string CvsCallbackResource = "cvsCallback";
        public static readonly Guid CvsCallbackLocationId = new Guid("68FB0862-7B4F-45AD-9BDD-9B689E233E4F");

        public const string TakedownResource = "takedown";
        public static readonly Guid TakedownLocationId = new Guid("7AE2F97A-5CCA-4A0A-AC90-81DD689F26F5");

        public const string AvertCallbackResource = "avertCallback";
        public static readonly Guid AvertCallbackLocationId = new Guid("E55E0DCC-84AE-43AC-BA89-F1C6D685A97A");

        public const string ViolationReportsResource = "reports";
        public static readonly Guid ViolationReportsLocationId = new Guid("3505911E-EAD6-431A-8656-B61C5D3B07A3");
    }
}
