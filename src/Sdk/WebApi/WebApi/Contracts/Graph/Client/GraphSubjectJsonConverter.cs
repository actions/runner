using System;
using System.Linq;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.Graph.Client
{
    public class GraphSubjectJsonConverter : VssJsonCreationConverter<GraphSubject>
    {
        protected override GraphSubject Create(Type objectType, JObject jsonObject)
        {
            var subjectKindObject = jsonObject.GetValue(nameof(GraphSubject.SubjectKind), StringComparison.OrdinalIgnoreCase);
            if (subjectKindObject == null)
            {
                throw new ArgumentException(WebApiResources.UnknownEntityType(subjectKindObject));
            }
            var typeName = subjectKindObject.ToString();
            switch (typeName)
            {
                case Constants.SubjectKind.Group:
                    var groupInfo = typeof(GraphGroup).GetTypeInfo();
                    var graphGroupConstructor = groupInfo.DeclaredConstructors.First(x => x.GetParameters().Length == 0);
                    return (GraphGroup)graphGroupConstructor.Invoke(null);
                case Constants.SubjectKind.Scope:
                    var scopeInfo = typeof(GraphScope).GetTypeInfo();
                    var graphScopeConstructor = scopeInfo.DeclaredConstructors.First(x => x.GetParameters().Length == 0);
                    return (GraphScope)graphScopeConstructor.Invoke(null);
                case Constants.SubjectKind.User:
                    var userInfo = typeof(GraphUser).GetTypeInfo();
                    var graphUserConstructor = userInfo.DeclaredConstructors.First(x => x.GetParameters().Length == 0);
                    return (GraphUser)graphUserConstructor.Invoke(null);
                case Constants.SubjectKind.SystemSubject:
                    var systemSubjectInfo = typeof(GraphSystemSubject).GetTypeInfo();
                    var graphSystemSubjectConstructor = systemSubjectInfo.DeclaredConstructors.First(x => x.GetParameters().Length == 0);
                    return (GraphSystemSubject)graphSystemSubjectConstructor.Invoke(null);
                default:
                    throw new ArgumentException(WebApiResources.UnknownEntityType(typeName));
            }
        }
    }
}
