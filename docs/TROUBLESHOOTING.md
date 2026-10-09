# Troubleshooting

## No drive is offered

The app requires an internal NTFS drive. USB, network, and removable drives are
excluded. Windows drive identification must also be available. An SSD is preferred.

## Anomaly or GAMMA already contains files

The app refuses to overwrite a nonempty folder unless it created that folder
previously. Choose another supported drive, or move an existing installation
yourself after backing up saves. Removing an ownership marker will make an existing
managed folder look unmanaged. This build has no import option.

## Not enough space

A fresh install requires about 200 GB free. Retry estimates remaining space from
managed folder sizes and still requires working room. A cached download does not
mean extraction is finished. Do not delete unrelated files to satisfy an error.

## Install stopped or a download failed

Open the log and read the last error. Try Retry when the provider is available.
Complete cached archives are reused, while a partial archive may restart. A bot
challenge or unavailable mirror may need attention from the upstream installer.

## Windows permission was cancelled

Click Install again and complete the Windows prompt. The worker needs elevated
access for the drive-root folders and the selected system components/settings.

## Microsoft signature verification failed

The app will not execute an unverified Microsoft download. It retries with a fresh
copy. If it still fails, check the log and Windows certificate/network health.
Do not replace the installer with an arbitrary third-party executable.

## Antivirus blocks installation or MO2

Read the security software's own detection details and the app log. There is an
optional, explicit Defender exclusion checkbox for the game folders. The app never
disables antivirus. Third-party security software is not configured by the app.

## Play stays unavailable

An install must finish successfully and pass the local profile/file checks. Follow
the displayed message. Restart Windows if requested after installing Microsoft
components. Close the game and MO2 before Repair / update; this reapplies the official
profile and may reset custom mod selections and executable entries.

## Play starts MO2 but the game fails

This first build has not completed an in-game test. Include the MO2/game error and
the app's log excerpt in a bug report. Do not use the raw Anomaly executable to test
whether GAMMA's virtual mod filesystem is active.

Logs: `%LOCALAPPDATA%\GammaEasy\installation.log`. The backend also writes logs in
`%APPDATA%\stalker-gamma`, under the administrator account used for installation.

[Report a problem](https://github.com/Nachos0/gamma-easy/issues/new/choose)
