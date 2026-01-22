## SSL Certificate Related Issues

You might run into an SSL certificate error when your GitHub Enterprise Server is using a self-signed SSL server certificate or a web proxy within your network is decrypting HTTPS traffic for a security audit.

As long as your certificate is generated properly, most of the issues should be fixed after your trust the certificate properly on the runner machine.

> Different OS might have extra requirements on SSL certificate,
> Ex: macOS requires `ExtendedKeyUsage` https://support.apple.com/en-us/HT210176

### Don't skip SSL cert validation

> !!! DO NOT SKIP SSL CERT VALIDATION !!!  
> !!! IT IS A BAD SECURITY PRACTICE !!!  

### Download SSL certificate chain

Depends on how your SSL server certificate gets configured, you might need to download the whole certificate chain from a machine that has trusted the SSL certificate's CA.

- Approach 1: Download certificate chain using a browser (Chrome, Firefox, IT), you can google for more example, [here is what I found](https://medium.com/@menakajain/export-download-ssl-certificate-from-server-site-url-bcfc41ea46a2)

- Approach 2: Download certificate chain using OpenSSL, you can google for more example, [here is what I found](https://superuser.com/a/176721)

- Approach 3: Ask your network administrator or the owner of the CA certificate to send you a copy of it

### Trust CA certificate for the Runner

The actions runner is a dotnet core application which will follow how [dotnet load SSL CA certificates on each OS](https://docs.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography#x509store).

In short:
- Windows: Load from Windows certificate store.
- Linux: Load from OpenSSL CA cert bundle.
- macOS: Load from macOS KeyChain.

To let the runner trusts your CA certificate, you will need to:
1. Save your SSL certificate chain which includes the root CA and all intermediate CAs into a `.pem` file.
2. Use `OpenSSL` to convert `.pem` file to a proper format for different OS, here is some [doc with sample commands](https://www.sslshopper.com/ssl-converter.html)
3. Trust CA on different OS:
    - Windows: https://docs.microsoft.com/en-us/skype-sdk/sdn/articles/installing-the-trusted-root-certificate
    - macOS: ![trust ca cert](./../res/macOStrustCA.gif)
    - Linux: Refer to the distribution documentation
      1. Red Hat: https://www.redhat.com/sysadmin/ca-certificates-cli
      2. Ubuntu: http://manpages.ubuntu.com/manpages/focal/man8/update-ca-certificates.8.html
      3. Google search: "trust ca certificate on [linux distribution]"
      4. If all approaches failed, set environment variable `SSL_CERT_FILE` to the CA bundle `.pem` file we get.
    > To verify cert gets installed properly on Linux, you can try use `curl -v https://sitewithsslissue.com` and `pwsh -Command \"Invoke-WebRequest -Uri https://sitewithsslissue.com\"`

### Trust CA certificate for Git CLI

Git uses various CA bundle file depends on your operation system.
- Git packaged the CA bundle file within the Git installation on Windows
- Git use OpenSSL certificate CA bundle file on Linux and macOS

You can check where Git check CA file by running:
```bash
export GIT_CURL_VERBOSE=1
git ls-remote https://github.com/actions/runner HEAD
```

You should see something like:
```
* Couldn't find host github.com in the .netrc file; using defaults
*   Trying 140.82.114.4...
* TCP_NODELAY set
* Connected to github.com (140.82.114.4) port 443 (#0)
* ALPN, offering h2
* ALPN, offering http/1.1
* successfully set certificate verify locations:
*   CAfile: /etc/ssl/cert.pem
  CApath: none
* SSL connection using TLSv1.2 / ECDHE-RSA-AES128-GCM-SHA256
```
This tells me `/etc/ssl/cert.pem` is where it read trusted CA certificates.

To let Git trusts your CA certificate, you will need to:
1. Save your SSL certificate chain which includes the root CA and all intermediate CAs into a `.pem` file.
2. Set `http.sslCAInfo` Git config or `GIT_SSL_CAINFO` environment variable to the full path of the `.pem` file [Git Doc](https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpsslCAInfo)
> I would recommend using `http.sslCAInfo` since it can be scope to certain hosts that need the extra trusted CA.  
> Ex: `git config --global http.https://myghes.com/.sslCAInfo /extra/ca/cert.pem`  
> This will make Git use the `/extra/ca/cert.pem` only when communicates with `https://myghes.com` and keep using the default CA bundle with others.

### Trust CA certificate for Node.js

Node.js has compiled a snapshot of the Mozilla CA store that is fixed at each version of Node.js' release time.

To let Node.js trusts your CA certificate, you will need to:
1. Save your SSL certificate chain which includes the root CA and all intermediate CAs into a `.pem` file.
2. Set environment variable `NODE_EXTRA_CA_CERTS` which point to the file. ex: `export NODE_EXTRA_CA_CERTS=/full/path/to/cacert.pem` or `set NODE_EXTRA_CA_CERTS=C:\full\path\to\cacert.pem`
