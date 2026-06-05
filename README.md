# .NET database API

## Task
The project is a simple .NET API to demonstrate communication with an SQL database. 
The demo includes Swagger for API testing, JWT for authentication and authorization, and EF for database communication.

The demo simulates managing the inventory of a library with tables for authors, books, book copies, loans and customers.

## Running the project
Set the connection string in LibraryApi/appsettings.json
The program can then be launched from the terminal using:
```
cd LibraryApi/
dotnet run
```

Swagger can then be opened on:
http://localhost:5000
(or the URL shown in the terminal output)


## JWT Authentication
The demo includes JWT authentication.
Use the login endpoint to obtain a JWT and a refresh token.
Log in with the JWT using the Swagger UI.
The longer living refresh token can be used to obtain a new JWT in the refresh endpoint.

Three dummy accounts and three protected endpoints are present to test the authorization:

| Username | Password | 
| -------- | -------- | 
| admin    | adm      | 
| authors  | aut      | 
| books    | boo      | 

| Endpoint		| Allowed roles		|
| --------		| -------------		|
| GET /books	| admin, books		|
| GET /authors	| admin, authors	|
| GET /loans	| admin				|


## Project structure
<b>LibraryApi/</b>  API app itself with endpoints, JWT and Swagger
<b>LibraryData/</b> DTOs, models, database access logic



