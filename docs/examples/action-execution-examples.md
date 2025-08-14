# Action Execution Examples

This directory contains examples demonstrating how different types of GitHub Actions are executed without compilation.

## JavaScript Action Example

A simple JavaScript action that runs source code directly:

### action.yml
```yaml
name: 'JavaScript Example'
description: 'Demonstrates direct JavaScript execution'
runs:
  using: 'node20'
  main: 'index.js'
```

### index.js
```javascript
// This file runs directly - no compilation needed
console.log('Hello from JavaScript action!');
console.log('Process args:', process.argv);
console.log('Environment:', process.env.INPUT_MESSAGE || 'No input provided');
```

**Execution**: The runner directly executes `node index.js` - no build step.

## Container Action Example

### action.yml (Pre-built image)
```yaml
name: 'Container Example'
description: 'Demonstrates container execution'
runs:
  using: 'docker'
  image: 'docker://alpine:latest'
  entrypoint: '/bin/sh'
  args:
    - '-c'
    - 'echo "Hello from container!" && env | grep INPUT_'
```

### action.yml (Build from source)
```yaml
name: 'Container Build Example'
description: 'Demonstrates building from Dockerfile'
runs:
  using: 'docker'
  image: 'Dockerfile'
  args:
    - 'Hello from built container!'
```

### Dockerfile
```dockerfile
FROM alpine:latest
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
```

### entrypoint.sh
```bash
#!/bin/sh
echo "Container built and running: $1"
echo "Environment variables:"
env | grep INPUT_ || echo "No INPUT_ variables found"
```

**Execution**: Docker builds the image (if needed) and runs the container - action source isn't compiled.

## Composite Action Example

### action.yml
```yaml
name: 'Composite Example'
description: 'Demonstrates composite action execution'
runs:
  using: 'composite'
  steps:
    - name: Run shell command
      run: echo "Step 1: Hello from composite action!"
      shell: bash
    
    - name: Use another action
      uses: actions/checkout@v4
      with:
        path: 'checked-out-code'
    
    - name: Run another shell command
      run: |
        echo "Step 3: Files in workspace:"
        ls -la
      shell: bash
```

**Execution**: The runner interprets the YAML and executes each step - no compilation.

## Comparison with Runner Compilation

The **runner itself** (this repository) must be compiled:

```bash
# This compiles the runner from C# source code
cd src
./dev.sh build

# The compiled runner then executes actions WITHOUT compiling them
./_layout/bin/Runner.Worker
```

## Key Takeaway

- **Actions** = Interpreted at runtime (JavaScript, containers, YAML)
- **Runner** = Compiled from source (C# → binaries)

The runner compiles once and then executes many different actions without compiling them.