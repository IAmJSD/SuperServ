FROM microsoft/dotnet:2.1.502-sdk-stretch
EXPOSE 8080
WORKDIR /var/superserv
RUN cd /var/superserv
COPY . .
RUN /usr/bin/python2.7 ./build.py
ENTRYPOINT cd ./releases/linux/ && ./SuperServ
