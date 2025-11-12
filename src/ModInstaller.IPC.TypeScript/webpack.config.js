const webpack = require('vortex-api/bin/webpack').default;

const config = webpack('fomod-installer-ipc', __dirname, 5);

config.entry = './src/lib/index.ts';

module.exports = config;