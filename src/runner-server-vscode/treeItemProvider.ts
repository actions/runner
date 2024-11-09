import { CancellationToken, commands, Event, EventEmitter, ExtensionContext, ProviderResult, TreeDataProvider, TreeItem, TreeItemCollapsibleState, Uri, window, workspace } from 'vscode'

interface IWorkflowRun {
    id: string,
    fileName: string,
    owner?: string,
    repo?: string,
    displayName: string,
    eventName: string,
    ref: string,
    sha: string,
    result: string
    status: string
}

function getAbsoluteIconPath(_context : ExtensionContext, relativeIconPath: string): {
    light: string | Uri;
    dark: string | Uri;
  } {
    return {
      light: Uri.joinPath(_context.extensionUri, "resources", "icons", "light", relativeIconPath),
      dark: Uri.joinPath(_context.extensionUri, "resources", "icons", "dark", relativeIconPath)
    };
  }

function getAbsoluteStatusIcon(_context : ExtensionContext, status: string) {
  switch(status?.toLowerCase()) {
    case "running":
    case "inprogress":
      return getAbsoluteIconPath(_context, "workflowruns/wr_inprogress.svg");
    case "waiting":
      return getAbsoluteIconPath(_context, "workflowruns/wr_waiting.svg");
    case "pending":
      return getAbsoluteIconPath(_context, "workflowruns/wr_pending.svg");
    case "succeeded":
      return getAbsoluteIconPath(_context, "workflowruns/wr_success.svg");
    case "failed":
      return getAbsoluteIconPath(_context, "workflowruns/wr_failure.svg");
    case "skipped":
      return getAbsoluteIconPath(_context, "workflowruns/wr_skipped.svg");
    case "canceled":
        return getAbsoluteIconPath(_context, "workflowruns/wr_cancelled.svg");
    default:
      return null;
  }
}
  
interface IJob {
    jobId: string,
    requestId: number,
    timeLineId: string,
    name: string,
    repo: string,
    workflowname: string,
    runid : number,
    errors: string[],
    result: string,
    attempt: number
  }

export class RSTreeDataProvider implements TreeDataProvider<TreeItem> {
    private changed: EventEmitter<void | TreeItem | TreeItem[]>;
    private ghHostApiUrl: string;
    private _context: ExtensionContext;
    
    constructor(_context : ExtensionContext, ghHostApiUrl : string) {
        this.ghHostApiUrl = ghHostApiUrl;
        this._context = _context;
        this.changed = new EventEmitter<void | TreeItem | TreeItem[]>();
        this.onDidChangeTreeData = this.changed.event;

        commands.registerCommand("runner.server.refreshWorkflows", () => {
            this.changed.fire();
        });

        (async() => {
            var ok = 0;
            while(ok < 10) {
                try {
                    var response = await fetch(`${this.ghHostApiUrl}/_apis/v1/Message/event2`, { keepalive: true });
                    const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
                    if(!response.ok) {
                        ok++;
                        await new Promise(suc => setTimeout(suc, 1000));
                    } else {
                        ok = 0;
                    }
                    while (true) {
                        var text = await reader.read();
                        console.log(text);
                        this.changed.fire();
                    }
                } catch(err) {
                    console.log(err);
                }
            }
        })();
    }

    onDidChangeTreeData?: Event<void | TreeItem | TreeItem[]>;
    getTreeItem(element: TreeItem): TreeItem | Thenable<TreeItem> {
        return element;
    }
    getChildren(element?: TreeItem): ProviderResult<TreeItem[]> {
        if(!element) {
            return (async () => {
                var result : IWorkflowRun[] = await (await fetch(`${this.ghHostApiUrl}/_apis/v1/Message/workflow/runs?page=${"0"}`)).json();
                return result.map(r => {
                    var item = new TreeItem(`${r.displayName ?? r.fileName} #${r.id}`, TreeItemCollapsibleState.Collapsed);
                    item.iconPath = getAbsoluteStatusIcon(this._context, r.result || r.status || "Pending");
                    item.contextValue = `/workflow/${r.result ? "completed" : "inprogress"}/`;
                    item.command = {
                        title: "Open Workflow",
                        command: "runner.server.openworkflowrun",
                        arguments: [
                            r.id
                        ]
                    };
                    return item;
                });
            })()
        }
        return (async () => {
            var result : IJob[] = await (await fetch(`${this.ghHostApiUrl}/_apis/v1/Message?page=${encodeURIComponent("0")}&runid=${encodeURIComponent(element.command.arguments[0])}`)).json();
            return result.map(r => {
                var item = new TreeItem(`${r.name ?? r.name} #${r.attempt}`, TreeItemCollapsibleState.None);
                item.iconPath = getAbsoluteStatusIcon(this._context, r.result ?? "inprogress");
                item.contextValue = `/job/${r.result ? "completed" : "inprogress"}/`;
                item.command = {
                    title: "Open Job",
                    command: "runner.server.openjob",
                    arguments: [
                        r.runid,
                        r.jobId,
                        `${r.name ?? r.name} #${r.attempt}`
                    ]
                };
                return item;
            });
        })();
    }
    getParent?(element: TreeItem): ProviderResult<TreeItem> {
        return null;
    }
    resolveTreeItem?(item: TreeItem, element: TreeItem, token: CancellationToken): ProviderResult<TreeItem> {
        return null;
    }

};