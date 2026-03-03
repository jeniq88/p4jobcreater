/**
 * @File This contains the class for receiving webhooks.
 * @Copyright 2023 Perforce Software Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this 
 * software and associated documentation files (the "Software"), to deal in the Software 
 * without restriction, including without limitation the rights to use, copy, modify, 
 * merge, publish, distribute, sublicense, and/or sell copies of the Software, and to 
 * permit persons to whom the Software is furnished to do so, subject to the following 
 * conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies 
 * or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 */

'use strict';

// NodeJS Modules
const { fork } = require('child_process');
const { join } = require('path'); 

module.exports = class WebhookReceiver {
  /**
   * Constructs the webhook receiver
   */
  constructor() {
    this.consoleOutput = false;
    this.processFinished = false;
    this.webhooksRecieved = [];
  }

  /**
   * Sets up the server process
   * 
   * @param {number} port - the port number for the server to listen to
   * @param {number} status - the status code to return to the sender
   * @param {boolean} consoleOutput - true if output should be written to the console
   * @param {boolean} fileOutput - true if output should be written to a file
   * @returns {object} - the child process object that contains the server process
   */
  async setup(port = 3000, status = 200, consoleOutput = false, fileOutput = false) {
    this.serverProcess = fork(join(__dirname, 'WebhookReceiver.js'), [port, status, consoleOutput, fileOutput]);

    this.consoleOutput = consoleOutput;

    this.serverProcess.on('message', (message) => {
      this.webhooksRecieved.push(message);
    });

    process.on('exit', async () => {
      await this.stop();
    });

    return this.serverProcess;
  }

  /**
   * Clears the current webhooks
   */
  clearReceivedWebhooks() {
    this.serverProcess.send({ clearHooks: true });
    this.webhooksRecieved = [];
  }

  /**
   * Changes the return status code
   * 
   * @param {number} status - the status code to return to the sender
   */
  changeStatusCode(status = 200) {
    this.serverProcess.send({ changeStatus: status});
  }

  /**
   * Wait for a new webhook to be received
   * 
   * @param {number} timeToWait - the number of milliseconds before timing out
   */
  async waitForNewWebhooks(timeToWait = 120000) {
    return Promise.race([
      new Promise((resolve) => {
        this.serverProcess.once('message', resolve);
      }),
      new Promise((resolve) => {
        const wait = setTimeout(() => {
          clearTimeout(wait);
          resolve();
        }, timeToWait);
      })
    ]);
  }

  /**
   * Wait for a new webhook to be received regardless of how long
   */
  async waitUntilNewWebhooks() {
    return new Promise((resolve) => {
      this.serverProcess.once('message', resolve);
    });
  }

  /**
   * Ends the server process
   */
  async stop() {
    if (!this.processFinished) {
      this.serverProcess.kill('SIGKILL');
      if (this.consoleOutput === true) {
        console.log('Server has stopped');
      }
      this.processFinished = true;
    }
    process.exit();
  }

  /**
   * Ends the server process without killing the parent process - safe for automated tests to call.
   */
  async stopSafe() {
    if (!this.processFinished) {
      this.serverProcess.kill('SIGKILL');
      if (this.consoleOutput === true) {
        console.log('Server has stopped');
      }
      this.processFinished = true;
    }
  }
};