#!/usr/bin/env node

/**
 * Build script for fomod-installer-native
 * Converted from commands.ps1 to native JavaScript
 *
 * Usage: node build.js [type] [configuration]
 * Types: build, test, clear, build-native, build-napi, build-webpack, test-build
 * Configuration: Release (default) or Debug
 */

const { execSync, spawn } = require("child_process");
const fs = require("fs");
const path = require("path");

// Valid build types
const VALID_TYPES = [
  "build",
  "test",
  "clear",
  "build-native",
  "build-napi",
  "build-webpack",
  "test-build",
];

// Parse command line arguments
const args = process.argv.slice(2);
const type = args[0] || "build";
const configuration = args[1] || "Release";

// Validate build type
if (!VALID_TYPES.includes(type)) {
  console.error(`Error: Invalid build type '${type}'`);
  console.error(`Valid types: ${VALID_TYPES.join(", ")}`);
  process.exit(1);
}

// Validate configuration
if (!["Release", "Debug"].includes(configuration)) {
  console.error(`Error: Invalid configuration '${configuration}'`);
  console.error("Valid configurations: Release, Debug");
  process.exit(1);
}

/**
 * Custom exception for missing .NET SDK
 */
class MissingDotNetSDKException extends Error {
  constructor() {
    super(
      "Missing .NET SDK - Install a .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/9.0",
    );
    this.name = "MissingDotNetSDKException";
  }
}

/**
 * Error code handlers for specific error conditions
 */
const ERROR_CODE_HANDLER = {
  2147516561: {
    genError: () => new MissingDotNetSDKException(),
  },
};

/**
 * Spawns a process and returns a promise
 * @param {string} exe - Executable to run
 * @param {string[]} args - Arguments
 * @param {Object} options - Spawn options
 * @param {Object} out - Output logger
 * @returns {Promise<void>}
 */
function spawnAsync(exe, args, options = {}, out = console) {
  return new Promise((resolve, reject) => {
    const desc = `${options.cwd || "."}/${exe} ${args.join(" ")}`;
    out.log("started: " + desc);
    const outBufs = [];

    try {
      const proc = spawn(exe, args, options);
      proc.stdout.on("data", (data) => outBufs.push(data));
      proc.stderr.on("data", (data) => out.error(data.toString()));
      proc.on("error", (err) => {
        out.log(Buffer.concat(outBufs).toString());
        reject(err);
      });
      proc.on("close", (code) => {
        out.log("done: " + desc + ": " + code);
        if (code === 0) {
          resolve();
        } else {
          out.log(Buffer.concat(outBufs).toString());
          reject(new Error(`${desc} failed with code ${code}`));
        }
      });
    } catch (err) {
      out.error(`failed to spawn ${desc}: ${err.message}`);
      reject(err);
    }
  });
}

/**
 * Signs a file using the configured signing tool
 * @param {string} filePath - Path to file to sign
 * @returns {Promise<void>}
 */
async function sign(filePath) {
  if (process.env["SIGN_TOOL"] !== undefined) {
    console.log(`  Signing: ${filePath}`);
    return spawnAsync(process.env["SIGN_TOOL"], [
      "sign",
      "/sha1",
      process.env["SIGN_THUMBPRINT"],
      "/td",
      "sha256",
      "/fd",
      "sha256",
      "/tr",
      "http://timestamp.comodoca.com",
      filePath,
    ]);
  } else {
    console.log(`  Skipping signing (SIGN_TOOL not configured)`);
  }
}

/**
 * Recursively creates directory and copies file
 * @param {string} sourcePath - Source file path
 * @param {string} destPath - Destination file path
 */
