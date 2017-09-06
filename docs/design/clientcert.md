# Support Ssl Client Certificate in Build/Release Job (TFS On-Prem Only)

## Goals

  - Support agent configure and connect to TFS use ssl client certificate
  - Support get source in Build job and download artifact in Release job works with ssl client certificate
  - Provide documentation and scripts to help customer prepare all pre-requrements before configuration
  - Expose ssl client certificate information in vsts-task-lib for task author

## Pre-requirements

  - CA certificate(s) in `.pem` format (This should contains the public key and signature of the CA certificate, you need put the root ca certificate and all your intermediate ca certificates into one `.pem` file)  
  - Client certificate in `.pem` format (This should contains the public key and signature of the Client certificate)  
  - Client certificate private key in `.pem` format (This should contains only the private key of the Client certificate)  
  - Client certificate archive package in `.pfx` format (This should contains the signature, public key and private key of the Client certificate)  
  - Use `SAME` password to protect Client certificate private key and Client certificate archive package, since they both have client certificate's private key  
  
The Build/Release agent is just xplat tool runner, base on what user defined in their Build/Release definition, invoke different tools to finish user's job. So the client certificate support is not only for the agent infrastructure but most important for all different tools and technologies user might use during a Build/Release job.
```
   Ex:
      Clone Git repository from TFS use Git
      Sync TFVC repository from TFS use Tf.exe on Windows and Tf on Linux/OSX
      Write customer Build/Release task that make REST call to TFS use VSTS-Task-Lib (PowerShell or Node.js)
      Consume Nuget/NPM packages from TFS package management use Nuget.exe and Npm
      [Future] Publish and consume artifacts from TFS artifact service use Drop.exe (artifact) and PDBSTR.exe (symbol)
```


You can use `OpenSSL` to get all pre-required certificates format ready easily as long as you have all pieces of information.

### Windows

Windows has a pretty good built-in certificate manger, the `Windows Certificate Manager`, it will make most Windows based application deal with certificate problem easily. However, most Linux background application (Git) and technologies (Node.js) won't check the `Windows Certificate Manager`, they just expect all certificates are just a file on disk.  

Use the following step to setup pre-reqs on Windows, assume you already installed your corporation's `CA root cert` into local machine's `Trusted CA Store`, and you have your client cert `clientcert.pfx` file on disk and you know the `password` for it.  
  - Export CA cert from `Trusted Root CA Store`, use `Base64 Encoding X.509 (.CER)` format, name the export cert to something like `ca.pem`.  
  - Export any intermediate CA cert from `Intermediate CA Store`, use `Base64 Encoding X.509 (.CER)` format, name the export cert to something like `ca_inter_1/2/3.pem`. Concatenate all intermediate ca certs into `ca.pem`, your `ca.pem` might looks like following:  
```
-----BEGIN CERTIFICATE----- 
(Your Root CA certificate: ca.pem) 
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE----- 
(Your Intermediate CA certificate: ca_inter_1.pem) 
-----END CERTIFICATE-----
...
-----BEGIN CERTIFICATE----- 
(Your Intermediate CA certificate: ca_inter_n.pem) 
-----END CERTIFICATE-----
```  
  - Extract Client cert and Client cert private key from `.pfx` file. You need `OpenSSL` to do this, you either install `OpenSSL for Windows` or just use `Git Bash`, since `Git Bash` has `OpenSSL` baked in.
```
Inside Git Bash:    
  Extract client-cert.pem
  openssl pkcs12 -in clientcert.pfx -passin pass:<YOURCERTPASSWORD> -nokeys -clcerts -out client-cert.pem
      
  Extract client-cert-key.pem, this will get password protected
  openssl pkcs12 -in clientcert.pfx -passin pass:<YOURCERTPASSWORD> -nocerts -out client-cert-key.pem -passout pass:<YOURCERTPASSWORD> 
```
    
At this point, you should have all required pieces `ca.pem`, `client-cert.pem`, `client-cert-key.pem` and `clientcert.pfx`.

### No-Windows

