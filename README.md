# LeakTestService

The LeakTestService is the service responsible for handling CRUD operations related to test results. It is part of a microservices application including [GatewayService](https://github.com/olavlinddam/GatewayService) and [TestObjectService](https://github.com/olavlinddam/TestObjectService). The services are containerized and set up as a Docker Swarm to ensure stability and high availability. They communicate via RabbitMQ which makes the application services highly decoupled.

This service was initially developed as part of a larger school project in collaboration with [Nolek](https://nolek.dk/). The project's objective was to create a prototype application that companies could use to store and retrieve data related to leak tests on various objects.


## High level system design of the application
![image](https://github.com/olavlinddam/LeakTestService/assets/110632249/1d7d8b52-6003-41d7-8fd5-e8697e909d16)


## High level swarm overview
![image](https://github.com/olavlinddam/GatewayService/assets/110632249/36c749d7-0a10-4892-8aba-4525c1dfedc2)
