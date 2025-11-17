const webpack = require('vortex-api/bin/webpack').default;

const config = webpack('fomod-installer-ipc', __dirname, 5);

module.exports = config;