#!/usr/bin/env node
// Copyright (c) GitHub. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var childProcess = require("child_process");
var path = require("path");

var supported = ["linux", "darwin"];

if (supported.indexOf(process.platform) == -1) {
  console.log("Unsupported platform: " + process.platform);
  console.log("Supported platforms are: " + supported.toString());
  process.exit(1);
}

var stopping = false;
var listener = null;

var exitServiceAfterNFailures = Number(
  process.env.GITHUB_ACTIONS_SERVICE_EXIT_AFTER_N_FAILURES
);

if (exitServiceAfterNFailures <= 0) {
  exitServiceAfterNFailures = NaN;
}

var unknownFailureRetryCount = 0;
var retriableFailureRetryCount = 0;

var gracefulShutdown = function () {
  console.log("Shutting down runner listener");
  stopping = true;
  if (listener) {
    console.log("Sending SIGINT to runner listener to stop");
    listener.kill("SIGINT");

    console.log("Sending SIGKILL to runner listener");
    setTimeout(() => listener.kill("SIGKILL"), 30000).unref();
  }
};

var runService = function () {
  var listenerExePath = path.join(__dirname, "../bin/Runner.Listener");
  var interactive = process.argv[2] === "interactive";

  if (!stopping) {
    try {
      if (interactive) {
        console.log("Starting Runner listener interactively");
        listener = childProcess.spawn(listenerExePath, ["run"], {
          env: process.env,
        });
      } else {
        console.log("Starting Runner listener with startup type: service");
        listener = childProcess.spawn(
          listenerExePath,
          ["run", "--startuptype", "service"],
          { env: process.env }
        );
      }

      console.log(`Started listener process, pid: ${listener.pid}`);

      listener.stdout.on("data", (data) => {
        if (data.toString("utf8").includes("Listening for Jobs")) {
          unknownFailureRetryCount = 0;
          retriableFailureRetryCount = 0;
        }
        process.stdout.write(data.toString("utf8"));
      });

      listener.stderr.on("data", (data) => {
        process.stdout.write(data.toString("utf8"));
      });

      listener.on("error", (err) => {
        console.log(`Runner listener fail to start with error ${err.message}`);
      });

      listener.on("close", (code) => {
        console.log(`Runner listener exited with error code ${code}`);

        if (code === 0) {
          console.log(
            "Runner listener exit with 0 return code, stop the service, no retry needed."
          );
          stopping = true;
        } else if (code === 1) {
          console.log(
            "Runner listener exit with terminated error, stop the service, no retry needed."
          );
          stopping = true;
        } else if (code === 2) {
          console.log(
            "Runner listener exit with retryable error, re-launch runner in 5 seconds."
          );
          unknownFailureRetryCount = 0;
          retriableFailureRetryCount++;
          if (retriableFailureRetryCount >= 10) {
            console.error(
              "Stopping the runner after 10 consecutive re-tryable failures"
            );
            stopping = true;
          }
        } else if (code === 3 || code === 4) {
          console.log(
            "Runner listener exit because of updating, re-launch runner in 5 seconds."
          );
          unknownFailureRetryCount = 0;
          retriableFailureRetryCount++;
          if (retriableFailureRetryCount >= 10) {
            console.error(
              "Stopping the runner after 10 consecutive re-tryable failures"
            );
            stopping = true;
          }
        } else {
          var messagePrefix = "Runner listener exit with undefined return code";
          unknownFailureRetryCount++;
          retriableFailureRetryCount = 0;
          if (
            !isNaN(exitServiceAfterNFailures) &&
            unknownFailureRetryCount >= exitServiceAfterNFailures
          ) {
            console.error(
              `${messagePrefix}, exiting service after ${unknownFailureRetryCount} consecutive failures`
            );
            stopping = true
          } else {
            console.log(`${messagePrefix}, re-launch runner in 5 seconds.`);
          }
        }

        if (!stopping) {
          setTimeout(runService, 5000);
        }
      });
    } catch (ex) {
      console.log(ex);
    }
  }
};

runService();
console.log("Started running service");

process.on("SIGINT", () => {
  gracefulShutdown();
});

process.on("SIGTERM", () => {
  gracefulShutdown();
});
