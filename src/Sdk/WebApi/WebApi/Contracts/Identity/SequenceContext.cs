using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace GitHub.Services.Identity
{
    public class SequenceContext
    {
        public SequenceContext(long identitySequenceId, long groupSequenceId) :
            this(identitySequenceId, groupSequenceId, SequenceContext.UnspecifiedSequenceId)
        {
        }

        public SequenceContext(long identitySequenceId, long groupSequenceId, long organizationIdentitySequenceId) :
            this(identitySequenceId, groupSequenceId, organizationIdentitySequenceId, 0)
        {
        }

        public SequenceContext(long identitySequenceId, long groupSequenceId, long organizationIdentitySequenceId, long pageSize)
        {
            IdentitySequenceId = identitySequenceId;
            GroupSequenceId = groupSequenceId;
            OrganizationIdentitySequenceId = organizationIdentitySequenceId;
            PageSize = pageSize;
        }

        internal long IdentitySequenceId { get; }

        internal long GroupSequenceId { get; }

        internal long OrganizationIdentitySequenceId { get; }

        internal long PageSize { get; }

        internal SequenceContext Clone()
        {
            return new SequenceContext(IdentitySequenceId, GroupSequenceId, OrganizationIdentitySequenceId);
        }

        public override string ToString() => $"[{nameof(IdentitySequenceId)}:{IdentitySequenceId}, {nameof(GroupSequenceId)}:{GroupSequenceId}, {nameof(OrganizationIdentitySequenceId)}:{OrganizationIdentitySequenceId}]";

        internal const long UnspecifiedSequenceId = -1;
        
        internal static SequenceContext MaxSequenceContext = new SequenceContext(long.MaxValue, long.MaxValue, long.MaxValue, 0);
        internal static SequenceContext InitSequenceContext = new SequenceContext(UnspecifiedSequenceId, UnspecifiedSequenceId, UnspecifiedSequenceId, 0);

        internal class HeadersUtils
        {
            internal const string MinIdentitySequenceId = "X-VSSF-MinIdentitySequenceId";
            internal const string MinGroupSequenceId = "X-VSSF-MinGroupSequenceId";
            internal const string MinOrgIdentitySequenceId = "X-VSSF-MinOrgIdentitySequenceId";
            internal const string PageSize = "X-VSSF-PagingSize";

            internal static bool TryExtractSequenceContext(HttpRequestHeaders httpRequestHeaders, out SequenceContext sequenceContext)
            {
                sequenceContext = null;
                bool hasMinIdentitySequenceHeader = httpRequestHeaders.TryGetValues(MinIdentitySequenceId, out var minIdentitySequenceIdValues) && minIdentitySequenceIdValues != null;
                bool hasMinGroupSequenceHeader = httpRequestHeaders.TryGetValues(MinGroupSequenceId, out var minGroupSequenceIdValues) && minGroupSequenceIdValues != null;
                bool hasMinOrgIdentitySequenceHeader = httpRequestHeaders.TryGetValues(MinOrgIdentitySequenceId, out var minOrgIdentitySequenceIdValues) && minOrgIdentitySequenceIdValues != null;
                bool hasPageSizeHeader = httpRequestHeaders.TryGetValues(PageSize, out var pageSizeValues) && pageSizeValues != null;

                if (!hasMinGroupSequenceHeader && !hasMinIdentitySequenceHeader && !hasMinOrgIdentitySequenceHeader)
                {
                    return false;
                }

                long minIdentitySequenceId = ParseOrGetDefault(minIdentitySequenceIdValues?.FirstOrDefault());
                long minGroupSequenceId = ParseOrGetDefault(minGroupSequenceIdValues?.FirstOrDefault());
                long minOrgIdentitySequenceId = ParseOrGetDefault(minOrgIdentitySequenceIdValues?.FirstOrDefault());
                long pageSize = ParseOrGetDefault(pageSizeValues?.FirstOrDefault());
                sequenceContext = new SequenceContext(minIdentitySequenceId, minGroupSequenceId, minOrgIdentitySequenceId, pageSize);
                return true;
            }

            internal static KeyValuePair<string, string>[] PopulateRequestHeaders(SequenceContext sequenceContext)
            {
                if (sequenceContext == null)
                {
                    return new KeyValuePair<string, string>[0];
                }

                return new[]
                {
                    new KeyValuePair<string, string>(MinIdentitySequenceId, sequenceContext.IdentitySequenceId.ToString()),
                    new KeyValuePair<string, string>(MinGroupSequenceId, sequenceContext.GroupSequenceId.ToString()),
                    new KeyValuePair<string, string>(MinOrgIdentitySequenceId, sequenceContext.OrganizationIdentitySequenceId.ToString()),
                    new KeyValuePair<string, string>(PageSize, sequenceContext.PageSize.ToString())
                };
            }

            private static long ParseOrGetDefault(string s)
            {
                if (!string.IsNullOrWhiteSpace(s) && long.TryParse(s, out long value))
                {
                    return value;
                }
                return UnspecifiedSequenceId;
            }
        }
    }
}