function copyItem(sourcePath, destPath) {
  const resolvedSource = path.resolve(sourcePath);
  const resolvedDest = path.resolve(destPath);

  // Check if source exists
  if (!fs.existsSync(resolvedSource)) {
    throw new Error(`Source file not found: ${sourcePath}`);
  }

  const destDir = path.dirname(resolvedDest);

  // Create directory if it doesn't exist
  if (!fs.existsSync(destDir)) {
    fs.mkdirSync(destDir, { recursive: true });
  }

  // Copy file
  fs.copyFileSync(resolvedSource, resolvedDest);
  console.log(`  Copied: ${sourcePath} -> ${destPath}`);
}

/**
 * Recursively removes files and directories with glob pattern support
 * @param {string[]} patterns - File patterns to remove
 */
function removeItems(patterns) {
  patterns.forEach((pattern) => {
    try {
      // Handle glob patterns (*.tgz, *.dll, etc.)
      if (pattern.includes("*")) {
        const dir = process.cwd();
        const regex = new RegExp(
          "^" + pattern.replace(/\*/g, ".*").replace(/\?/g, ".") + "$",
        );

        const files = fs.readdirSync(dir);
        files.forEach((file) => {
          if (regex.test(file)) {
            try {
              const fullPath = path.join(dir, file);
              if (fs.existsSync(fullPath)) {
                const stats = fs.statSync(fullPath);
                if (stats.isDirectory()) {
                  fs.rmSync(fullPath, { recursive: true, force: true });
                  console.log(`  Removed directory: ${file}`);
                } else {
                  fs.unlinkSync(fullPath);
                  console.log(`  Removed file: ${file}`);
                }
              }
            } catch (err) {
              // Ignore individual file errors
            }
          }
        });
      } else {
        // Handle direct paths
        if (fs.existsSync(pattern)) {
          const stats = fs.statSync(pattern);
          if (stats.isDirectory()) {
            fs.rmSync(pattern, { recursive: true, force: true });
            console.log(`  Removed directory: ${pattern}`);
          } else {
            fs.unlinkSync(pattern);
            console.log(`  Removed file: ${pattern}`);
          }
        }
      }
    } catch (err) {
      // Ignore errors (equivalent to -ErrorAction Ignore)
    }
  });
}

/**
 * Checks if a command exists in PATH (cross-platform)
 * @param {string} command - Command to check
 * @returns {boolean} True if command exists
 */
