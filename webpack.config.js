const fs = require('fs');
const path = require('path');
const readPkg = require('read-pkg');
const webpack = require('webpack');
const HappyPack = require("happypack");
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');

// Hack for Ubuntu on Windows: interface enumeration fails with EINVAL, so return empty.
try {
    require('os').networkInterfaces()
}
catch (e) {
    require('os').networkInterfaces = () => ({})
}

const pkg = readPkg.sync();
const production = process.env["NODE_ENV"] === "production";
const isWatching = process.argv.indexOf("--watch") > -1 || process.argv.some(a => a.indexOf("webpack-dev-server") > -1);

function buildHappyPackPlugin() {
    const base = {
        id: "ts",
        threads: 2
    }
    const query = { happyPackMode: true }
    const tsLoader = {
        path: "ts-loader",
        query: query
    }

    return new HappyPack({
        ...base,
        loaders: [
            {
                path: "babel-loader",
                exclude: /node_modules\/(?!(react-win-dialog|gearworks-http)\/).*/,
                query: {
                    babelrc: true,
                }
            },
            tsLoader
        ]
    })
}

/**
 * Takes a list of plugins and only returns those that are not undefined. Useful when you only pass plugins in certain conditions.
 */
function filterPlugins(plugins) {
    return (plugins || []).filter(plugin => !!plugin);
}

const clientConfig = {
    entry: {
        "client": ["babel-polyfill", "./src/client/client.tsx"]
    },
    output: {
        filename: "[name].js",
        path: path.join(__dirname, "src/client/public/js"),
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
        inline: true
    },
    plugins: filterPlugins([
        new ForkTsCheckerWebpackPlugin({ checkSyntacticErrors: true }),
        new webpack.DefinePlugin({
            "_VERSION": `"${pkg.version}"`,
            "NODE_ENV": `"${process.env.NODE_ENV}"` || `"development"`,
            // Process.env is necessary for bundling React in production
            "process.env": {
                "NODE_ENV": `"${process.env.NODE_ENV}"` || `"development"`,
            }
        }),
        buildHappyPackPlugin(),
        production ? undefined : new webpack.NoEmitOnErrorsPlugin(),
        production ? undefined : new webpack.HotModuleReplacementPlugin(),
    ]),
    resolve: {
        // Add `.ts` and `.tsx` as a resolvable extension.
        extensions: ['.ts', '.tsx', '.js', '.styl', '.stylus']
    },
    externals: {

    },
    module: {
        rules: [
            {
                test: /\.js$/,
                exclude: /node_modules\/(?!(react-win-dialog|gearworks-http)\/).*/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        babelrc: true
                    }
                }
            },
            {
                test: /\.tsx?$/,
                exclude: /node_modules\/(?!(react-win-dialog|gearworks-http)\/).*/,
                loader: 'happypack/loader?id=ts'
            },
            {
                test: /\.styl[us]?$/,
                use: ["style-loader", "css-loader", "autoprefixer-loader?{browsers:['last 2 version', 'ie >= 11']}", "stylus-loader"]
            }
        ],
    },
    devtool: "source-map"
}

module.exports = clientConfig