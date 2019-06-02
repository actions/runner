using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Common
{
    public static class CultureResolution
    {
        /// <summary>
        /// Given a list of acceptable cultures, ordered from most preferred to least, return the item from availableCultures which 
        /// best fits the culture(s) requested. If there is no suitable match, return null.
        /// </summary>
        /// <param name="orderedAcceptableCultures">Ordered list of acceptable cultures, from most preferred to least preferred.</param>
        /// <param name="availableCultures">Available cultures to choose from (e.g. locales available for a given resource)</param>
        /// <returns>A single element from availableCultures which is the best matching culture given the list of acceptable
        /// cultures, or null if there is no reasonable match.</returns>
        public static CultureInfo GetBestCultureMatch(IList<CultureInfo> orderedAcceptableCultures, ISet<CultureInfo> availableCultures)
        {
            Int32 shortestDistance = Int32.MaxValue;
            CultureInfo bestMatch = null;
            foreach (CultureInfo requested in orderedAcceptableCultures)
            {
                foreach (CultureInfo available in availableCultures)
                {
                    Int32 distance = GetCultureRelationshipDistance(requested, available);
                    if (distance == 0)
                    {
                        return available;
                    }
                    if (distance > 0 && distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        bestMatch = available;
                    }
                }
                if (bestMatch != null)
                {
                    return bestMatch;
                }
            }
            return null;
        }

        /// <summary>
        /// Get an integer indicating how related the given cultures are (this method is symmetric on its inputs).
        /// This implementation returns the integer difference of the distance from the Invariant Culture, assuming one culture
        /// is an ancestor of another. Otherwise -1 is returned.
        /// </summary>
        /// <param name="cultureA"></param>
        /// <param name="cultureB"></param>
        /// <returns>0 if the cultures are the same, higher integers mean they are less related. -1 means no relation.</returns>
        public static Int32 GetCultureRelationshipDistance(CultureInfo cultureA, CultureInfo cultureB)
        {
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            if (cultureA == null || cultureB == null || cultureA.Equals(invariantCulture) || cultureB.Equals(invariantCulture))
            {
                return -1;
            }
            Int32 aToB = DistanceToParentCulture(cultureA, cultureB);
            if (aToB >= 0)
            {
                return aToB;
            }
            return DistanceToParentCulture(cultureB, cultureA);
            
            Int32 DistanceToParentCulture(CultureInfo childCulture, CultureInfo parentCulture)
            {
                Int32 distance = 0;
                CultureInfo nextParent = childCulture;
                while (nextParent != null && nextParent != CultureInfo.InvariantCulture && nextParent.Parent != nextParent)
                {
                    if (nextParent.Equals(parentCulture))
                    {
                        return distance;
                    }
                    nextParent = nextParent.Parent;
                    distance++;
                }
                return -1;
            }
        }
    }
}
