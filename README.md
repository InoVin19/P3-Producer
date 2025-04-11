# P3-Producer Application

A .NET 9.0 console application that implements the producer side of a producer-consumer video upload system.

## Key Features
- Multiple producer threads (configurable)
- Dedicated TCP connections to consumers
- Leaky bucket queue implementation
- Docker container support
- Configurable via `config.txt`

## Configuration
Create a `config.txt` file with these parameters:
p=
c=
q=

## Docker configuration
Make sure that you have Docker and Docker Compose installed on your machine

Run the following command:
docker-compose up
