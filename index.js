//const cp = require('child_process');
const path = require('path');
const fs = require('fs-extra');
const dotnet = require('dotnet');

const initAssemblyResolver = async () => {
  dotnet.addListener('resolving', (name, version, resolve) => {
    // Lets try to keep this event listener as light, static and synchronous
    //  as possible for the sake of our future selves.
    const recursiveRead = (directory) => {
      const files = fs.readdirSync(directory)
        .map(file => path.join(directory, file))
        .filter(file => fs.statSync(file).isFile() && path.extname(file) === '.dll');
    
      return files.concat(
        ...files.map(file => recursiveRead(file))
          .flatMap(files => files)
      );
    }
    const assemblies = recursiveRead(__dirname);
    const potentialMatch = assemblies.find(file => path.basename(file).toLowerCase() === name.toLowerCase());
    if (potentialMatch) {
      resolve(potentialMatch)
    }
  });
};

module.exports = {
  __esModule: true,
  initAssemblyResolver,
};
