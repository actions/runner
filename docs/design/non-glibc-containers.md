# Non-glibc Containers

If you want to use a non-glibc-based container, such as Alpine Linux, you will need to arrange a few things on your own.
First, you must supply your own copy of Node.js.
Second, you must add a label to your image telling the agent where to find the Node.js binary.
Finally, stock Alpine doesn't come with other dependencies that Azure Pipelines depends on:
bash, sudo, which, and groupadd.

## Bring your own Node.js
You are responsible for adding a Node LTS binary to your container.
As of November 2018, we expect that to be Node 10 LTS.
You can start from the `node:10-alpine` image.

## Tell the agent about Node.js
The agent will read a container label "com.azure.dev.pipelines.handler.node.path".
If it exists, it must be the path to the Node.js binary.
For example, in an image based on `node:10-alpine`, add this line to your Dockerfile:
```
LABEL "com.azure.dev.pipelines.agent.handler.node.path"="/usr/local/bin/node"
```

## Add requirements
Azure Pipelines assumes a bash-based system with common administration packages installed.
Alpine Linux in particular doesn't come with several of the packages needed.
Installing `bash`, `sudo`, `which`, and `shadow` will cover the basic needs.
```
RUN apk add bash sudo which shadow
```

If you depend on any in-box or Marketplace tasks, you'll also need to supply the binaries they require.

## Full example of a Dockerfile

```
FROM node:10-alpine

RUN apk add --no-cache --virtual .pipeline-deps readline linux-pam \
  && apk add bash sudo which shadow \
  && apk del .pipeline-deps

LABEL "com.azure.dev.pipelines.agent.handler.node.path"="/usr/local/bin/node"

CMD [ "node" ]

```
