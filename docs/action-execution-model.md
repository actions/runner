# GitHub Actions Execution Model

## Question: Do Actions Need to be Compiled?

**Short Answer**: No, GitHub Actions themselves do **NOT** need to be compiled from source code. They run directly as interpreted code, container images, or step definitions.

## How Different Action Types Are Executed

### 1. JavaScript Actions (`using: node12/16/20/24`)

JavaScript actions execute source code directly without compilation:

```yaml
# action.yml
runs:
  using: 'node20'
  main: 'index.js'
```

**Execution Process**:
1. Runner downloads the action repository
2. Locates the `main` JavaScript file (e.g., `index.js`)
3. Executes it directly using Node.js runtime: `node index.js`
4. No compilation or build step required

**Code Reference**: `src/Runner.Worker/Handlers/NodeScriptActionHandler.cs`
- Resolves the target script file
- Executes using Node.js: `StepHost.ExecuteAsync()` with node executable

### 2. Container Actions (`using: docker`)

Container actions run pre-built images or build from Dockerfile:

```yaml
# action.yml - Pre-built image
runs:
  using: 'docker'
  image: 'docker://alpine:3.10'
```

```yaml
# action.yml - Build from Dockerfile
runs:
  using: 'docker'
  image: 'Dockerfile'
```

**Execution Process**:
1. If using pre-built image: Pull and run the container
2. If using Dockerfile: Build the container image, then run it
3. No compilation of action source code - Docker handles image building

**Code Reference**: `src/Runner.Worker/Handlers/ContainerActionHandler.cs`
- Handles both pre-built images and Dockerfile builds
- Uses Docker commands to run containers

### 3. Composite Actions (`using: composite`)

Composite actions are collections of steps defined in YAML:

```yaml
# action.yml
runs:
  using: 'composite'
  steps:
    - run: echo "Hello"
      shell: bash
    - uses: actions/checkout@v3
```

**Execution Process**:
1. Parse the YAML step definitions
2. Execute each step in sequence
3. No compilation - just step orchestration

**Code Reference**: `src/Runner.Worker/Handlers/CompositeActionHandler.cs`
- Iterates through defined steps
- Executes each step using appropriate handlers

## What Does Get Compiled?

### The GitHub Actions Runner (This Repository)

The runner itself is compiled from C# source code:

```bash
cd src
./dev.sh build  # Compiles the runner binaries
```

**What gets compiled**:
- `Runner.Listener` - Registers with GitHub and receives jobs
- `Runner.Worker` - Executes individual jobs and steps
- `Runner.PluginHost` - Handles plugin execution
- Supporting libraries

**Build Output**: Compiled binaries in `_layout/bin/`

## Key Distinctions

| Component | Compilation Required | Execution Method |
|-----------|---------------------|------------------|
| **Runner** (this repo) | ✅ Yes - C# → binaries | Compiled executable |
| **JavaScript Actions** | ❌ No | Direct interpretation |
| **Container Actions** | ❌ No* | Container runtime |
| **Composite Actions** | ❌ No | YAML interpretation |

*Container actions may involve building Docker images, but not compiling action source code.

## Implementation Details

### Action Loading Process

1. **Action Discovery** (`ActionManager.LoadAction()`)
   - Parses `action.yml` manifest
   - Determines action type from `using` field
   - Creates appropriate execution data object

2. **Handler Selection** (`HandlerFactory.Create()`)
   - Routes to appropriate handler based on action type
   - `NodeScriptActionHandler` for JavaScript
   - `ContainerActionHandler` for Docker
   - `CompositeActionHandler` for composite

3. **Execution** (Handler-specific `RunAsync()`)
   - Each handler implements execution logic
   - No compilation step - direct execution

### Source Code References

- **Action Type Detection**: `src/Runner.Worker/ActionManifestManager.cs:428-495`
- **Handler Factory**: `src/Runner.Worker/Handlers/HandlerFactory.cs`
- **JavaScript Execution**: `src/Runner.Worker/Handlers/NodeScriptActionHandler.cs:143-153`
- **Container Execution**: `src/Runner.Worker/Handlers/ContainerActionHandler.cs:247-261`

## Conclusion

GitHub Actions are designed for **runtime interpretation**, not compilation:

- **JavaScript actions** run source `.js` files directly
- **Container actions** use existing images or build from Dockerfile
- **Composite actions** are YAML step definitions

The only compilation involved is building the **runner infrastructure** (this repository) that interprets and executes the actions.