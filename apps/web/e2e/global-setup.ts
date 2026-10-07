import { createReadStream } from 'node:fs'
import { stat } from 'node:fs/promises'
import { createServer } from 'node:http'
import { fileURLToPath } from 'node:url'
import { extname, join, normalize } from 'node:path'

const root = fileURLToPath(new URL('../.output/public/', import.meta.url))
const mimeTypes: Record<string, string> = {
  '.css': 'text/css; charset=utf-8',
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
}

const findFile = async (urlPath: string) => {
  const safePath = normalize(decodeURIComponent(urlPath))
    .replace(/^(\.\.[/\\])+/, '')
    .replace(/^[/\\]+/, '')
  for (const candidate of [join(root, safePath), join(root, safePath, 'index.html')]) {
    try {
      if ((await stat(candidate)).isFile()) return candidate
    }
    catch {
      // Try the directory index, then fall back to the SPA shell.
    }
  }
  return join(root, '200.html')
}

export default async () => {
  const server = createServer(async (request, response) => {
    const path = new URL(request.url || '/', 'http://localhost').pathname
    const file = await findFile(path)
    response.setHeader('Content-Type', mimeTypes[extname(file)] || 'application/octet-stream')
    createReadStream(file).pipe(response)
  })
  await new Promise<void>((resolve, reject) => {
    server.once('error', reject)
    server.listen(3100, '127.0.0.1', resolve)
  })
  return () => new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()))
}
