// eslint-disable-next-line no-control-regex
const ansiColorRegex = /\u001b\[(\d+;)*\d+m/gm;
const groupMarker = "##[group]";

import {Parser, IStyle} from "./parser";

export interface LogSection {
  start: number;
  end: number;
  name?: string;
}

export interface LogStyleInfo {
  line: number;
  content: string;
  style?: IStyle;
}

export interface LogInfo {
  updatedLogLines: string[];
  sections: LogSection[];
  styleFormats: LogStyleInfo[];
}

export function parseLog(log: string): LogInfo {
  let firstSection: LogSection | null = {
    name: "Setup",
    start: 0,
    end: 1
  };

  // Assume there is always the setup section
  const sections: LogSection[] = [firstSection];

  let currentRange: LogSection | null = null;

  const parser = new Parser();
  const styleInfo: LogStyleInfo[] = [];
  const lines = log.split(/\n|\r/).filter(l => !!l);

  let lineIdx = 0;

  for (const line of lines) {
    // Groups
    const groupMarkerStart = line.indexOf(groupMarker);
    if (groupMarkerStart !== -1) {
      // If this is the first group marker we encounter, the previous range was the job setup
      if (firstSection) {
        firstSection.end = lineIdx - 1;
        firstSection = null;
      }

      if (currentRange) {
        currentRange.end = lineIdx - 1;
        sections.push(currentRange);
      }

      const name = line.substring(groupMarkerStart + groupMarker.length);

      currentRange = {
        name,
        start: lineIdx,
        end: lineIdx + 1
      };
    }

    const stateFragments = parser.getStates(line);
    for (const state of stateFragments) {
      styleInfo.push({
        line: lineIdx,
        content: state.output,
        style: state.style
      });
    }

    // Remove all other commands and codes from the output, we don't support those
    lines[lineIdx] = line.replace(ansiColorRegex, "");

    ++lineIdx;
  }

  if (currentRange) {
    currentRange.end = lineIdx - 1;
    sections.push(currentRange);
  }

  return {
    updatedLogLines: lines,
    sections: sections,
    styleFormats: styleInfo
  };
}
