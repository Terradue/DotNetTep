FROM mcr.microsoft.com/dotnet/sdk:6.0

# Install mono
RUN apt-get update && apt-get install -y gnupg ca-certificates && \
    apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    echo "deb https://download.mono-project.com/repo/ubuntu stable-buster main" | tee /etc/apt/sources.list.d/mono-official-stable.list && \
    apt-get update && \
    apt-get install -y mono-devel && \
    rm -rf /var/lib/apt/lists/*

# Workaround for BoringSSL issue
# https://github.com/duplicati/duplicati/issues/4721
RUN rm -rf /usr/share/ca-certificates/mozilla/DST_Root_CA_X3.crt && \
    rm -rf /etc/ssl/certs/DST_Root_CA_X3.pem && \
    update-ca-certificates && \
    /etc/ca-certificates/update.d/mono-keystore
