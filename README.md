# ritual-reversal
## Discord notifications

`.github/workflows/discord-notify.yml` posts repository activity to a Discord
channel: pushes, pull requests (opened, reopened, ready for review, merged,
closed), issues (opened, reopened, closed), and published releases.

### Setup

1. In Discord, open **Server Settings → Integrations → Webhooks**, create a
   webhook for the target channel, and copy its URL.
2. In GitHub, open **Settings → Secrets and variables → Actions** and add a
   repository secret named `DISCORD_WEBHOOK_URL` with that URL.

Never commit the webhook URL: anyone who has it can post to the channel. If it
leaks, delete the webhook in Discord and create a new one.

If the secret is not set, the workflow skips sending and still succeeds. Pull
requests from forks never receive secrets, so they don't send notifications.

To send a test message, run the workflow manually from the **Actions** tab.
To preview a payload locally without sending it:

```sh
GITHUB_EVENT_NAME=push GITHUB_EVENT_PATH=event.json GITHUB_REPOSITORY=owner/repo \
  DRY_RUN=1 .github/scripts/discord-notify.sh
```
