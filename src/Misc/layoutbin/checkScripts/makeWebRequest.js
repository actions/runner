const https = require('https')
const http = require('http')
const hostname = process.env['HOSTNAME'] || ''
const port = process.env['PORT'] || ''
const path = process.env['PATH'] || ''
const pat = process.env['PAT'] || ''
const proxyHost = process.env['PROXYHOST'] || ''
const proxyPort = process.env['PROXYPORT'] || ''
const proxyUsername = process.env['PROXYUSERNAME'] || ''
const proxyPassword = process.env['PROXYPASSWORD'] || ''

if (proxyHost === '') {
    const options = {
        hostname: hostname,
        port: port,
        path: path,
        method: 'GET',
        headers: {
            'User-Agent': 'GitHubActionsRunnerCheck/1.0',
            'Authorization': `token ${pat}`,
        }
    }
    const req = https.request(options, res => {
        console.log(`statusCode: ${res.statusCode}`)
        console.log(`headers: ${JSON.stringify(res.headers)}`)

        res.on('data', d => {
            process.stdout.write(d)
        })
    })
    req.on('error', error => {
        console.error(error)
    })
    req.end()
}
else {
    const proxyAuth = 'Basic ' + Buffer.from(proxyUsername + ':' + proxyPassword).toString('base64')
    const options = {
        hostname: proxyHost,
        port: proxyPort,
        method: 'CONNECT',
        path: `${hostname}:${port}`
    }

    if (proxyUsername != '' || proxyPassword != '') {
        options.headers = {
            'Proxy-Authorization': proxyAuth,
        }
    }
    http.request(options).on('connect', (res, socket) => {
        if (res.statusCode != 200) {
            throw new Error(`Proxy returns code: ${res.statusCode}`)
        }
        https.get({
            host: hostname,
            port: port,
            socket: socket,
            agent: false,
            path: path,
            headers: {
                'User-Agent': 'GitHubActionsRunnerCheck/1.0',
                'Authorization': `token ${pat}`,
            }
        }, (res) => {
            console.log(`statusCode: ${res.statusCode}`)
            console.log(`headers: ${JSON.stringify(res.headers)}`)

            res.on('data', d => {
                process.stdout.write(d)
            })
        })
    }).on('error', (err) => {
        console.error('error', err)
    }).end()
}