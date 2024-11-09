import * as vscode from "vscode";
import {LogScheme} from "./constants";

/**
 * @param displayName Must not contain '/'
 */
export function buildLogURI(displayName: string, owner: string, repo: string, jobId: string): vscode.Uri {
  return vscode.Uri.parse(`${LogScheme}://${owner}/${jobId}/${displayName}`);
}

export function parseUri(uri: vscode.Uri): {
  owner: string;
  repo: string;
  jobId: string;
} {
  if (uri.scheme != LogScheme) {
    throw new Error("Uri is not of log scheme");
  }

  return {
    owner: uri.authority,
    repo: uri.path.split("/").slice(0, 1).join(""),
    jobId: uri.path.split("/").slice(1, 2).join("")
  };
}
