import * as vscode from "vscode";
import {LogInfo} from "./model";

const cache = new Map<string, LogInfo>();

export function cacheLogInfo(uri: vscode.Uri, logInfo: LogInfo) {
  cache.set(uri.toString(), logInfo);
}

export function getLogInfo(uri: vscode.Uri): LogInfo | undefined {
  return cache.get(uri.toString());
}
