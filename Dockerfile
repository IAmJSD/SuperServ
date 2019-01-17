FROM microsoft/dotnet:2.1.502-sdk-stretch
EXPOSE 8080
WORKDIR /var/superserv
RUN cd /var/superserv
COPY . .
RUN tr -d '\r' < build.sh > build.fix.sh
RUN rm build.sh
RUN mv build.fix.sh build.sh
RUN sh ./build.sh
ENTRYPOINT cd ./releases/linux/ && ./SuperServ