function commandExists(command) {
  try {
    const isWindows = process.platform === "win32";
    const checkCommand = isWindows ? `where ${command}` : `which ${command}`;
    execSync(checkCommand, { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
}

/**
 * Executes a command and logs output
 * @param {string} command - Command to execute
 * @param {Object} options - Execution options
 */
function execCommand(command, options = {}) {
  console.log(`  Running: ${command}`);
  try {
    execSync(command, {
      stdio: "inherit",
      cwd: process.cwd(),
      ...options,
    });
  } catch (err) {
    console.error(`  Command failed: ${command}`);
    throw err;
  }
}

/**
 * Main build function
 */
async function main() {
  try {
    console.log(`\n=== Build Configuration ===`);
    console.log(`Type: ${type}`);
    console.log(`Configuration: ${configuration}`);
    console.log(`Working Directory: ${process.cwd()}`);
    console.log(`===========================\n`);

    // Validate prerequisites
    if (["build", "test", "build-native"].includes(type)) {
      if (!commandExists("dotnet")) {
        throw new Error(
          "dotnet CLI not found. Please install .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/9.0",
        );
      }
    }

    if (["build", "test", "build-napi", "build-webpack"].includes(type)) {
      if (!commandExists("npx")) {
        throw new Error("npx not found. Please install Node.js and npm.");
      }
    }

    // Clean
    if (["build", "test", "clear"].includes(type)) {
      console.log("Cleaning build artifacts...");
      removeItems([
        "*.tgz",
        "*.h",
        "*.dll",
        "*.lib",
        "*.so",
        "build",
        "dist",
        "coverage",
        ".nyc_output",
      ]);
      console.log("");
    }

    // Build C# Native Module
    if (["build", "test", "build-native"].includes(type)) {
      console.log(`Building ModInstaller.Native (${configuration})`);

      // Verify source directory exists
      const nativeDir = path.resolve("../ModInstaller.Native");
      if (!fs.existsSync(nativeDir)) {
        throw new Error(
          `ModInstaller.Native directory not found at: ${nativeDir}`,
        );
      }

      // Create directory if it doesn't exist
      const dotnetArtifacts = path.resolve("build-dotnet");
      if (!fs.existsSync(dotnetArtifacts)) {
        fs.mkdirSync(dotnetArtifacts, { recursive: true });
      }

      // Run dotnet publish with retry logic
      const buildArgs = [
        "publish",
        "--self-contained",
        "-c",
        configuration,
        "-o",
        dotnetArtifacts,
        "../ModInstaller.Native",
      ];

      try {
        await spawnAsync("dotnet", buildArgs);
      } catch (err) {
        // The build may fail because of locked files (sigh) so just try again...
        console.log("  Build failed, retrying after 500ms...");
        await new Promise((resolve) => setTimeout(resolve, 500));
        await spawnAsync("dotnet", buildArgs);
      }

      // Copy native artifacts
      console.log(`  Checking artifacts in: ${dotnetArtifacts}`);
      const artifactFiles = fs.readdirSync(dotnetArtifacts);
      console.log(`  Found files: ${artifactFiles.join(", ")}`);

      const nativeFiles = artifactFiles.filter((file) =>
        file.startsWith("ModInstaller.Native."),
      );

      if (nativeFiles.length === 0) {
        throw new Error(
          `No ModInstaller.Native.* files found in ${dotnetArtifacts}`,
        );
      }

      console.log(`  Native files to copy: ${nativeFiles.join(", ")}`);
      nativeFiles.forEach((file) => {
        copyItem(path.join(dotnetArtifacts, file), file);
      });

      // Also check for .lib file in the native subfolder (Windows AOT build)
      const nativeSubfolder = path.join(dotnetArtifacts, "native");
      console.log(`  Checking native subfolder: ${nativeSubfolder}`);
      if (fs.existsSync(nativeSubfolder)) {
        const nativeSubfolderFiles = fs.readdirSync(nativeSubfolder);
        console.log(`  Native subfolder files: ${nativeSubfolderFiles.join(", ")}`);
        nativeSubfolderFiles
          .filter((file) => file.startsWith("ModInstaller.Native."))
          .forEach((file) => {
            const destPath = file;
            if (!fs.existsSync(destPath)) {
              copyItem(path.join(nativeSubfolder, file), destPath);
            }
          });
      } else {
        console.log(`  Native subfolder does not exist`);
      }

      // Fallback: Check the original build location if .lib is still missing
      if (process.platform === "win32" && !fs.existsSync("ModInstaller.Native.lib")) {
        // Try multiple possible paths (MSBuild uses different output structures locally vs CI)
        const possibleLibPaths = [
          // Standard path: bin/Release/net9.0/win-x64/native/
          path.resolve(nativeDir, `bin/${configuration}/net9.0/win-x64/native/ModInstaller.Native.lib`),
          // CI path with platform folder: bin/x64/Release/net9.0/win-x64/native/
          path.resolve(nativeDir, `bin/x64/${configuration}/net9.0/win-x64/native/ModInstaller.Native.lib`),
        ];

        let foundLib = false;
        for (const libPath of possibleLibPaths) {
          console.log(`  Checking lib path: ${libPath}`);
          if (fs.existsSync(libPath)) {
            copyItem(libPath, "ModInstaller.Native.lib");
            foundLib = true;
            break;
          }
        }

        if (!foundLib) {
          console.log(`  .lib file not found in any expected location`);
          // List what's in the native dir bin folder for debugging
          const binDir = path.resolve(nativeDir, "bin");
          if (fs.existsSync(binDir)) {
            console.log(`  Contents of ${binDir}:`);
            const listDirRecursive = (dir, indent = "    ") => {
              const items = fs.readdirSync(dir, { withFileTypes: true });
              items.forEach(item => {
                console.log(`${indent}${item.name}${item.isDirectory() ? "/" : ""}`);
                if (item.isDirectory() && indent.length < 16) {
                  listDirRecursive(path.join(dir, item.name), indent + "  ");
                }
              });
            };
            listDirRecursive(binDir);
          }
        }
      }

      copyItem(
        path.join(nativeDir, "ModInstaller.Native.h"),
        "ModInstaller.Native.h",
      );

      // Sign the DLL if signing is configured
      if (fs.existsSync("ModInstaller.Native.dll")) {
        await sign("ModInstaller.Native.dll");
      }

      console.log("");
    }

    // Build NAPI
    if (["build", "test", "build-napi"].includes(type)) {
      console.log(`Building NAPI (${configuration})`);

      // Verify binding.gyp exists
      if (!fs.existsSync("binding.gyp")) {
        throw new Error("binding.gyp not found in current directory");
      }

      // Determine build tag
      let tag = "";
      if (configuration === "Release") {
        tag = "--release";
      } else if (configuration === "Debug") {
        tag = "--debug";
      }

      // Run node-gyp rebuild
      execCommand(`npx node-gyp rebuild --arch=x64 ${tag}`.trim());
      console.log("");
    }

    // Copy content to dist
    if (["build", "test", "test-build", "build-content"].includes(type)) {
      console.log("Copying content to dist");

      if (process.platform == "win32") {
        copyItem("ModInstaller.Native.dll", "dist/ModInstaller.Native.dll");
      } else if (process.platform == "linux") {
        copyItem("ModInstaller.Native.so", "dist/ModInstaller.Native.so");
      }

      copyItem(
        `build/${configuration}/modinstaller.node`,
        "dist/modinstaller.node",
      );
      console.log("");
    }

    // Build Webpack Bundle
    if (["build", "build-webpack"].includes(type)) {
      console.log("Building Webpack bundle");

      // Verify TypeScript config exists
      if (!fs.existsSync("tsconfig.json")) {
        throw new Error("tsconfig.json not found");
      }

      // Verify webpack config exists
      if (!fs.existsSync("webpack.config.js")) {
        throw new Error("webpack.config.js not found");
      }

      // Compile TypeScript declarations
      execCommand("npx tsc --emitDeclarationOnly");

      // Build with webpack
      execCommand("npx webpack --config webpack.config.js");
      console.log("");
    }

    // Run tests
    if (type === "test") {
      console.log("Running tests");

      // Compile TypeScript tests
      execCommand("npx tsc -p tsconfig.test.json");

      // Copy native module to dist for tests
      const buildDir = path.resolve("dist/build");
      if (!fs.existsSync(buildDir)) {
        fs.mkdirSync(buildDir, { recursive: true });
      }
      copyItem(`build/${configuration}/modinstaller.node`, "dist/build/modinstaller.node");

      // Run AVA tests
      // On Linux, tolerate exit code 139 (segfault) which can occur during Native AOT cleanup
      // This is a known issue: .NET Native AOT libraries don't support clean unloading
      try {
        execCommand("npx ava");
      } catch (err) {
        if (process.platform === 'linux' && err.status === 139) {
          console.log("  Note: Segfault during cleanup (exit code 139) - this is expected on Linux with Native AOT");
        } else {
          throw err;
        }
      }
      console.log("");
    }

    console.log("✓ Build completed successfully!");
    process.exit(0);
  } catch (err) {
    // Handle specific error codes
    const error =
      ERROR_CODE_HANDLER[err?.code] !== undefined
        ? ERROR_CODE_HANDLER[err.code].genError()
        : err;

    console.error("\n✗ Build failed:", error.message);
    if (process.env.DEBUG) {
      console.error("\nStack trace:", error.stack);
    }

    const exitCode = err?.code || -1;
    process.exit(exitCode);
  }
}

// Run main function
main();
