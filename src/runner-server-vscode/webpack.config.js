const path = require('path');

/** @typedef {import('webpack').Configuration} WebpackConfig **/
/** @type WebpackConfig */
const nodeExtensionConfig = {
  mode: 'none', // this leaves the source code as close as possible to the original (when packaging we set this to 'production')
  target: 'node', // extensions run in a webworker context
  entry: {
    main: './index.ts', // source of the web extension main file
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
  module: {
    rules: [
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

module.exports = [ nodeExtensionConfig ];