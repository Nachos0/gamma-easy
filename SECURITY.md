# Security reports

For a suspected vulnerability, use this repository's **Security → Report a
vulnerability** feature. Avoid posting credentials, exploitable personal paths, or
an unredacted proof of concept in a public issue. Ordinary installation bugs belong
in Issues.

This is an early-alpha community project. There is no guaranteed response time or
claim that an audit has been completed. Only the current prerelease is maintained.

Relevant boundaries include drive-root writes, ownership markers, UAC handoff,
archive extraction, signature verification, downloaded backend execution, and
optional Defender exclusions. Explain the affected version and how to reproduce
the problem without changing unrelated user data.

The launcher never disables antivirus. Microsoft runtime downloads require valid
Microsoft Authenticode signatures, and the embedded backend is SHA256-pinned.
These controls do not certify the security of every upstream game/mod file.
