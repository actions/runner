using System;
using System.Text.RegularExpressions;

namespace GitHub.Services.Common.Utility
{
    public enum SearchTermType
    {
        Unknown,
        Scope,
        DomainAndAccountName,
        AccoutName,
        DisplayName,
        Vsid
    }

    public class IdentityDisplayName
    {
        private static readonly Regex s_scopeRegex = new Regex(@"^\[[0-9A-Za-z -]+\]\\(.+)<([0-9A-Fa-f]{8}(?:-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12})>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*Example 
        [DefaultCollection]\Project Collection Administrators <26A719D0-094A-44F3-A02F-A0D4A26E3CE3>
        [trial]\ProjectLevelGroup<c1530d49-55c5-4e1f-8625-81918553079a>
                    
            output - group 1 - Project Collection Administrators  group 2 - 26A719D0-094A-44F3-A02F-A0D4A26E3CE3
                    group 1 - ProjectLevelGroup  group 2 - c1530d49-55c5-4e1f-8625-81918553079a
                
        */

        private static readonly Regex s_vsidRegex = new Regex(@"^\[[0-9A-Za-z -]+\]\\(.+)<" + VstsGroupDisambiguatedPartPrefix + @"([0-9A-Fa-f]{8}(?:-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12})>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*Example 
        [DefaultCollection]\Project Collection Administrators <id:26A719D0-094A-44F3-A02F-A0D4A26E3CE3>
        [trial]\ProjectLevelGroup<id:c1530d49-55c5-4e1f-8625-81918553079a>
                    
            output - group 1 - Project Collection Administrators  group 2 - 26A719D0-094A-44F3-A02F-A0D4A26E3CE3
                    group 1 - ProjectLevelGroup  group 2 - c1530d49-55c5-4e1f-8625-81918553079a
                
        */

        private static readonly Regex s_domainAccountRegex = new Regex(@"^.+<(.+\\.+)>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*Example (On premise)
            TFS Dev <domain\accountName>  
                
            output - domain\accountName                               
        */

        private static readonly Regex s_accountNameRegex = new Regex(@"^.+<(.+@.+)>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*Example (Hosted)
            User name <Username@hotmail.com>     
            
            output - Username@hotmail.com           
        */

        private static readonly Regex s_displayNameRegex = new Regex(@"^[^<\\]*(?:<[^>]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*Example 
            User Name              
            
            output - User Name  
        */

        public const string VstsGroupDisambiguatedPartPrefix = "id:";

        public Guid ScopeId { get; internal set; }
        public Guid Vsid { get; internal set; }
        public string Domain { get; internal set; }
        public string AccountName { get; internal set; }
        public string DisplayName { get; internal set; }
        public SearchTermType Type { get; internal set; }

        private IdentityDisplayName()
        {
            Type = SearchTermType.Unknown;
        }

        public static IdentityDisplayName GetDisambiguatedSearchTerm(string search)
        {
            var searchTerm = new IdentityDisplayName();
            if (string.IsNullOrEmpty(search))
            {
                return searchTerm;
            }

            Guid scopeId;
            Guid vsid;
            string displayName;
            string result;

            if (TryGetVsid(search, out vsid, out displayName))
            {
                searchTerm.Type = SearchTermType.Vsid;
                searchTerm.DisplayName = displayName;
                searchTerm.Vsid = vsid;
            }
            else if (TryGetDomainAndAccountName(search, out result))
            {
                string[] domainAccount = result.Split('\\');
                if (domainAccount.Length == 2)
                {
                    searchTerm.Domain = domainAccount[0];
                    searchTerm.AccountName = domainAccount[1];
                    searchTerm.Type = SearchTermType.DomainAndAccountName;
                }
            }
            else if (TryGetAccountName(search, out result))
            {
                searchTerm.AccountName = result;
                searchTerm.Type = SearchTermType.AccoutName;
            }
            else if (TryGetDisplayName(search, out result))
            {
                searchTerm.DisplayName = result;
                searchTerm.Type = SearchTermType.DisplayName;
            }
            else if (TryGetScope(search, out scopeId, out displayName))
            {
                searchTerm.ScopeId = scopeId;
                searchTerm.DisplayName = displayName;
                searchTerm.Type = SearchTermType.Scope;
            }

            return searchTerm;
        }

        #region Private Methods

        private static bool TryGetScope(string search, out Guid scopeId, out string displayName)
        {
            var match = s_scopeRegex.Match(search);
            if (match.Success && match.Groups.Count > 1)
            {
                displayName = match.Groups[1].Value;
                Guid.TryParse(match.Groups[2].Value, out scopeId);
                return true;
            }

            scopeId = Guid.Empty;
            displayName = string.Empty;
            return false;
        }

        private static bool TryGetVsid(string search, out Guid vsid, out string displayName)
        {
            var match = s_vsidRegex.Match(search);
            if (match.Success && match.Groups.Count > 1)
            {
                displayName = match.Groups[1].Value;
                Guid.TryParse(match.Groups[2].Value, out vsid);
                return true;
            }

            vsid = Guid.Empty;
            displayName = string.Empty;
            return false;
        }

        private static bool TryGetDomainAndAccountName(string search, out string domainAndAcccountName)
        {
            var match = s_domainAccountRegex.Match(search);
            domainAndAcccountName = null;
            if (match.Success && match.Groups.Count > 1)
            {
                if (match.Groups[1].Value.Contains(@"\"))
                {
                    domainAndAcccountName = match.Groups[1].Value;
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetAccountName(string search, out string acccountName)
        {
            var match = s_accountNameRegex.Match(search);
            acccountName = null;
            if (match.Success && match.Groups.Count > 1)
            {
                acccountName = match.Groups[1].Value;
                return true;
            }
            return false;
        }

        private static bool TryGetDisplayName(string search, out string displayName)
        {
            var match = s_displayNameRegex.Match(search);
            displayName = null;
            if (match.Success && match.Groups.Count > 0)
            {
                displayName = match.Groups[0].Value;
                return true;
            }
            return false;
        }

        #endregion
    }
}
