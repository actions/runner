import * as glob from '@actions/glob'

async function run(): Promise<void> {
  // arg0 -> node
  // arg1 -> hashFiles.js
  // env[followSymbolicLinks] = true/null
  // env[patterns] -> glob patterns
  let followSymbolicLinks = false
  const matchPatterns = process.env.patterns || ''
  if (process.env.followSymbolicLinks === 'true') {
    console.log('Follow symbolic links')
    followSymbolicLinks = true
  }
  console.log(`Match Pattern: ${matchPatterns}`)

  const result = glob.hashFiles(matchPatterns, {followSymbolicLinks})
  console.error(`__OUTPUT__${result}__OUTPUT__`)
}

run()
