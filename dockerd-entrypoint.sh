#!/bin/sh
set -eu

_tls_ensure_private() {
	local f="$1"; shift
	[ -s "$f" ] || openssl genrsa -out "$f" 4096
}
_tls_san() {
	{
		ip -oneline address | awk '{ gsub(/\/.+$/, "", $4); print "IP:" $4 }'
		{
			cat /etc/hostname
			echo 'docker'
			echo 'localhost'
			hostname -f
			hostname -s
		} | sed 's/^/DNS:/'
		[ -z "${DOCKER_TLS_SAN:-}" ] || echo "$DOCKER_TLS_SAN"
	} | sort -u | xargs printf '%s,' | sed "s/,\$//"
}
_tls_generate_certs() {
	local dir="$1"; shift

	# if ca/key.pem || !ca/cert.pem, generate CA public if necessary
	# if ca/key.pem, generate server public
	# if ca/key.pem, generate client public
	# (regenerating public certs every startup to account for SAN/IP changes and/or expiration)

	# https://github.com/FiloSottile/mkcert/issues/174
	local certValidDays='825'

	if [ -s "$dir/ca/key.pem" ] || [ ! -s "$dir/ca/cert.pem" ]; then
		# if we either have a CA private key or do *not* have a CA public key, then we should create/manage the CA
		mkdir -p "$dir/ca"
		_tls_ensure_private "$dir/ca/key.pem"
		openssl req -new -key "$dir/ca/key.pem" \
			-out "$dir/ca/cert.pem" \
			-subj '/CN=docker:dind CA' -x509 -days "$certValidDays"
	fi

	if [ -s "$dir/ca/key.pem" ]; then
		# if we have a CA private key, we should create/manage a server key
		mkdir -p "$dir/server"
		_tls_ensure_private "$dir/server/key.pem"
		openssl req -new -key "$dir/server/key.pem" \
			-out "$dir/server/csr.pem" \
			-subj '/CN=docker:dind server'
		cat > "$dir/server/openssl.cnf" <<-EOF
			[ x509_exts ]
			subjectAltName = $(_tls_san)
		EOF
		openssl x509 -req \
				-in "$dir/server/csr.pem" \
				-CA "$dir/ca/cert.pem" \
				-CAkey "$dir/ca/key.pem" \
				-CAcreateserial \
				-out "$dir/server/cert.pem" \
				-days "$certValidDays" \
				-extfile "$dir/server/openssl.cnf" \
				-extensions x509_exts
		cp "$dir/ca/cert.pem" "$dir/server/ca.pem"
		openssl verify -CAfile "$dir/server/ca.pem" "$dir/server/cert.pem"
	fi

	if [ -s "$dir/ca/key.pem" ]; then
		# if we have a CA private key, we should create/manage a client key
		mkdir -p "$dir/client"
		_tls_ensure_private "$dir/client/key.pem"
		chmod 0644 "$dir/client/key.pem" # openssl defaults to 0600 for the private key, but this one needs to be shared with arbitrary client contexts
		openssl req -new \
				-key "$dir/client/key.pem" \
				-out "$dir/client/csr.pem" \
				-subj '/CN=docker:dind client'
		cat > "$dir/client/openssl.cnf" <<-'EOF'
			[ x509_exts ]
			extendedKeyUsage = clientAuth
		EOF
		openssl x509 -req \
				-in "$dir/client/csr.pem" \
				-CA "$dir/ca/cert.pem" \
				-CAkey "$dir/ca/key.pem" \
				-CAcreateserial \
				-out "$dir/client/cert.pem" \
				-days "$certValidDays" \
				-extfile "$dir/client/openssl.cnf" \
				-extensions x509_exts
		cp "$dir/ca/cert.pem" "$dir/client/ca.pem"
		openssl verify -CAfile "$dir/client/ca.pem" "$dir/client/cert.pem"
	fi
}

