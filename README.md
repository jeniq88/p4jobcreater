![Support](https://img.shields.io/badge/Support-None-red.svg)
# Helix ALM Webhook Tester
## Overview
The Helix ALM Webhook Tester is a simple example application that is able to receive webhook notifications from Helix ALM. 

This can be used for testing purposes, or as a reference for building your own service to handle Helix ALM webhook notifications.

## Requirements
* [Helix ALM 2023.3 Server](https://www.perforce.com/downloads/helix-alm) or later

## License
[MIT License](LICENSE.txt)

# Support
This project is an example and provided as-is. 
Perforce offers no support for issues (either via GitHub nor via Perforce's standard support process).  All pull requests will be ignored.

# Build
To build, use:
`npm install`

# Usage
To run, use:
`npm run start`

## Webhook Signatures
If webhook signatures are configured, enter the shared secret key in the [key file](sharedSecretKey.txt).

The format for the key file is <verification_method>:<key_value>. Currently supported verification methods are `sha256` and `sha512`.

# Technical Documentation
As this project may be used as a reference utility for receiving Helix ALM webhook notifications, the following documentation will help explain how the app functions.

## app.js Functionality
Initially, app.js is configured only to start the service and continue to receive webhooks until the user ends the app.

This can easily be changed by including or altering any of the functions described below.

NOTE: When calling an asynchronous function from inside a loop, ensure that await is used to prevent errors.

## Webhook Receiver Class Functions
### setup
* Starts the service with the desired options
* Parameters
  * port - the port number the app will listen on for webhook notifications (default: 3000)
  * status - the status code to return to Helix ALM when it sends webhook notifications (default: 200)
  * consoleOutput - whether the service should write messages and webhook notification data to the console (default: false)
  * fileOutput - whether the service should write webhook notification data to a file (default: false)

### clearReceivedWebhooks
* Clears out any cached webhooks

### changeStatusCode
* Changes the status code that is returned to Helix ALM when receiving a webhook
* Parameters
  * status - the status code to return to the sender (default: 200)

### waitForNewWebhooks (asynchronous)
* Waits until timeout to receive a new webhook notification
* Parameters
  * timeToWait - the number of milliseconds before timing out (default: 120000)

### waitUntilNewWebhooks (asynchronous)
* Waits indefinitely to receive a new webhook notification

### stop (asynchronous)
* Stops the service