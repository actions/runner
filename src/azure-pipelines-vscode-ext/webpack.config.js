const path = require('path');
const webpack = require('webpack');

/** @typedef {import('webpack').Configuration} WebpackConfig **/
/** @type WebpackConfig */
const webExtensionConfig = {
  mode: 'none', // this leaves the source code as close as possible to the original (when packaging we set this to 'production')
  target: 'webworker', // extensions run in a webworker context
  entry: {
    main: './index.js', // source of the web extension main file
  },
  output: {
    filename: '[name].js',
    path: path.join(__dirname, './dist'),
    libraryTarget: 'commonjs',
  },
  resolve: {
    mainFields: ['browser', 'module', 'main'], // look for `browser` entry point in imported node modules
    extensions: ['.ts', '.js'], // support ts-files and js-files
    alias: {
      // provides alternate implementation for node module and source files
    },
    fallback: {
      // Webpack 5 no longer polyfills Node.js core modules automatically.
      // see https://webpack.js.org/configuration/resolve/#resolvefallback
      // for the list of Node.js core module polyfills.
      assert: require.resolve('assert'),
      process: false,
      module: false,
    }
  },
  module: {
    rules: [
      {
        test: /_framework/,
        exclude: /node_modules/,
        use: {
          loader: path.resolve('loader.mjs'),
          options: {
          },
        }
      },
      {
        test: /\.ts$/,
        exclude: /node_modules/,
        use: [
          {
            loader: 'ts-loader'
          }
        ]
      }
    ]
  },
  plugins: [
    new webpack.ProvidePlugin({
      process: 'process/browser' // provide a shim for the global `process` variable
    })
  ],
  externals: {
    vscode: 'commonjs vscode' // ignored because it doesn't exist
  },
  performance: {
    hints: false
  },
};

/** @typedef {import('webpack').Configuration} WebpackConfig **/
/** @type WebpackConfig */
const nodeExtensionConfig = {
  mode: 'none', // this leaves the source code as close as possible to the original (when packaging we set this to 'production')
  target: 'node', // extensions run in a webworker context
  entry: {
    main: './index.js', // source of the web extension main file
  },
  output: {
    filename: '[name]-node.js',
    path: path.join(__dirname, './dist'),
    libraryTarget: 'commonjs',
  },
  resolve: {
    mainFields: ['main'], // look for `browser` entry point in imported node modules
    extensions: ['.ts', '.js'], // support ts-files and js-files
  },
  // experiments: {
  //   outputModule: true
  // },
  plugins: [
    new webpack.IgnorePlugin({ resourceRegExp: /\.wasm$/ })
  ],
  module: {
    rules: [
      {
        test: /_framework/,
        exclude: /node_modules/,
        use: {
          loader: path.resolve('loader.mjs'),
          options: {
          },
        },
      },
      {
        test: /\.ts$/,
        exclude: /node_modules/,
        use: [
          {
            loader: 'ts-loader'
          }
        ]
      }
    ]
  },
  externals: {
    vscode: 'commonjs vscode' // ignored because it doesn't exist
  },
  performance: {
    hints: false
  },
  devtool: "source-map"
};

module.exports = [ webExtensionConfig,  nodeExtensionConfig];