cd OdhDiscovery/infrastructure
  DOCKER_IMAGE_WEBAPP=odh-discovery-webapp DOCKER_IMAGE_BACKEND=odh-discovery-backend \
  DOCKER_TAG=test AUTH_USERNAME=admin AUTH_PASSWORD=test123 \
  JWT_SECRET_KEY=your-secret-key LLM_API_KEY=dummy \
  docker compose -f docker-compose.run.yml up