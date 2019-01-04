FROM microsoft/dotnet:2.1.6-runtime-stretch-slim
MAINTAINER jake@gealer.email
EXPOSE 8080
RUN sh ./build.sh
RUN chmod 777 ./releases/linux/SuperServ
RUN cd ./releases/linux/
ENTRYPOINT ./SuperServ