As I mentioned before, most Linux backgroud application just expect all certificate related files are on disk, and use `OpenSSL` to deal with cert is quiet common on Liunx, so I assume for customer who wants to setup Build/Release agent on Linux already has `ca.pem`, `client-cert.pem` and `client-cert-key.pem` in place. So the only missing piece should be the client cert archive `.pfx` file.  
```
From Terminal:
openssl pkcs12 -export -out client-cert-archive.pfx -passout pass:<YOURCERTPASSWORD> -inkey client-cert-key.pem -in client-cert.pem -passin pass:<YOURCERTPASSWORD> -certfile CA.pem
```

## Configuration

Pass `--sslcacert`, `--sslclientcert`, `--sslclientcertkey`. `--sslclientcertarchive` and `--sslclientcertpassword` during agent configuration.   
Ex:
```batch
.\config.cmd --sslcacert .\enterprise.pem --sslclientcert .\client.pem --sslclientcertkey .\clientcert-key-pass.pem --sslclientcertarchive .\clientcert-2.pfx --sslclientcertpassword "test123"
```  

We store your client cert private key password securely on each platform.  
Ex:
```
Windows: Windows Credential Store
OSX: OSX Keychain
Linux: Encrypted with symmetric key based on machine id
```

**Windows service configure limitation**  
Since we store your client certificate private key password into `Windows Credential Store` and the `Windows Credential Store` is per user, when you configure the agent as Windows service, you need run the configuration as the same user as the service is going to run as.  
Ex, in order to configure the agent service run as `mydomain\buildadmin`, you need either login the box as `mydomain\buildadmin` and run `config.cmd` or login the box as someone else but use `Run as different user` option when you run `config.cmd` to run as `mydomain\buildadmin`  

## How agent handle client cert within a Build/Release job

After configuring client cert for agent, agent infrastructure will start talk to VSTS/TFS service using the client cert configured.  

Since the code for `Get Source` step in build job and `Download Artifact` step in release job are also bake into agent, those steps will also follow the agent client cert configuration.  

Agent will expose client cert configuration via environment variables for every task execution, task author need to use `vsts-task-lib` methods to retrieve back client cert configuration and handle client cert with their task.

## Get client cert configuration by using [VSTS-Task-Lib](https://github.com/Microsoft/vsts-task-lib) method

Please reference [VSTS-Task-Lib doc](https://github.com/Microsoft/vsts-task-lib/blob/master/node/docs/cert.md) for detail

## Progress
 - Agent infrastructure (you can configure and queue a build/release) [DONE]
 - Fetch git repository [DONE]
 - Fetch tfvc repository [Only supported in Windows agent]
 - Expose client cert info to task sdk [DONE]

## Self-Signed CA Certificates

I would assume there are some self-signed CA certificates along with the client certificate, however the current agent doesn't has a good way to handle self-signed CA certificates.  

The work of the client certificate support do add a `--sslcacert` option to agent configuration, but it currentlly just for some of the downstream tools your Build/Release job and not for the agent infrastructure. In order to use self-signed CA certificates with the agent, you need to maunally install all self-signed CA certificates into your OS's certificate store, like: `Windows certificate manager` on `Windows`, `OpenSSL CA store` on `Linux`. Just like you have to manually configure your browser to take those certificates. We might be able to improve this when we consume netcore 2.0 in the agent.  

The next problem is about all different downstream tools you used in your Build/Release job, the way they find CA certificates might all different.  
Ex:
 - Git (version < 2.14.x) expect a `--cainfo` option and point to the CA file.  
 - Git (version >= 2.14.x) has a config option to let Git to read CA from `Windows Certificate Manager` on `Windows`.  
 - Tf.exe expect read CA from `Windows Certificate Manager`.  
 - TEE (tf on linux) expect read CA from `Java Certificate Store`.  
 - PowerShell expect read CA from `Windows Certificate Manager`.  
 - Node.js expect a `ca` parameter on `tls.options`.  
 - Node.js (version >= 7.3) also expect an environment vairble to point to the CA file `NODE_EXTRA_CA_CERTS`, however the agent current use Node.js version 6.x which mean we can't use that envirinment variable.  

At this point, I would sugguest when you have a self-signed CA cert, please make sure the tools or technologies you used within your Build/Release works with your self-signed CA cert first, then try to configure the agent.  
In this way, even you get an error within your build/release job, you might have better idea of where is the error coming from.  
