import * as vscode from "vscode";
import {getLogInfo} from "./logInfo";

export class WorkflowStepLogFoldingProvider implements vscode.FoldingRangeProvider {
  provideFoldingRanges(document: vscode.TextDocument): vscode.ProviderResult<vscode.FoldingRange[]> {
    const logInfo = getLogInfo(document.uri);
    if (!logInfo) {
      return [];
    }

    return logInfo.sections.map(s => new vscode.FoldingRange(s.start, s.end, vscode.FoldingRangeKind.Region));
  }
}
