# Online Judge

## Idea
An online judge REST API which grades algorithmic task solutions automatically.

## Features
- Solve algorithmic problems in given time and memory limits
- Task and test case creation
- Support for task solutions in multiple programming languages
- Tags for tasks
- User profiles showing solved tasks

## Server Requirements
The operating system requirement for the server is Linux because of its nifty features. I have used Debian-based Ubuntu 18.04 for testing but it's probably possible to use other Linux distributions as well, with few to no changes.

#### Server Configuration
1. Install cgroup tools for manipulation of cgroups (cgcreate, cgset, cgget, cgexec, cgdelete)
```console
$ sudo apt install cgroup-tools
```
2. Create a non-privileged Linux user that will be used to run user programs (you will be prompted to select a password). We will also add him to a newly created Linux group with disabled network access. Execute the following commands:
```console
$ sudo adduser coderunner
```
```console
$ sudo addgroup no-network
```
```console
$ sudo adduser coderunner no-network
```
```console
$ sudo iptables -I OUTPUT 1 -m owner --gid-owner no-network -j DROP
```
3. Allow some commands to be usable as sudo without a password in the **/etc/sudoers** file. This is a requirement for the system to be autonomous. Because user solutions will be executed by a non-privileged Linux user, as opposed to the privileged user <YOUR_LINUX_USERNAME> who will be starting the REST API service, this change does not pose a security threat. Execute the following command and insert the below line, with the username replaced, at the end of the file:
```console
$ sudo visudo -f /etc/sudoers
```
```console
<YOUR_LINUX_USERNAME> ALL = (root) NOPASSWD: /usr/bin/cgcreate,/usr/bin/cgset,/usr/bin/cgexec,/usr/bin/cgdelete,/usr/bin/timeout
```
4. Make a directory **executionroot** in your Linux user's home directory (~) to serve as a chroot jail from which the task solutions will be executed. You can use a different directory for this, but be sure to update the reference in the project.
5. Copy **/bin/bash** and its dependencies (using ldd and cp) into **~/executionroot/** with these commands:
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
$ for i in $list; do cp -v --parents "$i" "${chr}"; done
```

#### Database Creation and Configuration
1. Run the two SQL scripts from the **/database-scripts/ directory** to create the project MySQL database schema and to populate it with initial data.
2. The project back end assumes the following MySQL connection pairs: localhost for the server, root for the username and no password. If needed, change the connection string in the **/OnlineJudgeApi/appsettings.json file**.

## Frameworks and Requirements
#### Back-end: ASP.NET Core - A C# Web API Framework by Microsoft
ASP.NET Core Web API version used: 3.1\
Authentication handled via JSON Web Tokens (JWT)

Versions used:

- .NET Core 3.1 SDK
- MySQL 8.0.11

#### Front-end: Angular - A Typescript Front End Web Framework by Google
[Link to Angular front-end web app repository](https://github.com/igi33/online-judge-angular-app)

## Running the REST API
To serve the REST API locally run the following command from the **/OnlineJudgeApi/ directory**:
```console
dotnet run
```

## Useful Links
* [JSON Web Tokens](https://jwt.io/) - Authorization tokens for a RESTful API
