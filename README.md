# Online Judge
*Programming Language Theory, PMF Nis: CS*

## Project Idea
A simple online judge platform which grades program solutions automatically.

## Features
- Task and test case creation
- Support for multiple programming languages
- Tags for tasks
- User profiles showing solved tasks

## Frameworks and Requirements
#### Back End: ASP.NET Core - C# Web API Framework by Microsoft
ASP.NET Core Web API version used: 2.2\
Authentication handled via JSON Web Tokens (JWT)

Requirements:

- .NET Core 2.2 SDK
- MySQL 8.0.11

#### Front End: WPF - C# Front End Framework for Windows by Microsoft
.NET Framework version used: 4.7.2

## Running the Application
#### Database
- Run the two SQL scripts from the **/database-scripts/ directory** to create the project MySQL database schema and to populate it with initial data
- The project back end assumes the following MySQL connection pairs: localhost for the server, root for the username and root for the password. Change this if needed in the **/OnlineJudgeApi/OnlineJudgeApi/appsettings.json file**
- The given table data row for C++ compilation in the ComputerLanguages table assumes the following location of the compiler: **C:/mingw-w64/x86_64-8.1.0-posix-seh-rt_v6-rev0/mingw64/bin**. Change this entry to where your compiler is located!

#### Back End
To serve the API locally run the following command from the **/OnlineJudgeApi/OnlineJudgeApi/ directory**:
```console
dotnet run
```
#### Front End
1. Build the OnlineJudgeWpfApp project
2. Run the project from Visual Studio, or find the executable in the **/OnlineJudgeWpfApp/OnlineJudgeWpfApp/bin/Debug/ directory**

## Useful Links
* [JSON Web Tokens](https://jwt.io/) - Authorization tokens for a RESTful API
