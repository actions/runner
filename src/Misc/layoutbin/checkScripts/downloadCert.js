const https = require('https')
const fs = require('fs')
const http = require('http')
const hostname = process.env['HOSTNAME'] || ''
const port = process.env['PORT'] || ''
const path = process.env['PATH'] || ''
const pat = process.env['PAT'] || ''
const proxyHost = process.env['PROXYHOST'] || ''
const proxyPort = process.env['PROXYPORT'] || ''
const proxyUsername = process.env['PROXYUSERNAME'] || ''
const proxyPassword = process.env['PROXYPASSWORD'] || ''

process.env['NODE_TLS_REJECT_UNAUTHORIZED'] = '0'

if (proxyHost === '') {
    const options = {
        hostname: hostname,
        port: port,
        path: path,
        method: 'GET',
        headers: {
            'User-Agent': 'GitHubActionsRunnerCheck/1.0',
            'Authorization': `token ${pat}`
        },
    }
    const req = https.request(options, res => {
        console.log(`statusCode: ${res.statusCode}`)
        console.log(`headers: ${JSON.stringify(res.headers)}`)
        let cert = socket.getPeerCertificate(true)
        let certPEM = ''
        let fingerprints = {}
        while (cert != null && fingerprints[cert.fingerprint] != '1') {
            fingerprints[cert.fingerprint] = '1'
            certPEM = certPEM + '-----BEGIN CERTIFICATE-----\n'
            let certEncoded = cert.raw.toString('base64')
            for (let i = 0; i < certEncoded.length; i++) {
                certPEM = certPEM + certEncoded[i]
                if (i != certEncoded.length - 1 && (i + 1) % 64 == 0) {
                    certPEM = certPEM + '\n'
                }
            }
            certPEM = certPEM + '\n-----END CERTIFICATE-----\n'
            cert = cert.issuerCertificate
        }
        console.log(certPEM)
        fs.writeFileSync('./download_ca_cert.pem', certPEM)
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
    const auth = 'Basic ' + Buffer.from(proxyUsername + ':' + proxyPassword).toString('base64')

    const options = {
        host: proxyHost,
        port: proxyPort,
        method: 'CONNECT',
        path: `${hostname}:${port}`,
    }

    if (proxyUsername != '' || proxyPassword != '') {
        options.headers = {
            'Proxy-Authorization': auth,
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
            path: '/',
            headers: {
                'User-Agent': 'GitHubActionsRunnerCheck/1.0',
                'Authorization': `token ${pat}`
            }
        }, (res) => {
            let cert = res.socket.getPeerCertificate(true)
            let certPEM = ''
            let fingerprints = {}
            while (cert != null && fingerprints[cert.fingerprint] != '1') {
                fingerprints[cert.fingerprint] = '1'
                certPEM = certPEM + '-----BEGIN CERTIFICATE-----\n'
                let certEncoded = cert.raw.toString('base64')
                for (let i = 0; i < certEncoded.length; i++) {
                    certPEM = certPEM + certEncoded[i]
                    if (i != certEncoded.length - 1 && (i + 1) % 64 == 0) {
                        certPEM = certPEM + '\n'
                    }
                }
                certPEM = certPEM + '\n-----END CERTIFICATE-----\n'
                cert = cert.issuerCertificate
            }
            console.log(certPEM)
            fs.writeFileSync('./download_ca_cert.pem', certPEM)
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