cd $env:RUNNER_HOME
if (!(Test-Path .\_work)) {
  .\config.cmd --unattended --url https://github.com/web-infra-dev --name "$env:RUNNER_NAME" --labels "$env:RUNNER_LABELS" --token "$env:RUNNER_TOKEN" --runnergroup default --work _work --disableupdate;
}

.\run.cmd
