#!/bin/sh

# Substitute environment variables in nginx config
# Only substitute specific vars to avoid breaking nginx variables like $uri
envsubst '${CHATBOT_BACKEND_URL} ${CHATBOT_BACKEND_HOST}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# Start nginx
exec nginx -g 'daemon off;'
