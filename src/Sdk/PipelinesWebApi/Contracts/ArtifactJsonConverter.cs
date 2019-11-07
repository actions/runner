using System;

namespace GitHub.Actions.Pipelines.WebApi.Contracts
{
    public class ArtifactJsonConverter : ArtifactBaseJsonConverter<Artifact>
    {
        protected override Artifact Create(Type objectType)
        {
            if (objectType == typeof(ActionsStorageArtifact))
            {
                return new ActionsStorageArtifact();
            }
            else
            {
                return null;
            }
        }

        protected override Artifact Create(ArtifactType type)
        {
            if (type == ArtifactType.Actions_Storage)
            {
                return new ActionsStorageArtifact();
            }

            return null;
        }
    }
}
