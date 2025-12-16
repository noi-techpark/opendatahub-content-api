#!/bin/sh

# Substitute environment variables in nginx config
# Only substitute CHATBOT_BACKEND_URL to avoid breaking nginx variables like $uri
envsubst '${CHATBOT_BACKEND_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# Start nginx
exec nginx -g 'daemon off;'
