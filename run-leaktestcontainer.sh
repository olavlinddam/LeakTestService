#!/bin/bash

# Få maskinnavnet
MACHINE_NAME=$(hostname)

# Build the images
docker build -t leaktestservice:1.0 -f ./LeakTestService/Dockerfile .

# Kør Docker containeren med den passende ASPNETCORE_ENVIRONMENT variabel
docker run -d -p 5175:80 -e ASPNETCORE_ENVIRONMENT=$MACHINE_NAME --name leaktestservice-container leaktestservice:1.0
