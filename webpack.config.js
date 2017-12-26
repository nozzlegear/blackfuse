const fs = require('fs');
const path = require('path');
const webpack = require('webpack');
const fableUtils = require("fable-utils");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

const isProduction = process.env["NODE_ENV"] === "production";
const babelOptions = fableUtils.resolveBabelOptions({
    presets: [
        ["env", {
            "targets": {
                "browsers": ["last 2 versions"]
            },
            "modules": false
        }]
    ],
    plugins: ["transform-runtime"]
});

/**
 * Takes a list of plugins and only returns those that are not undefined. Useful when you only pass plugins in certain conditions.
 */
function filterPlugins(plugins) {
    return (plugins || []).filter(plugin => !!plugin);
}

module.exports = {
    devtool: "source-map",
    entry: resolve("./src/client/client.fsproj"),
    output: {
        filename: "client.js",
        path: resolve("./src/client/public/js"),
        // Important: publicPath must begin with a / but must not end with one. Else hot module replacement won't find updates.
        publicPath: "/public/js",
    },
    devServer: {
        proxy: {
            // Forward *all* requests except those for this bundle to the server
            '**': {
                target: 'http://localhost:3000',
                changeOrigin: true
            }
        },
        hot: true,
        inline: true,
        port: 8000
    },
    plugins: filterPlugins([
        new webpack.DefinePlugin({
            "NODE_ENV": `"${process.env.NODE_ENV}"` || `"development"`,
            // Process.env is necessary for bundling React in production
            "process.env": {
                "NODE_ENV": `"${process.env.NODE_ENV}"` || `"development"`,
            }
        }),
        isProduction ? undefined : new webpack.NoEmitOnErrorsPlugin(),
        isProduction ? undefined : new webpack.HotModuleReplacementPlugin(),
    ]),
    resolve: {
        // Add `.ts` and `.tsx` as a resolvable extension.
        extensions: ['.ts', '.tsx', '.js', '.styl', '.stylus'],
        modules: [resolve("./node_modules/")]
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: "fable-loader",
                    options: {
                        babel: babelOptions,
                        define: isProduction ? [] : ["DEBUG"]
                    }
                }
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: babelOptions
                },
            },
            {
                test: /\.styl[us]?$/,
                use: ["style-loader", "css-loader", "autoprefixer-loader?{browsers:['last 2 version', 'ie >= 11']}", "stylus-loader"]
            }
        ],
    },
}