# no arguments passed
# or first arg is `-f` or `--some-option`
if [ "$#" -eq 0 ] || [ "${1#-}" != "$1" ]; then
	# set "dockerSocket" to the default "--host" *unix socket* value (for both standard or rootless)
	uid="$(id -u)"
	if [ "$uid" = '0' ]; then
		dockerSocket='unix:///var/run/docker.sock'
	else
		# if we're not root, we must be trying to run rootless
		: "${XDG_RUNTIME_DIR:=/run/user/$uid}"
		dockerSocket="unix://$XDG_RUNTIME_DIR/docker.sock"
	fi
	case "${DOCKER_HOST:-}" in
		unix://*)
			dockerSocket="$DOCKER_HOST"
			;;
	esac

	# add our default arguments
	if [ -n "${DOCKER_TLS_CERTDIR:-}" ] \
		&& _tls_generate_certs "$DOCKER_TLS_CERTDIR" \
		&& [ -s "$DOCKER_TLS_CERTDIR/server/ca.pem" ] \
		&& [ -s "$DOCKER_TLS_CERTDIR/server/cert.pem" ] \
		&& [ -s "$DOCKER_TLS_CERTDIR/server/key.pem" ] \
	; then
		# generate certs and use TLS if requested/possible (default in 19.03+)
		set -- dockerd \
			--host="$dockerSocket" \
			--host=tcp://0.0.0.0:6789 \
			--tlsverify \
			--tlscacert "$DOCKER_TLS_CERTDIR/server/ca.pem" \
			--tlscert "$DOCKER_TLS_CERTDIR/server/cert.pem" \
			--tlskey "$DOCKER_TLS_CERTDIR/server/key.pem" \
			"$@"
		DOCKERD_ROOTLESS_ROOTLESSKIT_FLAGS="${DOCKERD_ROOTLESS_ROOTLESSKIT_FLAGS:-} -p 0.0.0.0:6789:6789/tcp"
	else
		# TLS disabled (-e DOCKER_TLS_CERTDIR='') or missing certs
		set -- dockerd \
			--host="$dockerSocket" \
			--host=tcp://0.0.0.0:6788 \
			"$@"
		DOCKERD_ROOTLESS_ROOTLESSKIT_FLAGS="${DOCKERD_ROOTLESS_ROOTLESSKIT_FLAGS:-} -p 0.0.0.0:6788:6788/tcp"
	fi
fi

if [ "$1" = 'dockerd' ]; then
	# explicitly remove Docker's default PID file to ensure that it can start properly if it was stopped uncleanly (and thus didn't clean up the PID file)
	find /run /var/run -iname 'docker*.pid' -delete || :

	uid="$(id -u)"
	if [ "$uid" != '0' ]; then
		# if we're not root, we must be trying to run rootless
		if ! command -v rootlesskit > /dev/null; then
			echo >&2 "error: attempting to run rootless dockerd but missing 'rootlesskit' (perhaps the 'docker:dind-rootless' image variant is intended?)"
			exit 1
		fi
		user="$(id -un 2>/dev/null || :)"
		if ! grep -qE "^($uid${user:+|$user}):" /etc/subuid || ! grep -qE "^($uid${user:+|$user}):" /etc/subgid; then
			echo >&2 "error: attempting to run rootless dockerd but missing necessary entries in /etc/subuid and/or /etc/subgid for $uid"
			exit 1
		fi
		: "${XDG_RUNTIME_DIR:=/run/user/$uid}"
		export XDG_RUNTIME_DIR
		if ! mkdir -p "$XDG_RUNTIME_DIR" || [ ! -w "$XDG_RUNTIME_DIR" ] || ! mkdir -p "$HOME/.local/share/docker" || [ ! -w "$HOME/.local/share/docker" ]; then
			echo >&2 "error: attempting to run rootless dockerd but need writable HOME ($HOME) and XDG_RUNTIME_DIR ($XDG_RUNTIME_DIR) for user $uid"
			exit 1
		fi
		if [ -f /proc/sys/kernel/unprivileged_userns_clone ] && unprivClone="$(cat /proc/sys/kernel/unprivileged_userns_clone)" && [ "$unprivClone" != '1' ]; then
			echo >&2 "error: attempting to run rootless dockerd but need 'kernel.unprivileged_userns_clone' (/proc/sys/kernel/unprivileged_userns_clone) set to 1"
			exit 1
		fi
		if [ -f /proc/sys/user/max_user_namespaces ] && maxUserns="$(cat /proc/sys/user/max_user_namespaces)" && [ "$maxUserns" = '0' ]; then
			echo >&2 "error: attempting to run rootless dockerd but need 'user.max_user_namespaces' (/proc/sys/user/max_user_namespaces) set to a sufficiently large value"
			exit 1
		fi
		# TODO overlay support detection?
		exec rootlesskit \
			--net="${DOCKERD_ROOTLESS_ROOTLESSKIT_NET:-vpnkit}" \
			--mtu="${DOCKERD_ROOTLESS_ROOTLESSKIT_MTU:-1500}" \
			--disable-host-loopback \
			--port-driver=builtin \
			--copy-up=/etc \
			--copy-up=/run \
			${DOCKERD_ROOTLESS_ROOTLESSKIT_FLAGS:-} \
			"$@"
	elif [ -x '/usr/local/bin/dind' ]; then
		# if we have the (mostly defunct now) Docker-in-Docker wrapper script, use it
		set -- '/usr/local/bin/dind' "$@"
	fi
else
	# if it isn't `dockerd` we're trying to run, pass it through `docker-entrypoint.sh` so it gets `DOCKER_HOST` set appropriately too
	set -- docker-entrypoint.sh "$@"
fi

exec "$@"