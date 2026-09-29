#!/usr/bin/env bash
# Posts a summary of the current GitHub Actions event to a Discord webhook.
#
# Env (set by GitHub Actions): GITHUB_EVENT_NAME, GITHUB_EVENT_PATH, GITHUB_REPOSITORY
# Env (set by the workflow):   DISCORD_WEBHOOK_URL, from the repository secret
# Env (optional):              DRY_RUN=1 prints the payload instead of sending it
set -euo pipefail

if [[ -z "${DISCORD_WEBHOOK_URL:-}" && "${DRY_RUN:-}" != 1 ]]; then
  echo "::notice::DISCORD_WEBHOOK_URL is not set; skipping Discord notification."
  exit 0
fi

# Everything user-controlled (commit messages, titles, bodies) is read from the
# event JSON by jq, never interpolated into the shell.
payload=$(jq -c \
  --arg event "$GITHUB_EVENT_NAME" \
  --arg repo "$GITHUB_REPOSITORY" \
  --arg now "$(date -u +%Y-%m-%dT%H:%M:%SZ)" '
  def trunc($n): if length > $n then .[0:$n - 1] + "…" else . end;
  def firstline: split("\n")[0];

  # Embed colors (decimal RGB).
  def green: 3066993;
  def red: 15158332;
  def purple: 10181046;
  def blue: 3447003;
  def gray: 9807270;

  . as $e
  | (if $event == "push" then
       ($e.ref | sub("^refs/(heads|tags)/"; "")) as $ref
       | if $e.deleted then
           {title: "[\($repo)] Deleted \($ref)", url: $e.repository.html_url, color: red}
         elif ($e.commits | length) == 0 then
           {title: "[\($repo)] Created \($ref)", url: $e.compare, color: green}
         else
           ($e.commits | length) as $n
           | {
               title: "[\($repo):\($ref)] \($n) new commit\(if $n == 1 then "" else "s" end)",
               url: $e.compare,
               color: blue,
               description: (
                 $e.commits[-10:]
                 | map("[`\(.id[0:7])`](\(.url)) \(.message | firstline | trunc(100)) - \(.author.username // .author.name)")
                 | join("\n")
                 | trunc(4096)
               )
             }
         end
     elif $event == "pull_request" then
       $e.pull_request as $pr
       | (if $e.action == "closed" then (if $pr.merged then "merged" else "closed" end)
          elif $e.action == "ready_for_review" then "ready for review"
          else $e.action end) as $verb
       | {
           title: "[\($repo)] Pull request \($verb): #\($pr.number) \($pr.title)",
           url: $pr.html_url,
           color: ({merged: purple, closed: red}[$verb] // green),
           description: (if $e.action == "closed" then "" else ($pr.body // "" | trunc(1000)) end)
         }
     elif $event == "issues" then
       $e.issue as $i
       | {
           title: "[\($repo)] Issue \($e.action): #\($i.number) \($i.title)",
           url: $i.html_url,
           color: (if $e.action != "closed" then green
                   elif $i.state_reason == "not_planned" then gray
                   else purple end),
           description: (if $e.action == "closed" then "" else ($i.body // "" | trunc(1000)) end)
         }
     elif $event == "release" then
       {
         title: "[\($repo)] New release: \($e.release.name // "" | if . == "" then $e.release.tag_name else . end)",
         url: $e.release.html_url,
         color: blue,
         description: ($e.release.body // "" | trunc(1000))
       }
     elif $event == "workflow_dispatch" then
       {
         title: "[\($repo)] Test notification",
         url: $e.repository.html_url,
         color: blue,
         description: ($e.inputs.message // "" | if . == "" then "Discord notifications are working." else . end | trunc(4096))
       }
     else empty end)
  | .title |= trunc(256)
  | if .description == "" then del(.description) else . end
  | . + {
      timestamp: $now,
      author: {name: $e.sender.login, url: $e.sender.html_url, icon_url: $e.sender.avatar_url}
    }
  | {embeds: [.], allowed_mentions: {parse: []}}
' "$GITHUB_EVENT_PATH")

if [[ -z "$payload" ]]; then
  echo "No Discord notification for event '$GITHUB_EVENT_NAME'."
  exit 0
fi

if [[ "${DRY_RUN:-}" == 1 ]]; then
  echo "$payload"
  exit 0
fi

curl -fsS --retry 3 -H "Content-Type: application/json" -d "$payload" "$DISCORD_WEBHOOK_URL"
echo "Sent Discord notification for '$GITHUB_EVENT_NAME'."
