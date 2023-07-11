# DistributedSystems

#First Project

This is a client/server system implemented in C# using the .NET framework libraries (System*). The purpose of this system is to provide a file coverage processing service for CSV files. It allows clients to send multiple files to the server for processing, according to the defined rules.

#Features
-Client/Server Architecture: The system follows a client/server model, where the client program communicates with the server program to send files for processing and receive the results.

-CSV File Processing: The server is capable of processing CSV files, extracting coverage data, and performing calculations or analysis based on the provided rules.

-Protocol Specification: A custom communication protocol is defined for the interaction between the client and server. This protocol outlines the message formats, commands, and responses used for file submission, processing, and result retrieval.

-Scalability: The system is designed to handle multiple file submissions concurrently, allowing efficient processing of a large number of files.

-Error Handling: The system includes robust error handling mechanisms to ensure that errors during file processing or communication are properly handled and reported to the client.



#Usage
-Start the server program on a designated machine or network.

-Run the client program on a client machine.

-Establish a connection between the client and server using the specified network configuration.

-Select the CSV files to be processed and submit them to the server for processing.

-Monitor the progress of file processing and receive the results from the server.

-Analyze and utilize the processed coverage data as required by the client application.

-Please refer to the documentation for detailed instructions on how to set up and use the File Coverage Processing System.


#Note

This system is developed as a sample project to demonstrate client/server communication and CSV file processing using C# and .NET framework libraries. It can serve as a foundation for building more complex coverage processing systems or be customized to meet specific requirements.

_______________________________________________________________________________________________________________________________________________________________________________________________________________________________
