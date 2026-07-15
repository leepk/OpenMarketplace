FROM node:22-alpine AS builder

WORKDIR /app

COPY package*.json ./

RUN yarn install --ignore-engines

COPY . .

ARG VITE_API_BASE_URL
ARG VITE_SWAGGER_URL

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_SWAGGER_URL=$VITE_SWAGGER_URL

RUN yarn build

FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
