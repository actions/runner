namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Event for when a patch operation is about to be applied
    /// </summary>
    public interface IPatchOperationApplying
    {
        event PatchOperationApplyingEventHandler PatchOperationApplying;
    }

    /// <summary>
    /// Event handler for patch operation applying.
    /// </summary>
    public delegate void PatchOperationApplyingEventHandler(object sender, PatchOperationApplyingEventArgs e);
}
