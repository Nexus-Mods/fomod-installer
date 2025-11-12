const webpack = require('vortex-api/bin/webpack').default;

const config = webpack('fomod-installer-native', __dirname, 5);

config.entry = './src/lib/index.ts';

config.externals['./../../build/modinstaller.node'] = './modinstaller.node';

module.exports = config;


/*
const path = require('path');

module.exports = {
  mode: 'production',
  entry: './src/lib/index.ts',

  output: {
    path: path.resolve(__dirname, 'dist', 'webpack'),
    filename: 'index.js',
    library: {
      type: 'commonjs2'
    },
    // Clean the output directory before emit
    clean: true
  },

  resolve: {
    extensions: ['.ts', '.js']
  },

  module: {
    rules: [
      {
        test: /\.ts$/,
        use: {
          loader: 'ts-loader',
          options: {
            configFile: 'tsconfig.webpack.json'
          }
        },
        exclude: /node_modules/
      }
    ]
  },

  // External dependencies that should not be bundled
  externals: {
    './../../modinstaller.node': 'commonjs2 ../../modinstaller.node'
  },

  target: 'node',

  // Optimization settings
  optimization: {
    minimize: true,
    usedExports: true,
    sideEffects: false
  },

  // Generate source maps for debugging
  devtool: 'source-map',

  stats: {
    warnings: true,
    errors: true
  }
};
*/