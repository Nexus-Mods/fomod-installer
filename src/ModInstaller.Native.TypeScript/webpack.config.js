const webpack = require('vortex-api/bin/webpack').default;

const config = webpack('fomod-installer-native', __dirname, 5);

config.externals['./../build/modinstaller.node'] = './modinstaller.node';

module.exports = config;