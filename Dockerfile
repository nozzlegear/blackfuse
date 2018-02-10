FROM microsoft/dotnet
WORKDIR /app

# Install nodejs, yarn and mono
RUN curl -sL "https://deb.nodesource.com/setup_8.x" | bash -
RUN curl -sS "https://dl.yarnpkg.com/debian/pubkey.gpg" | apt-key add -
RUN echo "deb https://dl.yarnpkg.com/debian/ stable main" | tee /etc/apt/sources.list.d/yarn.list
RUN apt-get -qq update && apt-get -qq install -y nodejs yarn mono-complete libc-bin

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
RUN cd src/client && dotnet fable yarn-build && cd -
RUN dotnet publish -c release -r linux-x64 -o ../../dist src/server/server.fsproj
RUN find . -not -iwholename './node_modules/*'

# Make the server file executable
RUN chmod +x dist/server

EXPOSE 3000
HEALTHCHECK --interval=5s CMD [ -e /tmp/.lock ] || exit 1
CMD ["./dist/server"]