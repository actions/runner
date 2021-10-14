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

    const networkName = 'actions_podman_network'
    // podman network create {network}  -> track and return `network` for ${{job.container.network}}
    await exec.exec('podman', ['network', 'create', networkName])

    const containerImage = `docker.io/library/${jobContainer.containerImage}`
    // podman pull docker.io/library/{image}
    await exec.exec('podman', ['pull', containerImage])

    // podman create --name e088c842be1f46b394212618408aaba0_node1016jessie_6196c9
    //      --label fa4e14
    //      --workdir /__w/canary/canary
    //      --network github_network_f98a6e1e96e74d919d814c165641cba3
    //      -e "HOME=/github/home" -e GITHUB_ACTIONS=true -e CI=true
    //      -v "/var/run/docker.sock":"/var/run/docker.sock"
    //      -v "/home/runner/work":"/__w"
    //      -v "/home/runner/runners/2.283.2/externals":"/__e":ro
    //      -v "/home/runner/work/_temp":"/__w/_temp"
    //      -v "/home/runner/work/_actions":"/__w/_actions"
    //      -v "/opt/hostedtoolcache":"/__t"
    //      -v "/home/runner/work/_temp/_github_home":"/github/home"
    //      -v "/home/runner/work/_temp/_github_workflow":"/github/workflow"
    //      --entrypoint "tail" node:10.16-jessie "-f" "/dev/null"
    const creatArgs = ['create']
    creatArgs.push(`--workdir=${jobContainer.containerWorkDirectory}`)
    creatArgs.push(`--network=${networkName}`)
    creatArgs.push(`--entrypoint=${jobContainer.containerEntryPoint}`)

    for (const mountVolume of jobContainer.mountVolumes) {
      creatArgs.push(
        `-v=${mountVolume.sourceVolumePath}:${mountVolume.targetVolumePath}`
      )
    }

    creatArgs.push(containerImage)
    creatArgs.push(jobContainer.containerEntryPointArgs)

    core.debug(JSON.stringify(creatArgs))

    // const containerId = await exec.getExecOutput('podman', [
    //   'create',
    //   // `--workdir ${jobContainer.containerWorkDirectory}`,
    //   `--network=${networkName}`,
    //   // `-v=/Users/ting/Desktop/runner/_layout/_work:/__w`,
    //   `--entrypoint=${jobContainer.containerEntryPoint}`,
    //   `${containerImage}`,
    //   `${jobContainer.containerEntryPointArgs}`
    // ])

    const containerId = await exec.getExecOutput('podman', creatArgs)

    core.debug(JSON.stringify(containerId))

    // podman start {containerId}
    await exec.exec('podman', ['start', containerId.stdout.trim()])

    // get PATH inside the container

    // output containerId for ${{job.container.id}}

    const creationOutput = {
      JobContainerId: containerId.stdout.trim(),
      Network: networkName
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
    const network = removeInput.network

    await exec.exec('podman', ['rm', '-f', jobContainerId])
    await exec.exec('podman', ['network', 'rm', '-f', network])
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
    execArgs.push('-i')
    execArgs.push(`--workdir=${execInput.workingDirectory}`)
    for (const envKey of execInput.environmentKeys) {
      execArgs.push(`-e=${envKey}`)
    }
    execArgs.push(execInput.jobContainer.containerId)
    execArgs.push(execInput.fileName)
    execArgs.push(execInput.arguments)

    core.debug(JSON.stringify(execArgs))

    await exec.exec('podman', execArgs)
  }

  await exec.exec('podman', ['network', 'ls'])
  await exec.exec('podman', ['ps', '-a'])
}

run()
