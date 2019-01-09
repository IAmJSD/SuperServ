FROM microsoft/dotnet:2.1.502-sdk-stretch
MAINTAINER jake@gealer.email
EXPOSE 8080
WORKDIR /var/superserv
RUN cd /var/superserv
COPY . .
RUN sh ./remove_slash_r.sh
RUN sh ./build.sh
RUN cd ./releases/linux/
ENTRYPOINT ./SuperServ
