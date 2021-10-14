// Job container creation

// podman network create {network}  -> track and return `network` for ${{job.container.network}}

// podman pull docker.io/library/{image}

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

// podman start {containerId}

// get PATH inside the container

// output containerId for ${{job.container.id}}



// Job container stop

// podman rm --force {containerId}

// podman network rm {network}


// Run step

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