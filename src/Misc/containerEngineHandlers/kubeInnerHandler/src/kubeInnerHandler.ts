import * as exec from '@actions/exec'
import * as core from '@actions/core'
import * as events from 'events'
import * as readline from 'readline'

async function run(): Promise<void> {
  let input = ''

  const rl = readline.createInterface({
    input: process.stdin
  })

  rl.on('line', line => {
    core.debug(`Line from STDIN: ${line}`)
    input = line
  })

  await events.once(rl, 'close')

  core.debug(input)

  const execInput = JSON.parse(input)
  core.debug(JSON.stringify(execInput))

  // podman exec -i --workdir /__w/canary/canary
  // -e GITHUB_JOB -e GITHUB_REF -e GITHUB_SHA -e GITHUB_REPOSITORY
  // -e GITHUB_REPOSITORY_OWNER -e GITHUB_RUN_ID -e GITHUB_RUN_NUMBER
  // -e GITHUB_RETENTION_DAYS -e GITHUB_RUN_ATTEMPT -e GITHUB_ACTOR
  // -e GITHUB_WORKFLOW -e GITHUB_HEAD_REF -e GITHUB_BASE_REF -e GITHUB_EVENT_NAME
  // -e GITHUB_SERVER_URL -e GITHUB_API_URL -e GITHUB_GRAPHQL_URL
  // -e GITHUB_WORKSPACE -e GITHUB_ACTION -e GITHUB_EVENT_PATH -e GITHUB_ACTION_REPOSITORY
  // -e GITHUB_ACTION_REF -e GITHUB_PATH -e GITHUB_ENV -e RUNNER_DEBUG
  // -e RUNNER_OS -e RUNNER_NAME -e RUNNER_TOOL_CACHE
  // -e RUNNER_TEMP -e RUNNER_WORKSPACE
  //   eccdf520697a035599d6e8c8dc801f004fdd3797cdce88f590aba3669a88d9bc sh -e /__w/_temp/d3b30383-719c-4e76-a16f-8f85443352be.sh

  const execArgs = []
  const args = (<string>execInput.arguments).split(' ')
  core.debug(JSON.stringify(args))
  execArgs.push(...args)

  core.debug(JSON.stringify(execArgs))

  await exec.exec(execInput.fileName, execArgs, {
    env: execInput.environmentVariables
  })
}

run()
