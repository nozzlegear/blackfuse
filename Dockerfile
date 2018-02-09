FROM node 
WORKDIR /app

# COPY package.json paket.dependencies ./
COPY package.json ./

RUN "npm i -g yarn"
RUN "yarn install"

COPY src/client/ ./

RUN "yarn build"

# FROM microsoft/dotnet
# WORKDIR /app

# # copy project and restore as distinct layers
# COPY .paket/ *.sln paket.lock paket.dependencies src/*/paket.references src/client/*.fsproj src/server/*.fsproj ./

# RUN "./.paket/paket.bootstrapper.exe"
# RUN "./.paket/paket.exe restore"
# RUN "dotnet restore"

# # copy everything else and build
# COPY . ./
# RUN dotnet publish -c release -o dist

# HEALTHCHECK --interval=5s CMD [ -e /tmp/.lock ] || exit 1

# CMD ["./dist/server.dll"]