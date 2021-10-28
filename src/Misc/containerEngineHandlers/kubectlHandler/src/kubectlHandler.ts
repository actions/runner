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

  const inputJson = JSON.parse(input)
  core.debug(JSON.stringify(inputJson))

  const command = inputJson.command
  if (command === 'Create') {
    const creationInput = inputJson.creationInput
    core.debug(JSON.stringify(creationInput))
    const containers = creationInput.containers
    const jobContainer = containers[0]

    // const networkName = 'actions_podman_network'
    // // podman network create {network}  -> track and return `network` for ${{job.container.network}}
    // await exec.exec('podman', ['network', 'create', networkName])

    const containerImage = `${jobContainer.containerImage}`
    // podman pull docker.io/library/{image}
    // await exec.exec('podman', ['pull', containerImage])

    // kubectl run e088c842be1f46b394212618408aaba0_node1016jessie_6196c9
    //      --image=node:10.16-jessie
    //      -- tail -f /dev/null
    const runArgs = ['run', 'job-container']
    // runArgs.push(`--workdir=${jobContainer.containerWorkDirectory}`)
    // runArgs.push(`--network=${networkName}`)

    // for (const mountVolume of jobContainer.mountVolumes) {
    //   runArgs.push(
    //     `-v=${mountVolume.sourceVolumePath}:${mountVolume.targetVolumePath}`
    //   )
    // }
    runArgs.push(`--image=${containerImage}`)
    runArgs.push(`--`)
    runArgs.push(`tail`)
    runArgs.push(`-f`)
    runArgs.push(`/dev/null`)

    core.debug(JSON.stringify(runArgs))

    // const containerId = await exec.getExecOutput('podman', [
    //   'create',
    //   // `--workdir ${jobContainer.containerWorkDirectory}`,
    //   `--network=${networkName}`,
    //   // `-v=/Users/ting/Desktop/runner/_layout/_work:/__w`,
    //   `--entrypoint=${jobContainer.containerEntryPoint}`,
    //   `${containerImage}`,
    //   `${jobContainer.containerEntryPointArgs}`
    // ])

    await exec.exec('kubectl', runArgs)

    // get PATH inside the container

    const waitArgs = ['wait', '--for=condition=Ready', 'pod/job-container']
    await exec.exec('kubectl', waitArgs)

    // output containerId for ${{job.container.id}}

    // copy over node.js
    const cpNodeArgs = [
      'cp',
      '/actions-runner/externals/node12/bin',
      'job-container:/__runner_util/'
    ]
    await exec.exec('kubectl', cpNodeArgs)

    // copy over innerhandler
    const cpKubeInnerArgs = [
      'cp',
      '/actions-runner/bin/kubeInnerHandler',
      'job-container:/__runner_util/kubeInnerHandler'
    ]
    await exec.exec('kubectl', cpKubeInnerArgs)

    const creationOutput = {
      JobContainerId: 'job-container',
      Network: 'job-container'
    }

    const output = JSON.stringify({CreationOutput: creationOutput})
    core.debug(output)

    process.stderr.write(
      `___CONTAINER_ENGINE_HANDLER_OUTPUT___${output}___CONTAINER_ENGINE_HANDLER_OUTPUT___`
    )
  } else if (command === 'Remove') {
    const removeInput = inputJson.removeInput
    core.debug(JSON.stringify(removeInput))
    const jobContainerId = removeInput.jobContainerId

    await exec.exec('kubectl', ['delete', 'pod', jobContainerId, '--force'])
    // await exec.exec('podman', ['network', 'rm', '-f', network])
  } else if (command === 'Exec') {
    const execInput = inputJson.execInput
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

    const execArgs = ['exec']
    execArgs.push(execInput.jobContainer.containerId)
    execArgs.push('-i')
    execArgs.push('-t')
    execArgs.push('--')
    execArgs.push('/__runner_util/node')
    execArgs.push('/__runner_util/kubeInnerHandler')

    core.debug(JSON.stringify(execArgs))

    await exec.exec('kubectl', execArgs, {
      input: Buffer.from(JSON.stringify(execInput))
    })
  }
}

run()
