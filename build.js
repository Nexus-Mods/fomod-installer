const semver = require('semver');
const dotnet = require('node-api-dotnet');

class MissingDotNetSDKException extends Error {
  constructor() {
    super('Missing .NET SDK - Install a .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/6.0');
    this.name = 'MissingDotNetSDKException';
  }
}

const ERROR_CODE_HANDLER = {
  2147516561: {
    genError: () => new MissingDotNetSDKException(),
  },
}

function userHasDotNet() {
  return  dotnet?.runtimeVersion !== null && semver.satisfies(dotnet?.runtimeVersion, '>=6.0.0');
}

async function main() {
  return true;
}

main();
