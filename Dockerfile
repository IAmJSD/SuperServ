FROM microsoft/dotnet:2.1.502-sdk-stretch
MAINTAINER jake@gealer.email
EXPOSE 8080
COPY . .
RUN sh ./build.sh
RUN chmod 777 ./releases/linux/SuperServ
RUN cd ./releases/linux/
ENTRYPOINT ./SuperServ
