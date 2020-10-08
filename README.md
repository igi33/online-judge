# Online Judge

## Project Idea
A simple online judge platform which grades algorithmic task solutions automatically.

## Features
- Solve algorithmic problems in given time and memory limits
- Task and test case creation
- Support for task solutions in multiple programming languages
- Tags for tasks
- User profiles showing solved tasks

## Server Requirements
The operating system requirement for the server is Linux because of its nifty features. I have used Debian-based Ubuntu 18.04 for testing but it's probably possible to use other Linux distributions as well.

#### Server configuration
1. Install cgroup tools for manipulation of cgroups (cgcreate, cgset, cgget, cgexec, cgdelete)
```console
$ sudo apt install cgroup-tools
```
2. Add commands in the **/etc/sudoers** file to be usable as sudo without a password (required for the system to be autonomous). Execute the following command and insert the below two lines at the end of the file:
```console
$ sudo visudo /etc/sudoers
```
```console
yourLinuxUsername ALL = (root) NOPASSWD: /usr/bin/cgcreate,/usr/bin/cgset,/usr/bin/cgexec,/usr/bin/cgdelete
yourLinuxUsername ALL = (root) NOPASSWD: /usr/bin/timeout
```
3. Make a directory **executionroot** in your Linux user's home directory (~) to serve as a chroot jail from which the task solutions will be executed
4. Copy **/bin/bash** and its dependenciess (using ldd and cp) into **~/executionroot/** with these commands:
```console
$ chr=~/executionroot
```
```console
$ cp -v --parents /bin/bash "${chr}"
```
```console
$ list="$(ldd /bin/bash | egrep -o '/lib.*\.[0-9]')"
```
```console
for i in $list; do cp -v --parents "$i" "${chr}"; done
```

## Frameworks and Requirements
#### Back End: ASP.NET Core - A C# Web API Framework by Microsoft
ASP.NET Core Web API version used: 2.2\
Authentication handled via JSON Web Tokens (JWT)

Versions used:

- .NET Core 2.2 SDK
- MySQL 8.0.11

#### Front End: Angular - A Typescript Front End Web Framework by Google
[An Angular front end web app for an online judge REST API](https://github.com/igi33/online-judge-angular-app)

#### Front End: WPF - A C# Front End Framework for Windows by Microsoft
.NET Framework version used: 4.7.2

## Running the Application
#### Database
- Run the two SQL scripts from the **/database-scripts/ directory** to create the project MySQL database schema and to populate it with initial data
- The project back end assumes the following MySQL connection pairs: localhost for the server, root for the username and no password. Change the connection string in the **/OnlineJudgeApi/OnlineJudgeApi/appsettings.json file**

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
