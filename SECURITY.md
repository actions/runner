# Security policy

## What qualifies as a security issue

Credentials leakage, outdated dependencies with known vulnerabilities, and
other issues that could lead to unprivileged or unauthorised access to the
database or the system.

## Reporting a vulnerability

The easiest way to report a security issue is through GitHub.
See [Privately reporting a security
vulnerability](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability#privately-reporting-a-security-vulnerability)
for instructions.

The repository admins will be notified of the issue and will work with you
to determine whether the issue qualifies as a security issue and, if so, in
which component. We will then handle figuring out a fix, getting a CVE
assigned and coordinating the release of the fix.

The [Ubuntu Security disclosure and embargo policy](https://ubuntu.com/security/disclosure-policy)
contains more information about what you can expect when you contact us, and what we
expect from you.

## Vulnerability scanning

The `Secscan` workflow scans the Linux runner tarballs with Canonical's secscan service
(Trivy). `Runner CD` runs it on the freshly built packages and does not create the
release if it fails; it also runs weekly against the latest release and on demand.

When it fails, maintainers download the `secscan-report-*` artifacts and either fix the
issue (usually a .NET or bundled Node.js update) or open a draft
[GitHub security advisory](../../security/advisories/new) recording the CVE, its severity
and the remediation plan. A CVE accepted as a false positive or acceptable risk is added
to `.github/secscan-exclusions/<runtime>.txt` with a justification comment and a link to
its advisory. High or critical exclusions need security team approval. The SSDLC cycle is
set by `SSDLC_CYCLE` in the workflow and must be bumped each cycle.
