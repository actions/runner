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
    const containerId = await exec.getExecOutput('podman', [
      'create',
      // `--workdir ${jobContainer.containerWorkDirectory}`,
      `--network=${networkName}`,
      // `-v /Users/ting/Desktop/runner/_layout/_work:/__w`,
      `--entrypoint=${jobContainer.containerEntryPoint}`,
      `${containerImage}`,
      `${jobContainer.containerEntryPointArgs}`
    ])

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

    process.stderr.write(output)
  }
  // else if (command === 'Remove') {
  // } else if (command === 'Exec') {
  // }
  await exec.exec('podman', ['network', 'ls'])
}

run()
