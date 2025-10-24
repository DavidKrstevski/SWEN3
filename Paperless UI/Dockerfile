# build React app
FROM node:18 AS build
WORKDIR /app
COPY paperless-react/package*.json ./
RUN npm install
COPY paperless-react/ .
RUN npm run build

# serve using nginx
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
