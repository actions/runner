import * as vscode from "vscode";
import {getLogInfo} from "./logInfo";

export class WorkflowStepLogSymbolProvider implements vscode.DocumentSymbolProvider {
  provideDocumentSymbols(
    document: vscode.TextDocument
  ): vscode.ProviderResult<vscode.SymbolInformation[] | vscode.DocumentSymbol[]> {
    const logInfo = getLogInfo(document.uri);
    if (!logInfo) {
      return [];
    }

    return logInfo.sections.map(
      s =>
        new vscode.DocumentSymbol(
          s.name || "Setup",
          "Step",
          vscode.SymbolKind.Function,
          new vscode.Range(s.start, 0, s.end, 0),
          new vscode.Range(s.start, 0, s.end, 0)
        )
    );
  }
}
