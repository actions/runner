Use Node.js 22.22.1 or later in the 22.x release line, or Node.js 24 or later, for development (including `lint-staged`). ESLint 10 itself requires Node.js `^20.19.0 || ^22.13.0 || >=24`; the runner's bundled Node.js runtime is independent of this development toolchain.

To compile this package (output will be stored in `Misc/layoutbin`) run `npm ci && npm run all`.

Linting uses `eslint.config.mjs`. Run `npm run lint` to check the TypeScript source and `npm test` to verify lint rules, Node type information, and ignored paths. These checks also run as part of `npm run all` and Runner CI.

When you commit changes to the JSON or Typescript file, the javascript binary will be automatically re-compiled and added to the latest commit.
