﻿#### Does not work:
docker buildx build --platform linux/arm64 -f ./Dockerfile -t anilsezer/portfolio ..



#### Push the image:
docker build -f ./Dockerfile -t anilsezer/portfolio:latest ..
docker login
docker push anilsezer/portfolio:latest


docker build -f ./Dockerfile -t anilsezer/portfolio ..

No cache: --no-cache
todo: Add dockerignore file, and add daily bing image to it. Also add bin and obj folders? Reduce the image size!