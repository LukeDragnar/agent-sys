
# Define nginx for front-end server
FROM nginx:1.15.8-alpine

RUN rm -rf /usr/share/nginx/html/*
COPY ./dist/frontend/browser /usr/share/nginx/html
COPY /nginx.conf  /etc/nginx/conf.d/default.conf

EXPOSE 80

# CMD ["nginx", "-g", "daemon off;"]