# Helix ALM Webhook Tester

## Overview
A Node.js backend application that receives webhook notifications from Helix ALM and automatically creates Perforce (P4) jobs from those events. It logs payloads to console and file, and enriches each event with a direct item URL.

## Architecture
- **Runtime**: Node.js 20 + .NET 8.0
- **Framework**: Express (HTTPS)
- **Entry point**: `app.js`
- **Port**: 3000 (HTTPS)
- **No frontend** — pure backend webhook receiver + P4 job creator

## Project Structure
```
app.js                          # Main entry point, creates and starts WebhookReceiver
ReceiverClass/
  WebhookReceiverClass.js       # Class managing the child server process
  WebhookReceiver.js            # Express HTTPS server; handles POST requests and spawns P4JobCreator
  cert.pem / key.pem            # Self-signed TLS certificate
P4JobCreator/
  Program.cs                    # C# console app: reads webhook JSON from stdin, creates P4 job
  P4JobCreator.csproj
  publish/                      # Compiled C# binary (auto-built on npm start)
sharedSecretKey.txt             # Optional HMAC shared secret for signature verification
```

## How It Works
1. Helix ALM sends a POST webhook to `https://<host>:3000/`
2. `WebhookReceiver.js` receives the payload and:
   - Adds `httpurl` field to each event's item (constructed from static project ID + item number)
   - Logs to console and/or `receivedWebhooks.txt`
   - Verifies HMAC signature if a key is configured
   - Spawns `P4JobCreator` (C# executable) with the payload piped to stdin
3. `P4JobCreator` (C#):
   - Parses the JSON payload
   - Connects to P4 server via CLI (`ssl:localhost:1666`)
   - Creates a P4 job with mapped fields

## P4 Job Field Mapping
| P4 Job Field | Webhook Source         |
|--------------|------------------------|
| Description  | Full payload + summary |
| ISSUE_ID     | `events[].item.tag`    |
| ISSUE_URL    | `events[].item.httpurl`|

## P4 Configuration
Defaults (can be overridden with environment variables):
- `P4PORT`: `ssl:localhost:1666`
- `P4USER`: `jeniq`
- `P4PASSWD`: `Password`
- `P4CLIENT`: `jeniq_JQUESTA0725_5856`

## Helix ALM URL Config
Static values in `ReceiverClass/WebhookReceiver.js`:
- `HALM_BASE_URL`: `http://jquesta0725/ttweb/index.html#Default`
- `HALM_PROJECT_ID`: `65`

## Webhook Signature Verification
To enable, edit `sharedSecretKey.txt`:
```
sha256:<your-secret-key>
```

## Workflow
- **Start application**: `npm start` — builds C# project then runs Node.js app on port 3000

## Deployment
- Target: VM (always-running)
- Run command: `node app.js`
