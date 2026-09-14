import assert from 'node:assert/strict'
import {createRequire} from 'node:module'
import {fileURLToPath} from 'node:url'
import {test} from 'node:test'

const packageUrl = new URL(
  '../../Misc/expressionFunc/hashFiles/',
  import.meta.url
)
const require = createRequire(new URL('package.json', packageUrl))
const {ESLint} = require('eslint')
const eslint = new ESLint({cwd: fileURLToPath(packageUrl)})

test('hashFiles source passes lint with type-aware rules enabled', async () => {
  const results = await eslint.lintFiles(['src/**/*.ts'])
  assert.equal(results.length, 1)
  assert.deepEqual(results[0].messages, [])

  const config = await eslint.calculateConfigForFile('src/hashFiles.ts')
  assert.equal(config.rules['@typescript-eslint/await-thenable'][0], 2)
  assert.equal(config.rules['@typescript-eslint/no-unused-vars'][0], 2)
  assert.equal(config.rules['github/array-foreach'][0], 2)
  assert.equal(config.rules['github/no-then'][0], 1)
  assert.equal(config.rules['no-console'][0], 0)
  assert.equal(config.rules['import/no-namespace'][0], 0)
})

test('generated output, dependencies and dotfiles remain ignored', async () => {
  for (const file of [
    'dist/generated.ts',
    'nested/dist/generated.ts',
    'lib/generated.ts',
    'nested/lib/generated.ts',
    'node_modules/dependency/index.ts',
    'nested/node_modules/dependency/index.ts',
    '.hidden.ts',
    '.cache/generated.ts'
  ]) {
    assert.equal(await eslint.isPathIgnored(file), true, file)
  }
  assert.equal(await eslint.isPathIgnored('src/hashFiles.ts'), false)
})

test('Node globals and intentional namespace imports remain allowed', async () => {
  const [result] = await eslint.lintText(
    "import * as path from 'path'\n\nexport function workspace(): string {\n  console.log(process.cwd())\n  return path.resolve(process.cwd())\n}\n",
    {filePath: 'src/hashFiles.ts'}
  )
  assert.deepEqual(result.messages, [])
})

test('invalid code still triggers TypeScript, GitHub and stylistic rules', async () => {
  const [result] = await eslint.lintText(
    'export async function invalid(): Promise<void> {\n  const unused = 1\n  await 42\n  ;[1, 2].forEach(value => console.log (value));\n}\n',
    {filePath: 'src/hashFiles.ts'}
  )
  assert.equal(result.fatalErrorCount, 0)
  for (const rule of [
    '@typescript-eslint/no-unused-vars',
    '@typescript-eslint/await-thenable',
    'github/array-foreach',
    '@stylistic/function-call-spacing',
    '@stylistic/semi'
  ]) {
    assert.ok(
      result.messages.some(message => message.ruleId === rule),
      `${rule}: ${JSON.stringify(result.messages)}`
    )
  }
})

test('type-aware lint uses Node declarations', async () => {
  const [result] = await eslint.lintText(
    'export async function invalid(): Promise<void> {\n  await process.cwd()\n}\n',
    {filePath: 'src/hashFiles.ts'}
  )
  assert.equal(result.fatalErrorCount, 0)
  assert.ok(
    result.messages.some(
      message => message.ruleId === '@typescript-eslint/await-thenable'
    ),
    JSON.stringify(result.messages)
  )
})

test('recommended import and global checks still reject invalid code', async () => {
  const [result] = await eslint.lintText(
    "import {missing} from './missing-module'\n\nexport function invalid(): void {\n  console.log(missing, unknownGlobal)\n}\n",
    {filePath: 'src/hashFiles.ts'}
  )
  assert.equal(result.fatalErrorCount, 0)
  for (const rule of ['import/no-unresolved', 'no-undef']) {
    assert.ok(
      result.messages.some(message => message.ruleId === rule),
      `${rule}: ${JSON.stringify(result.messages)}`
    )
  }
})
