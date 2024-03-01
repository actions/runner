FROM debian:buster

ENV GITHUB_PAT ""
ENV GITHUB_OWNER ""
ENV GITHUB_REPOSITORY ""
ENV RUNNER_WORKDIR "_work"
ENV RUNNER_LABELS ""
ENV ADDITIONAL_PACKAGES ""
ENV DOCKER_VERSION "20.10.6"
ENV DOCKER_HOST ""
ENV YQ_VERSION "v4.9.3"
ENV YQ_BINARY "yq_linux_amd64"

RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive \
      apt-get install -y \
        curl \
        sudo \
        git \
        jq \
        wget \
        unzip \
        gnupg2 \
        gcc \
        g++ \
        make \
        procps \
        dirmngr \
        git-core \
        zlib1g-dev \
        build-essential \
        libssl-dev \
        libreadline-dev \
        libyaml-dev \
        libsqlite3-dev \
        sqlite3 \
        libxml2-dev \
        libxslt1-dev \
        libcurl4-openssl-dev \
        software-properties-common \
        libffi-dev \
        iputils-ping \
        apt-utils \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && useradd -m github \
    && usermod -aG sudo github \
    && echo "%sudo ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers \
    && curl https://download.docker.com/linux/static/stable/x86_64/docker-${DOCKER_VERSION}.tgz --output docker-${DOCKER_VERSION}.tgz \
    && tar xvfz docker-${DOCKER_VERSION}.tgz \
    && cp docker/* /usr/bin/

USER github
WORKDIR /home/github

ENV GEM_HOME="/home/github/bundle"
ENV PATH $GEM_HOME/bin:$GEM_HOME/gems/bin:$PATH

# Install azure-cli
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Install Ruby, Rake, Node and Yarn for Idean build tooling
RUN curl -sL https://deb.nodesource.com/setup_14.x | sudo -E bash - \
&& curl -sS https://dl.yarnpkg.com/debian/pubkey.gpg | sudo apt-key add - \
&& echo "deb https://dl.yarnpkg.com/debian/ stable main" | sudo tee /etc/apt/sources.list.d/yarn.list \
&& sudo apt-get update \
&& sudo apt-get upgrade -y \
&& DEBIAN_FRONTEND=noninteractive \
  sudo apt-get install -y \
    nodejs \
    yarn=1.22.21 \
&& sudo apt-get clean \
&& sudo rm -rf /var/lib/apt/lists/* \
&& sudo wget https://github.com/mikefarah/yq/releases/download/${YQ_VERSION}/${YQ_BINARY} -O /usr/bin/yq \
&& sudo chown github:github /usr/bin/yq \
&& sudo chmod 755 /usr/bin/yq \
&& git clone https://github.com/rbenv/rbenv.git ~/.rbenv \
&& echo 'export PATH="$HOME/.rbenv/bin:$PATH"' >> ~/.bashrc \
&& export PATH="$HOME/.rbenv/bin:$PATH" \
&& echo 'eval "$(rbenv init -)"' >> ~/.bashrc \
&& eval "$(rbenv init -)" \
&& exec $SHELL \
&& git clone https://github.com/rbenv/ruby-build.git ~/.rbenv/plugins/ruby-build \
&& echo 'export PATH="$HOME/.rbenv/plugins/ruby-build/bin:$PATH"' >> ~/.bashrc \
&& export PATH="$HOME/.rbenv/plugins/ruby-build/bin:$PATH" \
&& exec $SHELL \
&& rbenv install 2.7.0 \
&& rbenv global 2.7.0 \
&& ruby -v \
&& echo "gem: --no-document" >> ~/.gemrc \
&& gem update --system \
&& gem install \
    bundler \
    rails \
    rake

RUN GITHUB_RUNNER_VERSION=$(curl --silent "https://api.github.com/repos/adaptivelab/runner/releases/latest" | jq -r '.tag_name[1:]') \
    && curl -Ls https://github.com/adaptivelab/runner/releases/download/v${GITHUB_RUNNER_VERSION}/actions-runner-linux-x64-${GITHUB_RUNNER_VERSION}.tar.gz | tar xz \
    && sudo ./bin/installdependencies.sh

COPY --chown=github:github entrypoint.sh runsvc.sh ./
RUN sudo chmod u+x ./entrypoint.sh ./runsvc.sh

ENTRYPOINT ["/home/github/entrypoint.sh"]
