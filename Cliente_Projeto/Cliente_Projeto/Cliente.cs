using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Cliente
{
    static void Main(string[] args)
    {
        // Define o ip do servidor e o número da porta
        string serverIP = "127.0.0.1";
        int serverPort = 8888;

        // Cria o objeto cliente TCP e conecta ao servidor
        TcpClient client = new TcpClient(serverIP, serverPort);

        // Recebe a network stream para enviar e receber dados
        NetworkStream stream = client.GetStream();

        while (true)
        {
            // Lê a mensagem de input da consola
            Console.Write("Write a message to the server or 'CSV' file path to send a file \nor 'send' to recive the file or 'quit' to close connection:\n");
            string message = Console.ReadLine();

            if (message.ToUpper() == "QUIT")
            {
                // Converte a mensagem para bytes e envia ao servidor
                byte[] quitMessageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(quitMessageBytes, 0, quitMessageBytes.Length);
                Console.WriteLine("400 BYE");
                // Sai do loop se a mensagem for 'quit'
                break;
            }

            if (message.EndsWith(".csv") && !File.Exists(message))
            {
                Console.WriteLine("ERROR: File not found. Please try again");
                continue;

            }

            if (message.EndsWith(".csv"))
            {
                //Read file content and send to server
                string fileContent = File.ReadAllText(message);

                // Envia uma mensagem de confirmação ao cliente
                Console.WriteLine("File saved successfully!");

                byte[] fileBytes = Encoding.ASCII.GetBytes(fileContent);
                stream.Write(fileBytes, 0, fileBytes.Length);

                //Mostra a mensagem de confirmação do servidor
                byte[] clientBuffer = new byte[1024];
                int clientByteCount = stream.Read(clientBuffer, 0, clientBuffer.Length);
                string clientMessage = Encoding.ASCII.GetString(clientBuffer, 0, clientByteCount);
                Console.WriteLine(clientMessage);
            }            

            if (message.ToUpper() == "SEND")
                {
                    // Converte a mensagem para bytes e envia ao servidor
                    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);

                    // Cria um buffer para armazenar os bytes recebidos
                    byte[] buffer = new byte[1024];

                    // Cria um FileStream para armazenar o arquivo recebido
                    using (FileStream fileStream = new FileStream("FicheiroCSV.csv", FileMode.Create))
                    {
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            fileStream.Flush();
                        }
                    }
                // Recebe a mensagem de confirmação do servidor
                byte[] confirmacaobuffer = new byte[1024];
                int confirmacaobytesRead = stream.Read(buffer, 0, buffer.Length);
                string confirmacaoMessage = Encoding.ASCII.GetString(buffer, 0, confirmacaobytesRead);
                Console.WriteLine(confirmacaoMessage);
            }
            
            //Para envios de mensagens sem ser ficheiros
            else
            {
                // Converte a mensagem para bytes e envia ao servidor
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);

                // Recebe a mensagem de confirmação do servidor
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string serverMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Mostra a mensagem de confirmação do servidor
                Console.WriteLine("-Server acknowledge-");
            }
        }

        // Fecha a conexão e o cliente
        stream.Close();
        client.Close();
    }
}