var cp = require('child_process');
var msbuildLib = require('msbuild');
var path = require('path');

function sign() {
  if (process.env['SIGN_TOOL'] !== undefined) {
    cp.spawn(process.env['SIGN_TOOL'], ['sign', '/sha1', process.env['SIGN_THUMBPRINT'], '/t', 'http://timestamp.verisign.com/scripts/timestamp.dll', 'Build\\bin\\Release\\ModInstallerIPC.exe']);
  }
}

function build(cb) {
  var msbuild = new msbuildLib(cb);
  msbuild.sourcePath = path.join(__dirname, 'FomodInstaller.sln');

  msbuild.configuration = process.argv[2] || 'Release';
  msbuild.configuration += ';TargetFrameworkVersion=v4.5';
  msbuild.overrideParams.push('/m'); // parallel build
  msbuild.overrideParams.push('/clp:ErrorsOnly');

  msbuild.build();
}


function restore(cb) {
  var msbuild = new msbuildLib(cb);

  msbuild.configuration = process.argv[2] || 'Release';
  msbuild.configuration += ';TargetFrameworkVersion=v4.5';
  msbuild.overrideParams.push('/t:restore');
  msbuild.build();
}

restore(() => build(() => sign()));

