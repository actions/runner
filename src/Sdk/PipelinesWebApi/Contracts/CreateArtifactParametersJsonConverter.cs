using System;

namespace GitHub.Actions.Pipelines.WebApi
{
    public class CreateArtifactParametersJsonConverter : ArtifactBaseJsonConverter<CreateArtifactParameters>
    {
        protected override CreateArtifactParameters Create(Type objectType)
        {
            if (objectType == typeof(CreateActionsStorageArtifactParameters))
            {
                return new CreateActionsStorageArtifactParameters();
            }
            else
            {
                return null;
            }
        }

        protected override CreateArtifactParameters Create(ArtifactType type)
        {
            if (type == ArtifactType.Actions_Storage)
            {
                return new CreateActionsStorageArtifactParameters();
            }

            return null;
        }
    }
}
