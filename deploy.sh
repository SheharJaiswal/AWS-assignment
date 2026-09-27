#!/usr/bin/env bash

set -euo pipefail

# ============================================================
# AWS CONFIGURATION
# ============================================================

AWS_REGION="us-east-1"
AWS_ACCOUNT_ID="509425091939"

ECR_REPOSITORY="insurance-document-portal"
IMAGE_TAG="latest"

# Existing EC2
EC2_HOST="44.204.224.238"
EC2_USER="ubuntu"

# SSH key inside WSL
SSH_KEY="$HOME/.ssh/InsuranceDocumentPortalKey.pem"

# Docker container
CONTAINER_NAME="insurance-document-portal"

# IMPORTANT:
# EC2 port 80 -> container port 8080
# Change CONTAINER_PORT if your Dockerfile/application uses another port.
HOST_PORT="80"
CONTAINER_PORT="8080"

ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPOSITORY}"


# ============================================================
# FUNCTIONS
# ============================================================

fail() {
    echo
    echo "ERROR: $1"
    exit 1
}

step() {
    echo
    echo "============================================================"
    echo "$1"
    echo "============================================================"
}


# ============================================================
# 1. CHECK REQUIRED COMMANDS
# ============================================================

step "1. Checking prerequisites"

command -v docker >/dev/null 2>&1 \
    || fail "Docker command is not available in WSL."

command -v aws >/dev/null 2>&1 \
    || fail "AWS CLI is not installed."

command -v ssh >/dev/null 2>&1 \
    || fail "SSH is not available."

[[ -f "$SSH_KEY" ]] \
    || fail "SSH key not found: $SSH_KEY"

echo "Docker: $(docker --version)"
echo "AWS CLI: $(aws --version 2>&1)"
echo "SSH: available"

echo
echo "Checking Docker daemon..."

docker info >/dev/null 2>&1 \
    || fail "Docker daemon is not running. Start Docker Desktop."

echo "Docker daemon: OK"


# ============================================================
# 2. CHECK AWS AUTHENTICATION
# ============================================================

step "2. Checking AWS authentication"

aws sts get-caller-identity \
    --region "$AWS_REGION"

echo
echo "AWS authentication: OK"


# ============================================================
# 3. CHECK DOCKERFILE
# ============================================================

step "3. Checking Dockerfile"

[[ -f "Dockerfile" ]] \
    || fail "Dockerfile was not found in: $(pwd)"

echo "Dockerfile found."


# ============================================================
# 4. CHECK / CREATE ECR REPOSITORY
# ============================================================

step "4. Checking ECR repository"

if aws ecr describe-repositories \
    --repository-names "$ECR_REPOSITORY" \
    --region "$AWS_REGION" >/dev/null 2>&1
then
    echo "ECR repository already exists:"
    echo "$ECR_REPOSITORY"
else
    echo "ECR repository does not exist."
    echo "Creating repository..."

    aws ecr create-repository \
        --repository-name "$ECR_REPOSITORY" \
        --region "$AWS_REGION"

    echo "ECR repository created."
fi


# ============================================================
# 5. LOGIN TO ECR
# ============================================================

step "5. Logging Docker into ECR"

aws ecr get-login-password \
    --region "$AWS_REGION" |
docker login \
    --username AWS \
    --password-stdin "$ECR_URI"

echo "ECR login successful."


# ============================================================
# 6. BUILD DOCKER IMAGE
# ============================================================

step "6. Building Docker image"

echo "Image:"
echo "${ECR_REPOSITORY}:${IMAGE_TAG}"

docker build \
    --tag "${ECR_REPOSITORY}:${IMAGE_TAG}" \
    .

echo
echo "Docker build successful."


# ============================================================
# 7. TAG IMAGE
# ============================================================

step "7. Tagging Docker image"

docker tag \
    "${ECR_REPOSITORY}:${IMAGE_TAG}" \
    "${ECR_URI}:${IMAGE_TAG}"

echo "Image:"
echo "${ECR_URI}:${IMAGE_TAG}"


# ============================================================
# 8. PUSH IMAGE TO ECR
# ============================================================

step "8. Pushing image to ECR"

docker push \
    "${ECR_URI}:${IMAGE_TAG}"

echo
echo "Image pushed successfully."


# ============================================================
# 9. DEPLOY TO EC2
# ============================================================

step "9. Deploying to EC2"

echo "EC2:"
echo "${EC2_USER}@${EC2_HOST}"

echo
echo "Connecting to EC2..."

ssh \
    -i "$SSH_KEY" \
    -o StrictHostKeyChecking=no \
    "${EC2_USER}@${EC2_HOST}" \
    "AWS_REGION='$AWS_REGION' \
     ECR_URI='$ECR_URI' \
     IMAGE_TAG='$IMAGE_TAG' \
     CONTAINER_NAME='$CONTAINER_NAME' \
     HOST_PORT='$HOST_PORT' \
     CONTAINER_PORT='$CONTAINER_PORT' \
     bash -s" <<'REMOTE_SCRIPT'

set -euo pipefail

echo
echo "------------------------------------------------------------"
echo "EC2: Checking Docker"
echo "------------------------------------------------------------"

if ! command -v docker >/dev/null 2>&1
then
    echo "Docker is not installed."
    echo "Installing Docker..."

    sudo apt-get update
    sudo apt-get install -y docker.io

    sudo systemctl enable docker
    sudo systemctl start docker
else
    echo "Docker already installed."
fi

sudo systemctl start docker

echo
sudo docker --version


echo
echo "------------------------------------------------------------"
echo "EC2: Checking IAM Role"
echo "------------------------------------------------------------"

sudo apt install awscli -y

aws sts get-caller-identity


echo
echo "------------------------------------------------------------"
echo "EC2: Logging into ECR"
echo "------------------------------------------------------------"

aws ecr get-login-password \
    --region "$AWS_REGION" |
sudo docker login \
    --username AWS \
    --password-stdin "$ECR_URI"


echo
echo "------------------------------------------------------------"
echo "EC2: Pulling latest image"
echo "------------------------------------------------------------"

sudo docker pull \
    "$ECR_URI:$IMAGE_TAG"


echo
echo "------------------------------------------------------------"
echo "EC2: Stopping old container"
echo "------------------------------------------------------------"

sudo docker stop \
    "$CONTAINER_NAME" \
    2>/dev/null || true

sudo docker rm \
    "$CONTAINER_NAME" \
    2>/dev/null || true


echo
echo "------------------------------------------------------------"
echo "EC2: Starting new container"
echo "------------------------------------------------------------"

sudo docker run \
    --detach \
    --name "$CONTAINER_NAME" \
    --restart unless-stopped \
    --publish "$HOST_PORT:$CONTAINER_PORT" \
    "$ECR_URI:$IMAGE_TAG"


echo
echo "------------------------------------------------------------"
echo "EC2: Container status"
echo "------------------------------------------------------------"

sudo docker ps \
    --filter "name=$CONTAINER_NAME"


echo
echo "------------------------------------------------------------"
echo "EC2: Container logs"
echo "------------------------------------------------------------"

sudo docker logs \
    --tail 50 \
    "$CONTAINER_NAME"


echo
echo "EC2 deployment completed."

REMOTE_SCRIPT


# ============================================================
# 10. SUCCESS
# ============================================================

step "DEPLOYMENT SUCCESSFUL"

echo "Application:"
echo "http://${EC2_HOST}"

echo
echo "ECR image:"
echo "${ECR_URI}:${IMAGE_TAG}"

echo
echo "Container:"
echo "$CONTAINER_NAME"

echo
echo "Done."