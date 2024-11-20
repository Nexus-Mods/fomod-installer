const path = require('path');
const fs = require('fs-extra');

const relevantAssemblies = [
  'fomodinstaller.interface.dll', 'antlrutil.dll', 'modinstaller.dll', 'utils.dll', 'modscript.dll'
];

const recursiveRead = (directory) => {
  const entries = fs.readdirSync(directory);
  return entries.reduce((accum, entry) => {
    const fullPath = path.join(directory, entry);
    if (fs.statSync(fullPath).isDirectory()) {
      return accum.concat(recursiveRead(fullPath));
    } else if (relevantAssemblies.includes(entry.toLowerCase())) {
      accum.push(fullPath);
    }
    return accum;
  }, []);
};

// Main function to find assemblies
const findAssemblies = () => {
  try {
    const assemblies = recursiveRead(__dirname); // Call the recursive function
    return assemblies;
  } catch (error) {
    console.error('Error reading assemblies:', error);
    return [];
  }
};

module.exports = {
  __esModule: true,
  findAssemblies,
};
