namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Event for when a patch operation has been applied.
    /// </summary>
    public interface IPatchOperationApplied
    {
        event PatchOperationAppliedEventHandler PatchOperationApplied;
    }

    /// <summary>
    /// Event handler for patch operation applied.
    /// </summary>
    public delegate void PatchOperationAppliedEventHandler(object sender, PatchOperationAppliedEventArgs e);
}
