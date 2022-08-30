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

  const result = await glob.hashFiles(
    matchPatterns,
    {followSymbolicLinks},
    true
  )
  console.error(`__OUTPUT__${result}__OUTPUT__`)
}

run()
  .then(out => {
    console.log(out)
    process.exit(0)
  })
  .catch(err => {
    console.error(err)
    process.exit(1)
  })
