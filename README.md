## File Manager Test 
The project contains two applications
    A WPF Project that allows for sending of file over a network.
    ASP.NET that reads the files that have been sent.
### Dependencies
Some of the dependencies that have been used on this project include
NetworkCommsDotNet which is a network library
Dapper; which is a micro ORM that enables linking to the database
Protobuf-net; provides for the serialization in .NET applications
### Functionality
The WPF Application verifies the users by prompting Login
The user is then supposed to provide the ip address and port to send files to
Browse for the file and the application sends
The application employs MD5 Encryption whilst verifying the users

The .NET Application requires the users to login before viewing the sent files
The functionality for this however is yet to be complete but is still a work in progress