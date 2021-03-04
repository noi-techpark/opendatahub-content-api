pipeline {
    agent any

    environment {
	ASPNETCORE_ENVIRONMENT = "Development"
        DOCKER_PROJECT_NAME = "odh-tourism-api"
        DOCKER_IMAGE = '755952719952.dkr.ecr.eu-west-1.amazonaws.com/odh-tourism-api'
        DOCKER_TAG = "test-$BUILD_NUMBER"
	SERVER_PORT = "1011"        
        PG_CONNECTION = credentials('odh-tourism-api-test2-pg-connection')
	MSS_USER = credentials('odh-tourism-api-test-mss-user')
	MSS_PSWD = credentials('odh-tourism-api-test-mss-pswd')
	LCS_USER = credentials('odh-tourism-api-test-lcs-user')
	LCS_PSWD = credentials('odh-tourism-api-test-lcs-pswd')
	LCS_MSGPSWD = credentials('odh-tourism-api-test-lcs-msgpswd')
	SIAG_USER = credentials('odh-tourism-api-test-siag-user')
	SIAG_PSWD = credentials('odh-tourism-api-test-siag-pswd')
	XMLDIR = credentials('odh-tourism-api-test-xmldir')
	IMG_URL = "https://images.tourism.testingmachine.eu/api/Image/GetImage?imageurl="
	S3_BUCKET_ACCESSPOINT = credentials('odh-tourism-api-test-bucket-accesspoint')
	S3_IMAGEUPLOADER_ACCESSKEY = credentials('odh-tourism-api-test-s3-imageuploader-accesskey')
	S3_IMAGEUPLOADER_SECRETKEY = credentials('odh-tourism-api-test-s3-imageuploader-secretkey')
	OAUTH_AUTORITY = "https://auth.opendatahub.testingmachine.eu/auth/realms/noi/"
	ELK_URL = credentials('odh-tourism-api-test-elk-url')
	ELK_TOKEN = credentials('odh-tourism-api-test-elk-token')
	JSONPATH = "/wwwroot/json/"
    }

    stages {
        stage('Configure') {
            steps {
                sh """
                    rm -f .env
                    cp .env.example .env
                    echo 'COMPOSE_PROJECT_NAME=${DOCKER_PROJECT_NAME}' >> .env
                    echo 'DOCKER_IMAGE=${DOCKER_IMAGE}' >> .env
                    echo 'DOCKER_TAG=${DOCKER_TAG}' >> .env
                    echo 'SERVER_PORT=${SERVER_PORT}' >> .env         
		    echo 'ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}' >> .env         
                    echo 'PG_CONNECTION=${PG_CONNECTION}' >> .env
		    echo 'MSS_USER=${MSS_USER}' >> .env
		    echo 'MSS_PSWD=${MSS_PSWD}' >> .env
		    echo 'LCS_USER=${LCS_USER}' >> .env
		    echo 'LCS_PSWD=${LCS_PSWD}' >> .env
		    echo 'LCS_MSGPSWD=${LCS_MSGPSWD}' >> .env
		    echo 'SIAG_USER=${SIAG_USER}' >> .env
		    echo 'SIAG_PSWD=${SIAG_PSWD}' >> .env
		    echo 'XMLDIR=${XMLDIR}' >> .env
		    echo 'IMG_URL=${IMG_URL}' >> .env
		    echo 'S3_BUCKET_ACCESSPOINT=${S3_BUCKET_ACCESSPOINT}' >> .env
		    echo 'S3_IMAGEUPLOADER_ACCESSKEY=${S3_IMAGEUPLOADER_ACCESSKEY}' >> .env
		    echo 'S3_IMAGEUPLOADER_SECRETKEY=${S3_IMAGEUPLOADER_SECRETKEY}' >> .env
		    echo 'OAUTH_AUTORITY=${OAUTH_AUTORITY}' >> .env
		    echo 'ELK_URL=${ELK_URL}' >> .env
		    echo 'ELK_TOKEN=${ELK_TOKEN}' >> .env
		    echo 'JSONPATH=${JSONPATH}' >> .env
                """
            }
        }
        stage('Build') {
            steps {
                sh '''
                    aws ecr get-login --region eu-west-1 --no-include-email | bash
                    docker-compose --no-ansi -f docker-compose.yml build --pull
                    docker-compose --no-ansi -f docker-compose.yml push
                '''
            }
        }
        stage('Deploy') {
            steps {
               sshagent(['jenkins-ssh-key']) {
                    sh """
                        (cd infrastructure/ansible && ansible-galaxy install -f -r requirements.yml)
                        (cd infrastructure/ansible && ansible-playbook --limit=test deploy.yml --extra-vars "release_name=${BUILD_NUMBER}")
                    """
                }
            }
        }
    }
}
