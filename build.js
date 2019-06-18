var msbuildLib = require('msbuild');
var path = require('path');

function build() {
  var msbuild = new msbuildLib();
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

restore(() => build());

