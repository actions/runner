using System;

using MediatR;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace Runner.Language.Server;

public class WorkspaceFolderListener : IDidChangeWorkspaceFoldersHandler
{
    public DidChangeWorkspaceFolderRegistrationOptions GetRegistrationOptions(ClientCapabilities clientCapabilities)
    {
        return new DidChangeWorkspaceFolderRegistrationOptions() { ChangeNotifications = true };
    }

    public Task<Unit> Handle(DidChangeWorkspaceFoldersParams request, CancellationToken cancellationToken)
    {
        //request.Event.Added;
        return Unit.Task;
    }
}
