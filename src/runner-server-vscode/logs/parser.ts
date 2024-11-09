// #region ANSI section

const ESC = "\u001b";
const BrightClassPostfix = "-br";

// match characters that could be enclosing url to cleanly handle url formatting
export const URLRegex = /([{([]*https?:\/\/[a-z0-9]+(?:-[a-z0-9]+)*\.[^\s<>|'",]{2,})/gi;

/**
 * Regex for matching ANSI escape codes
 * \u001b - ESC character
 * ?: Non-capturing group
 * (?:\u001b[) : Match ESC[
 * (?:[\?|#])??: Match also ? and # formats that we don't supports but want to eat our special characters to get rid of ESC character
 * (?:[0-9]{1,3})?: Match one or more occurrences of the simple format we want with out semicolon
 * (?:(?:;[0-9]{0,3})*)?: Match one or more occurrences of the format we want with semicolon
 */

// eslint-disable-next-line no-control-regex
const _ansiEscapeCodeRegex = /(?:\u001b\[)(?:[?|#])?(?:(?:[0-9]{1,3})?(?:(?:;[0-9]{0,3})*)?[A-Z|a-z])/;

/**
 * https://en.wikipedia.org/wiki/ANSI_escape_code#SGR_(Select_Graphic_Rendition)_parameters
 * We support sequences of format:
 *  Esc[CONTENTHEREm
 *  Where CONTENTHERE can be of format: VALUE;VALUE;VALUE or VALUE
 *      Where VALUE is SGR parameter https://www.vt100.net/docs/vt510-rm/SGR
 *          We support: 0 (reset), 1 (bold), 3 (italic), 4 (underline), 22 (not bold), 23 (not italic), 24 (not underline), 38 (set fg), 39 (default fg), 48 (set bg), 49 (default bg),
 *                      fg colors - 30 (black), 31 (red), 32 (green), 33 (yellow), 34 (blue), 35 (magenta), 36 (cyan), 37 (white), 90 (grey)
 *                        with more brightness - 91 (red), 92 (green), 93 (yellow), 94 (blue), 95 (magenta), 96 (cyan), 97 (white)
 *                      bg colors - 40 (black), 41 (red), 42 (green), 43 (yellow), 44 (blue), 45 (magenta), 46 (cyan), 47 (white), 100 (grey)
 *                        with more brightness- 101 (red), 102 (green), 103 (yellow), 104 (blue), 105 (magenta), 106 (cyan), 107 (white)
 *  Where m refers to the "Graphics mode"
 *
 * 8-bit color is supported
 *  https://en.wikipedia.org/wiki/ANSI_escape_code#8-bit
 *  Esc[38;5;<n> To set the foreground color
 *  Esc[48;5;<n> To set the background color
 *  n can be from 0-255
 *  0-7 are standard colors that match the 4_bit color palette
 *  8-15 are high intensity colors that match the 4_bit high intensity color palette
 *  16-231 are 216 colors that cover the entire spectrum
 *  232-255 are grayscale colors that go from black to white in 24 steps
 *
 * 24-bit color is also supported
 *  https://en.wikipedia.org/wiki/ANSI_escape_code#24-bit
 *  Esc[38;2;<r>;<g>;<b> To set the foreground color
 *  Esc[48;2;<r>;<g>;<b> To set the background color
 *  Where r,g and b must be between 0-255
 */
// #endregion ANSI section

// #region commands
enum Resets {
  Reset = "0",
  Bold = "22",
  Italic = "23",
  Underline = "24",
  Set_Fg = "38",
  Default_Fg = "39",
  Set_Bg = "48",
  Default_Bg = "49"
}

const specials = {
  "1": "bold",
  "3": "italic",
  "4": "underline"
} as {[key: string]: string};

const bgColors = {
  // 40 (black), 41 (red), 42 (green), 43 (yellow), 44 (blue), 45 (magenta), 46 (cyan), 47 (white), 100 (grey)
  "40": "b",
  "41": "r",
  "42": "g",
  "43": "y",
  "44": "bl",
  "45": "m",
  "46": "c",
  "47": "w",
  "100": "gr"
} as {[key: string]: string};

const fgColors = {
  // 30 (black), 31 (red), 32 (green), 33 (yellow), 34 (blue), 35 (magenta), 36 (cyan), 37 (white), 90 (grey)
  "30": "b",
  "31": "r",
  "32": "g",
  "33": "y",
  "34": "bl",
  "35": "m",
  "36": "c",
  "37": "w",
  "90": "gr"
} as {[key: string]: string};

const base8BitColors = {
  // 0 (black), 1 (red), 2 (green), 3 (yellow), 4 (blue), 5 (magenta), 6 (cyan), 7 (white), 8 (grey)
  "0": "b",
  "1": "r",
  "2": "g",
  "3": "y",
  "4": "bl",
  "5": "m",
  "6": "c",
  "7": "w"
} as Record<string, string>;

// VS Code default values taken from this table: https://en.wikipedia.org/wiki/ANSI_escape_code#3-bit_and_4-bit
export const VSCodeDefaultColors = {
  b: "#000000", // '30/40',
  r: "#cd3131", // '31/41',
  g: "#0dbc79", // '32/42',
  y: "#e5e510", // '33/43',
  bl: "#2472c8", // '34/44',
  m: "#bc3fbc", // '35/45',
  c: "#11a8cd", // '36/46',
  w: "#e5e5e5", // '37/47',
  gr: "#666666", // '90/100'
  "b-br": "#666666", // '90/100',
  "r-br": "#f14c4c", // '91/101',
  "g-br": "#23d18b", // '92/102',
  "y-br": "#f5f543", // '93/103',
  "bl-br": "#3b8eea", // '94/104',
  "m-br": "#d670d6", // '95/105',
  "c-br": "#21b8db", // '96/106',
  "w-br": "#e5e5e5" // '97/107',
} as Record<string, string>;

//0-255 in 6 increments, used to generate 216 equally incrementing colors
const colorIncrements216 = {
  0: 0,
  1: 51,
  2: 102,
  3: 153,
  4: 204,
  5: 255
} as Record<number, number>;

// #endregion commands

export interface IStyle {
  fg: string;
  bg: string;
  isFgRGB: boolean;
  isBgRGB: boolean;
  bold: boolean;
  italic: boolean;
  underline: boolean;
  [key: string]: boolean | string;
}

interface IRGBColor {
  r: number;
  g: number;
  b: number;
}

interface IAnsiEscapeCodeState {
  output: string;
  style?: IStyle;
}

export class Parser {
  /**
   * Parses the content into ANSI states
   * @param content content to parse
   */
  public getStates(content: string): IAnsiEscapeCodeState[] {
    const result: IAnsiEscapeCodeState[] = [];
    // Eg: "ESC[0KESC[33;1mWorker informationESC[0m
    if (!_ansiEscapeCodeRegex.test(content)) {
      // Not of interest, don't touch content
      return [
        {
          output: content
        }
      ];
    }

    let command = "";
    let currentText = "";
    let code = "";
    let state = {} as IAnsiEscapeCodeState;
    let isCommandActive = false;
    let codes = [];
    for (let index = 0; index < content.length; index++) {
      const character = content[index];
      if (isCommandActive) {
        if (character === ";") {
          if (code) {
            codes.push(code);
            code = "";
          }
        } else if (character === "m") {
          if (code) {
            isCommandActive = false;
            // done
            codes.push(code);
            state.style = state.style || ({} as IStyle);

            let setForeground = false;
            let setBackground = false;
            let isSingleColorCode = false;
            let isRGBColorCode = false;
            const rgbColors: number[] = [];

            for (const currentCode of codes) {
              const style = state.style;
              const codeNumber = parseInt(currentCode);
              if (setForeground && isSingleColorCode) {
                // set foreground color using 8-bit (256 color) palette - Esc[ 38:5:<n> m
                if (codeNumber >= 0 && codeNumber < 16) {
                  style.fg = this._get8BitColorClasses(codeNumber);
                } else if (codeNumber >= 16 && codeNumber < 256) {
                  style.fg = this._get8BitRGBColors(codeNumber);
                  style.isFgRGB = true;
                }
                setForeground = false;
                isSingleColorCode = false;
              } else if (setForeground && isRGBColorCode) {
                // set foreground color using 24-bit (true color) palette - Esc[ 38:2:<r>:<g>:<b> m
                if (codeNumber >= 0 && codeNumber < 256) {
                  rgbColors.push(codeNumber);
                  if (rgbColors.length === 3) {
                    style.fg = `${rgbColors[0]},${rgbColors[1]},${rgbColors[2]}`;
                    style.isFgRGB = true;
                    rgbColors.length = 0; // clear array
                    setForeground = false;
                    isRGBColorCode = false;
                  }
                }
              } else if (setBackground && isSingleColorCode) {
                // set background color using 8-bit (256 color) palette - Esc[ 48:5:<n> m
                if (codeNumber >= 0 && codeNumber < 16) {
                  style.bg = this._get8BitColorClasses(codeNumber);
                } else if (codeNumber >= 16 && codeNumber < 256) {
                  style.bg = this._get8BitRGBColors(codeNumber);
                  style.isBgRGB = true;
                }
                setBackground = false;
                isSingleColorCode = false;
              } else if (setBackground && isRGBColorCode) {
                // set background color using 24-bit (true color) palette - Esc[ 48:2:<r>:<g>:<b> m
                if (codeNumber >= 0 && codeNumber < 256) {
                  rgbColors.push(codeNumber);
                  if (rgbColors.length === 3) {
                    style.bg = `${rgbColors[0]},${rgbColors[1]},${rgbColors[2]}`;
                    style.isBgRGB = true;
                    rgbColors.length = 0; // clear array
                    setBackground = false;
                    isRGBColorCode = false;
                  }
                }
              } else if (setForeground || setBackground) {
                if (codeNumber === 5) {
                  isSingleColorCode = true;
                } else if (codeNumber === 2) {
                  isRGBColorCode = true;
                }
              } else if (fgColors[currentCode]) {
                style.fg = fgColors[currentCode];
              } else if (bgColors[currentCode]) {
                style.bg = bgColors[currentCode];
              } else if (currentCode === Resets.Reset) {
                // reset
                state.style = {} as IStyle;
              } else if (currentCode === Resets.Set_Bg) {
                setBackground = true;
              } else if (currentCode === Resets.Set_Fg) {
                setForeground = true;
              } else if (currentCode === Resets.Default_Fg) {
                style.fg = "";
              } else if (currentCode === Resets.Default_Bg) {
                style.bg = "";
              } else if (codeNumber >= 91 && codeNumber <= 97) {
                style.fg = fgColors[codeNumber - 60] + BrightClassPostfix;
              } else if (codeNumber >= 101 && codeNumber <= 107) {
                style.bg = bgColors[codeNumber - 60] + BrightClassPostfix;
              } else if (specials[currentCode]) {
                style[specials[currentCode]] = true;
              } else if (currentCode === Resets.Bold) {
                style.bold = false;
              } else if (currentCode === Resets.Italic) {
                style.italic = false;
              } else if (currentCode === Resets.Underline) {
                style.underline = false;
              }
            }

            // clear
            command = "";
            currentText = "";
            code = "";
          } else {
            // To handle ESC[m, we should just ignore them
            isCommandActive = false;
            command = "";
            state.style = {} as IStyle;
          }

          codes = [];
        } else if (isNaN(parseInt(character))) {
          // if this is not a number, eg: 0K, this isn't something we support
          code = "";
          isCommandActive = false;
          command = "";
        } else if (code.length === 4) {
          // we probably got code that we don't support, ignore
          code = "";
          isCommandActive = false;
          if (character !== ESC) {
            // if this is not an ESC, let's not consider command from now on
            // eg: ESC[0Ksometexthere, at this point, code would be 0K, character would be 's'
            command = "";
            currentText += character;
          }
        } else {
          code += character;
        }

        continue;
      } else if (command) {
        if (command === ESC && character === "[") {
          isCommandActive = true;
          // push state
          if (currentText) {
            state.output = currentText;
            result.push(state);
            // deep copy existing style for the line to preserve different styles between commands
            let previousStyle;
            if (state.style) {
              previousStyle = Object.assign({}, state.style);
            }
            state = {} as IAnsiEscapeCodeState;
            if (previousStyle) {
              state.style = previousStyle;
            }
            currentText = "";
          }
        }

        continue;
      }

      if (character === ESC) {
        command = character;
      } else {
        currentText += character;
      }
    }

    // still pending text
    if (currentText) {
      state.output = currentText + (command ? command : "");
      result.push(state);
    }

    return result;
  }

  /**
   * Gets a unique key for each style
   * @param style style to get key for
   * @returns a string that is guaranteed to be unique for every different style
   */
  public static styleKey(style: IStyle): string {
    const fg = style.fg ?? "-";
    const bg = style.bg ?? "-";
    const bold = style.bold ? "b" : "n";
    const ital = style.italic ? "i" : "n";
    const underline = style.underline ? "u" : "n";
    return fg + bg + bold + ital + underline;
  }

  /**
   * With 8 bit colors, from 16-256, rgb color combinations are used
   * 16-231 (216 colors) is a 6 x 6 x 6 color cube
   * 232 - 256 are grayscale colors
   * @param codeNumber 16-256 number
   */
  private _get8BitRGBColors(codeNumber: number): string {
    let rgbColor: IRGBColor;
    if (codeNumber < 232) {
      rgbColor = this._get216Color(codeNumber - 16);
    } else {
      rgbColor = this._get8bitGrayscale(codeNumber - 232);
    }
    return `${rgbColor.r},${rgbColor.g},${rgbColor.b}`;
  }

  /**
   * With 8 bit color, from 0-15, css classes are used to represent customer colors
   * @param codeNumber 0-15 number that indicates the standard or high intensity color code that should be used
   */
  private _get8BitColorClasses(codeNumber: number): string {
    let colorClass = "";
    if (codeNumber < 8) {
      colorClass = `${base8BitColors[codeNumber]}`;
    } else {
      colorClass = `${base8BitColors[codeNumber - 8] + BrightClassPostfix}`;
    }
    return colorClass;
  }

  /**
   * 6 x 6 x 6 (216 colors) rgb color generator
   * https://en.wikipedia.org/wiki/Web_colors#Web-safe_colors
   * @param increment 0-215 value
   */
  private _get216Color(increment: number): IRGBColor {
    return {
      r: colorIncrements216[Math.floor(increment / 36)],
      g: colorIncrements216[Math.floor(increment / 6) % 6],
      b: colorIncrements216[increment % 6]
    };
  }

  /**
   * Grayscale from black to white in 24 steps. The first value of 0 represents rgb(8,8,8) while the last value represents rgb(238,238,238)
   * @param increment 0-23 value
   */
  private _get8bitGrayscale(increment: number): IRGBColor {
    const colorCode = increment * 10 + 8;
    return {
      r: colorCode,
      g: colorCode,
      b: colorCode
    };
  }
}
