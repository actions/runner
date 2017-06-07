// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommitsDownloadException.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the CommitsDownloadException type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

public class CommitsDownloadException : Exception
{
    public CommitsDownloadException()
    {
    }

    public CommitsDownloadException(string message) : base(message)
    {
    }

    public CommitsDownloadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
