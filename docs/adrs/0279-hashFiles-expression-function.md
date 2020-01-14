# ADR 0279: HashFiles Expression Function

**Date**: 2019-09-30

**Status**: Accepted

## Context
First party action `actions/cache` needs a input which is an explicit `key` used for restoring and saving the cache. For packages caching, the most comment `key` might be the hash result of contents from all `package-lock.json` under `node_modules` folder.
  
There are serval different ways to get the hash `key` input for `actions/cache` action.

1. Customer calculate the `key` themselves from a different action, customer won't like this since it needs extra step for using cache feature
```yaml
  steps:
  - run: |
      hash=some_linux_hash_method(file1, file2, file3)
      echo ::set-output name=hash::$hash
    id: createHash
  - uses: actions/cache@v1
    with:
      key: ${{ steps.createHash.outputs.hash }}
``` 

2. Make the `key` input of `actions/cache` follow certain convention to calculate hash, this limited the `key` input to a certain format customer may not want.
```yaml
  steps:
  - uses: actions/cache@v1
    with:
      key: ${{ runner.os }}|${{ github.workspace }}|**/package-lock.json
```

## Decision

### Add hashFiles() function to expression engine for calculate files' hash

`hashFiles()` will only allow on runner side since it needs to read files on disk, using `hashFiles()` on any server side evaluated expression will cause runtime errors.

`hashFiles()` will only support hashing files under the `$GITHUB_WORKSPACE` since the expression evaluated on the runner, if customer use job container or container action, the runner won't have access to file system inside the container.

`hashFiles()` will only take 1 parameters:
 - `hashFiles('**/package-lock.json')`  // Search files under $GITHUB_WORKSPACE and calculate a hash for them

**Question: Do we need to support more than one match patterns?**  
Ex: `hashFiles('**/package-lock.json', '!toolkit/core/package-lock.json', '!toolkit/io/package-lock.json')`  
Answer: Only support single match pattern for GA, we can always add later.

This will help customer has better experience with the `actions/cache` action's input.
```yaml
  steps:
  - uses: actions/cache@v1
    with:
      key: ${{hashFiles('**/package-lock.json')}}-${{github.ref}}-${{runner.os}}
```

For search pattern, we will use basic globbing (`*` `?` and `[]`) and globstar (`**`).

Additional pattern details:
- Root relative paths with `github.workspace` (the main repo)
- Make `*` match files that start with `.`
- Case insensitive on Windows
- Accept `\` or `/` path separators on Windows

Hashing logic:
1. Get all files under `$GITHUB_WORKSPACE`.
2. Use search pattern filter all files to get files that matches the search pattern. (search pattern only apply to file path not folder path)
3. Sort all matched files by full file path in alphabet order.
4. Use SHA256 algorithm to hash each matched file and store hash result.
5. Use SHA256 to hash all stored files' hash results to get the final 64 chars hash result.

**Question: Should we include the folder structure info into the hash?**  
Answer: No