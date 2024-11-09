import * as vscode from "vscode";
import {cacheLogInfo} from "./logInfo";
import {parseLog} from "./model";
import {parseUri} from "./scheme";

interface ITimeLine {
  id?: string | undefined,
  parentId?: string | undefined,
  Type?: string | undefined,
  log?: ILog | undefined,
  order?: number | undefined,
  name?: string | undefined,
  busy?: boolean | undefined,
  failed?: boolean | undefined,
  state?: string | undefined,
  result?: string | undefined
  timelineId?: string
}

interface ILog {
  id: number,
  location: string
  content: string
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

export class WorkflowStepLogProvider implements vscode.TextDocumentContentProvider {
  onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
  onDidChange = this.onDidChangeEmitter.event;
  ghHostApiUrl: string;

  constructor(ghHostApiUrl : string) {
    this.ghHostApiUrl = ghHostApiUrl;
  }

  async provideTextDocumentContent(uri: vscode.Uri): Promise<string> {
    const {owner, repo, jobId} = parseUri(uri);

    try {
      var resp = await fetch(this.ghHostApiUrl + "/_apis/v1/Message?jobid=" + encodeURIComponent(jobId || ""), { })
      if(resp.status !== 200) {
        throw new Error("failed to get job")
      }
      var job : IJob | null = await resp.json();

      var resp = await fetch(this.ghHostApiUrl + "/_apis/v1/Timeline/" + job.timeLineId, { });
      if(resp.status !== 200) {
        throw new Error("failed to get timeline")
      }
      var newTimeline = await resp.json() as ITimeLine[];

      var response = await fetch(this.ghHostApiUrl + "/_apis/v1/Logfiles/" + newTimeline[0]?.log?.id, { });
      if(response.status !== 200 && response.status !== 204) {
          throw new Error(`Unexpected http error: ${response.status}`);
      }
      const log = await response.text();

      const logInfo = parseLog(log as string);
      cacheLogInfo(uri, logInfo);

      return logInfo.updatedLogLines.join("\n");
    } catch (e) {

      console.error("Error loading logs", e);
      return `Could not open logs, unhandled error. ${(e as Error).message}`;
    }
  }
}
