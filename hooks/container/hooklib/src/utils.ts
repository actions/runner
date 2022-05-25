import * as core from '@actions/core'
import * as events from 'events'
import * as fs from 'fs'
import * as os from 'os'
import * as readline from 'readline'
import {HookData} from './interfaces'

export async function getInputFromStdin(): Promise<HookData> {
  let input = ''

  const rl = readline.createInterface({
    input: process.stdin
  })

  rl.on('line', line => {
    core.debug(`Line from STDIN: ${line}`)
    input = line
  })
  await events.default.once(rl, 'close')
  const inputJson = JSON.parse(input)
  return inputJson as HookData
}

export function writeToResponseFile(filePath: string, message: any): void {
  if (!filePath) {
    throw new Error(`Expected file path`)
  }
  if (!fs.existsSync(filePath)) {
    throw new Error(`Missing file at path: ${filePath}`)
  }

  fs.appendFileSync(filePath, `${toCommandValue(message)}${os.EOL}`, {
    encoding: 'utf8'
  })
}

function toCommandValue(input: any): string {
  if (input === null || input === undefined) {
    return ''
  } else if (typeof input === 'string' || input instanceof String) {
    return input as string
  }
  return JSON.stringify(input)
}
