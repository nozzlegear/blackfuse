FROM microsoft/dotnet
WORKDIR /app

# Install nodejs, yarn and mono
RUN curl -sL "https://deb.nodesource.com/setup_8.x" | bash -
RUN curl -sS "https://dl.yarnpkg.com/debian/pubkey.gpg" | apt-key add -
RUN echo "deb https://dl.yarnpkg.com/debian/ stable main" | tee /etc/apt/sources.list.d/yarn.list
RUN apt-get -qq update && apt-get -qq install -y nodejs yarn mono-complete

# Restore yarn packages first  to take advantage of cache
COPY package.json .
COPY yarn.lock .
RUN yarn install --production=false

# Restore dotnet next
COPY paket.lock .
COPY paket.dependencies .
COPY .paket ./.paket
COPY Nuget.Config .
COPY *.sln .
COPY src/client/*.fsproj src/client/
COPY src/client/paket.references src/client/
COPY src/server/*.fsproj src/server/
COPY src/server/paket.references src/server/

RUN dotnet restore

# Copy everything else and build
COPY . .
RUN find . -not -iwholename './node_modules/*'
RUN cd src/client && dotnet fable yarn-build && cd -

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