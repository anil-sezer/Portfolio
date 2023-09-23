#### Does not work:
docker buildx build --platform linux/arm64 -f ./Dockerfile -t anilsezer/portfolio ..



#### Push the image:
docker build -f ./deployment/Dockerfile -t anilsezer/portfolio Portfolio.WebUi/.
docker login
docker push anilsezer/portfolio:latest


docker build -f ./Dockerfile -t anilsezer/portfolio ..

No cache: --no-cache
todo: Add dockerignore file, and add daily bing image to it. Also add bin and obj folders? Reduce the image size!


### From Root:
docker build -f ./deployment/Dockerfile -t anilsezer/portfolio Portfolio.WebUi/.
docker run -p 8080:80 anilsezer/portfolio
