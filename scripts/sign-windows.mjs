/**
 * Sign one .exe or .dll in place with SSL.com eSigner.
 *
 *     node scripts/sign-windows.mjs <file>
 *
 * Needs CodeSignTool from `download-codesigntool.ps1` and the ES_* credentials
 * in the environment.
 */

import { spawnSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { basename, dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const CODESIGNTOOL = resolve(process.env.CODESIGNTOOL_DIR ?? join(ROOT, 'CodeSignTool'))
const TEMP = join(ROOT, 'CodeSignTool-out')
const CREDENTIALS = ['ES_USERNAME', 'ES_PASSWORD', 'ES_CREDENTIAL_ID', 'ES_TOTP_SECRET']

/** Whether a PE has a non-empty certificate table (data directory entry 4). */
function hasCertificateTable(bytes) {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength)
  const pe = view.getUint32(0x3c, true)
  if (bytes[pe] !== 0x50 || bytes[pe + 1] !== 0x45) return false
  const optional = pe + 24
  const directories = optional + (view.getUint16(optional, true) === 0x20b ? 112 : 96)
  return view.getUint32(directories + 4 * 8 + 4, true) > 0
}

function redact(text, secrets) {
  let out = text
  for (const secret of secrets) {
    if (secret) out = out.split(secret).join('***')
  }
  return out
}

const file = process.argv[2]
if (file === undefined || process.argv.length > 3) {
  console.error('usage: node scripts/sign-windows.mjs <file>')
  process.exit(2)
}
const target = resolve(file)

const missing = CREDENTIALS.filter((name) => !process.env[name])
if (missing.length > 0) {
  console.error(`cannot sign ${target}: ${missing.join(', ')} not set`)
  process.exit(1)
}
const [username, password, credentialId, totpSecret] = CREDENTIALS.map((name) => process.env[name])
const secrets = [password, totpSecret, credentialId, username]

const tool = join(CODESIGNTOOL, 'CodeSignTool.bat')
if (!existsSync(tool)) {
  console.error(`cannot sign ${target}: no CodeSignTool.bat in ${CODESIGNTOOL}`)
  process.exit(1)
}

// CodeSignTool won't overwrite its input, so sign a copy and write it back.
const inDir = join(TEMP, 'in')
const outDir = join(TEMP, 'out')
const presented = join(inDir, basename(target))
const produced = join(outDir, basename(target))

let failure

try {
  rmSync(TEMP, { recursive: true, force: true })
  mkdirSync(inDir, { recursive: true })
  mkdirSync(outDir, { recursive: true })
  const original = readFileSync(target)
  writeFileSync(presented, original)

  const command =
    `"${tool}" sign -input_file_path="${presented}" -output_dir_path="${outDir}"` +
    ` -credential_id="${credentialId}" -username="${username}"` +
    ` -password="${password}" -totp_secret="${totpSecret}"`

  // CodeSignTool reports some errors on stderr and still exits zero, so print
  // both streams and check what it wrote rather than trusting the exit code.
  const result = spawnSync(command, { cwd: CODESIGNTOOL, encoding: 'utf8', shell: true })
  const output = [result.stdout, result.stderr].filter(Boolean).join('\n')
  console.log(redact(output, secrets).trim())

  if (result.error !== undefined) {
    failure = `could not run CodeSignTool: ${result.error.message}`
  } else if (result.status !== 0) {
    failure = `CodeSignTool exited with code ${result.status}`
  } else if (!existsSync(produced)) {
    failure = `CodeSignTool wrote no file to ${outDir}`
  } else {
    const bytes = readFileSync(produced)
    if (bytes.equals(original) || !hasCertificateTable(bytes)) {
      failure = `CodeSignTool returned ${basename(target)} unsigned`
    } else {
      writeFileSync(target, bytes)
      console.log(`${target}: signed`)
    }
  }
} catch (error) {
  failure = `signing ${target} failed: ${error.message}`
}

rmSync(TEMP, { recursive: true, force: true })

if (failure !== undefined) {
  console.error(failure)
  process.exit(1)
}
