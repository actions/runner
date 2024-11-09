import * as vscode from "vscode";
import {LogInfo} from "./model";
import {Parser, VSCodeDefaultColors} from "./parser";

const timestampRE = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{7}Z/;

const timestampDecorationType = vscode.window.createTextEditorDecorationType({
  color: "#99999959"
});

export function updateDecorations(activeEditor: vscode.TextEditor, logInfo: LogInfo) {
  if (!activeEditor) {
    return;
  }

  // Decorate timestamps
  const numberOfLines = activeEditor.document.lineCount;
  activeEditor.setDecorations(
    timestampDecorationType,
    Array.from(Array(numberOfLines).keys())
      .filter(i => {
        const line = activeEditor.document.lineAt(i).text;
        return timestampRE.test(line);
      })
      .map(i => ({
        range: new vscode.Range(i, 0, i, 28) // timestamps always have 28 chars
      }))
  );

  // Custom decorations
  const decoratorTypes: {
    [key: string]: {type: vscode.TextEditorDecorationType; ranges: vscode.Range[]};
  } = {};

  for (let lineNo = 0; lineNo < logInfo.updatedLogLines.length; lineNo++) {
    // .filter() preserves the order of the array
    const lineStyles = logInfo.styleFormats.filter(style => style.line == lineNo);
    let pos = 0;
    for (let styleNo = 0; styleNo < lineStyles.length; styleNo++) {
      const styleInfo = lineStyles[styleNo];
      const endPos = pos + styleInfo.content.length;
      const range = new vscode.Range(lineNo, pos, lineNo, endPos);
      pos = endPos;

      if (styleInfo.style) {
        const key = Parser.styleKey(styleInfo.style);
        let fgHex = "";
        let bgHex = "";

        // Convert to hex colors if RGB-formatted, or use lookup for predefined colors
        if (styleInfo.style.isFgRGB) {
          const rgbValues = styleInfo.style.fg.split(",");
          fgHex = rgbToHex(rgbValues);
        } else {
          fgHex = VSCodeDefaultColors[styleInfo.style.fg] ?? "";
        }
        if (styleInfo.style.isBgRGB) {
          const rgbValues = styleInfo.style.bg.split(",");
          bgHex = rgbToHex(rgbValues);
        } else {
          bgHex = VSCodeDefaultColors[styleInfo.style.bg] ?? "";
        }

        if (!decoratorTypes[key]) {
          decoratorTypes[key] = {
            type: vscode.window.createTextEditorDecorationType({
              color: fgHex,
              backgroundColor: bgHex,
              fontWeight: styleInfo.style.bold ? "bold" : "normal",
              fontStyle: styleInfo.style.italic ? "italic" : "normal",
              textDecoration: styleInfo.style.underline ? "underline" : ""
            }),
            ranges: [range]
          };
        } else {
          decoratorTypes[key].ranges.push(range);
        }
      }
    }
  }

  for (const decoratorType of Object.values(decoratorTypes)) {
    activeEditor.setDecorations(decoratorType.type, decoratorType.ranges);
  }
}

function rgbToHex(rgbValues: string[]) {
  let hex = "";
  if (rgbValues.length == 3) {
    hex = "#";
    for (let i = 0; i < 3; i++) {
      hex = hex.concat(parseInt(rgbValues[i]).toString(16).padStart(2, "0"));
    }
  }
  return hex;
}
