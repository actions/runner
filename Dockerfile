FROM debian:buster

ENV GITHUB_PAT ""
ENV GITHUB_OWNER ""
ENV GITHUB_REPOSITORY ""
ENV RUNNER_WORKDIR "_work"
ENV RUNNER_LABELS ""
ENV ADDITIONAL_PACKAGES ""
ENV DOCKER_VERSION "20.10.6"
ENV DOCKER_HOST ""

RUN apt-get update \
    && apt-get install -y \
        curl \
        sudo \
        git \
        jq \
        unzip \
        gnupg2 \
        gcc \
        g++ \
        make \
        iputils-ping \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && useradd -m github \
    && usermod -aG sudo github \
    && echo "%sudo ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers \
    && curl https://download.docker.com/linux/static/stable/x86_64/docker-${DOCKER_VERSION}.tgz --output docker-${DOCKER_VERSION}.tgz \
    && tar xvfz docker-${DOCKER_VERSION}.tgz \
    && cp docker/* /usr/bin/

# Install azure-cli
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Install Ruby, Rake, Node and Yarn for Idean build tooling
RUN command curl -sSL https://rvm.io/mpapis.asc | gpg2 --import - \
&& echo "DONE: mpapis key" \
&& command curl -sSL https://rvm.io/pkuczynski.asc | gpg2 --import - \
&& echo "DONE: pkuczynski key" \
&& groupadd -f rvm \
&& echo "DONE: rvm group add" \
&& usermod -a -G rvm github \
&& echo "DONE: add github user to rvm group" \
&& usermod -a -G rvm root \
&& echo "DONE: add root user to rvm group" \
&& curl -sSL https://get.rvm.io | bash -s stable --ruby \
&& echo "DONE: download rvm" \
&& source /usr/local/rvm/scripts/rvm \
&& echo "DONE: install rvm" \
&& rvm get stable --autolibs=enable \
&& rvm install ruby-2.6 \
&& rvm --default use ruby-2.6 \
&& curl -sL https://deb.nodesource.com/setup_14.x | bash - \
&& curl -sL https://dl.yarnpkg.com/debian/pubkey.gpg | sudo apt-key add - \
&& echo "deb https://dl.yarnpkg.com/debian/ stable main" | sudo tee /etc/apt/sources.list.d/yarn.list \
&& apt-get update \
&& apt-get install -y \
    nodejs \
    yarn \
&& gem update --system \
&& echo "gem: --no-document" >> ~/.gemrc \
&& gem install rails -v 6.0.2 \
&& gem install rake -v 11

USER github
WORKDIR /home/github

RUN GITHUB_RUNNER_VERSION=$(curl --silent "https://api.github.com/repos/adaptivelab/runner/releases/latest" | jq -r '.tag_name[1:]') \
    && curl -Ls https://github.com/adaptivelab/runner/releases/download/v${GITHUB_RUNNER_VERSION}/actions-runner-linux-x64-${GITHUB_RUNNER_VERSION}.tar.gz | tar xz \
    && sudo ./bin/installdependencies.sh

COPY --chown=github:github entrypoint.sh runsvc.sh ./
RUN sudo chmod u+x ./entrypoint.sh ./runsvc.sh

ENTRYPOINT ["/home/github/entrypoint.sh"]
