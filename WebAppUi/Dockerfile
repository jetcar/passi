FROM node:21 as vueWeb
COPY WebAppUi /app
WORKDIR /app/vue-project
RUN npm install
RUN npm run build


ENTRYPOINT ["sleep", "3600"]