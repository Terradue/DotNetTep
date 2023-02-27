FROM mcr.microsoft.com/dotnet/sdk:6.0

ARG JENKINS_API_TOKEN

# Install mono
RUN apt-get update && apt install -y gnupg ca-certificates
RUN gpg --keyserver hkps://keyserver.ubuntu.com --recv 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN gpg --export 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF | tee /usr/share/keyrings/mono.gpg >/dev/null
RUN gpg --batch --yes --delete-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb [signed-by=/usr/share/keyrings/mono.gpg] https://download.mono-project.com/repo/ubuntu stable-buster main" | tee /etc/apt/sources.list.d/mono-official-stable.list
RUN apt update
RUN apt install -y mono-devel

# Workaround for BoringSSL issue
# https://github.com/duplicati/duplicati/issues/4721
RUN rm -rf /usr/share/ca-certificates/mozilla/DST_Root_CA_X3.crt && \
    rm -rf /etc/ssl/certs/DST_Root_CA_X3.pem && \
    update-ca-certificates && \
    /etc/ca-certificates/update.d/mono-keystore

# RUN dotnet nuget add source https://repository.terradue.com/artifactory/api/nuget/nuget-release --name t2 --username jenkins --password $JENKINS_API_TOKEN --store-password-in-clear-text
