# Use the official .NET SDK 8.0 image
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install additional tools
RUN apt-get update && apt-get install -y \
    git \
    curl \
    wget \
    unzip \
    vim \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js 20.x for the Angular frontend (if needed later)
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g npm@latest

# Create a non-root user
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME \
    && apt-get update \
    && apt-get install -y sudo \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

# Set the default user
USER $USERNAME

# .NET SDK is already installed in the base image at /usr/share/dotnet
# Add .NET paths to shell configs for convenience
RUN echo 'export DOTNET_ROOT=/usr/share/dotnet' >> ~/.bashrc \
    && echo 'export PATH=$PATH:/usr/share/dotnet:/usr/share/dotnet/tools' >> ~/.bashrc \
    && echo 'export DOTNET_ROOT=/usr/share/dotnet' >> ~/.zshrc \
    && echo 'export PATH=$PATH:/usr/share/dotnet:/usr/share/dotnet/tools' >> ~/.zshrc

# Set working directory
WORKDIR /workspace
