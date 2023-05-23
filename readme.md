# OutSystems.FileServer.Api

This is the README file for the API. It provides information on how to run the API locally and other relevant details.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 7 or higher)
- [Docker](https://www.docker.com/get-started) (if running with Docker)

## Getting Started

### Running Locally as Console Application

1. Clone the repository: `git clone https://github.com/os-adv-dev/OutSystems.FileServer.git`
2. Navigate to the project directory
3. Restore the dependencies: `dotnet restore`
4. Build the project: `dotnet build`
5. Run the API: `dotnet run`
6. The API will be available at `Take a look at the launchSettings.json for details`
7. Add  `/swagger` for documentation.

### Running with IIS Express

1. Clone the repository: `git clone https://github.com/os-adv-dev/OutSystems.FileServer.git`
2. Open the solution file in Visual Studio
3. Build the solution (press F6 or go to Build > Build Solution)
4. Set the startup project to the API project
5. Press F5 to start debugging
6. The API will be available at `Take a look at the launchSettings.json for details`

### Running with Docker

1. Clone the repository: `git clone https://github.com/os-adv-dev/OutSystems.FileServer.git`
2. Navigate to the project directory
3. Build the Docker image: `docker build -t outsystems-fileserver-api`
4. Run the Docker container: `docker run -d -p 8080:80 outsystems-fileserver-api`
5. The API will be available at `http://localhost:8080`

## Publishing (IIS)

1. Navigate to the Api directory using terminal.
2. Use the `dotnet publish -c release` command to publish.
3. Avoid to copy the `appsettings.json` file and copy the rest of the content from `PATH TO THE API\Api\bin\release\net7.0\publish\`
4. Paste on the destination folder configured in IIS. If the site is running maybe you need to stop first to replace the files.
5. If it's necessary, change the site `appsettings.json` file. 
6. Start the site and navigate to the swagger to test and make sure it's working.

## API Documentation

The API documentation is generated using Swagger. Once the API is running, you can access the Swagger UI at `http://localhost:<port>/swagger` to explore the available endpoints and interact with them.

## Configuration

The API configuration can be customized by modifying the `appsettings.json` file. Update the relevant settings as per your requirements.

## License

This project is licensed under the [MIT License](LICENSE).