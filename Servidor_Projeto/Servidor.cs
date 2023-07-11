using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Runtime.InteropServices.ComTypes;

namespace Servidor_Projeto
{
    class Ficheiro
    {
        public string OWNER { get; set; }
        public string Municipio { get; set; }
        public string Operadora { get; set; }
        public string Domicilio { get; set; }
        // public string Responsavel { get; set; }
        // public DateTime DataProcessamento { get; set; }
    }

    class Servidor
    {
        static List<TcpClient> clientes = new List<TcpClient>();
      
        static Mutex mutex = new Mutex();

        static void Main(string[] args)
        {
            // Define o IP local e port number
            string localIP = "127.0.0.1";
            int localPort = 8888;

            // Cria o objeto listener TCP e start a listening thread
            TcpListener ServerSocket = new TcpListener(IPAddress.Parse(localIP), localPort);
            ServerSocket.Start();
            Console.WriteLine("Server waiting...");

            while (true)
            {
                // Aceita cliente e cria um objeto TCP
                TcpClient client = ServerSocket.AcceptTcpClient();
                Console.WriteLine("100 OK");
                Console.WriteLine("Client {0} connected", client.Client.RemoteEndPoint);

                mutex.WaitOne();
                clientes.Add(client);
                mutex.ReleaseMutex();

                // Cria uma thread separada para lidar com o cliente
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        public static void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();


                while (true)
                {

                    // Lê os bytes enviados pelo cliente e converte em uma string
                    byte[] buffer = new byte[1024];
                    int byteCount = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, byteCount);

                    if (message.ToUpper() == "QUIT")
                    {
                        // Se o cliente enviar "QUIT", desconecta do servidor
                        Console.WriteLine("Client {0} disconnected.", client.Client.RemoteEndPoint);
                        Console.WriteLine("400 BYE");

                        // Remove o cliente da lista de clientes conectados
                        mutex.WaitOne();
                        clientes.Remove(client);
                        mutex.ReleaseMutex();

                        // Fecha a conexão com o cliente e encerra o loop
                        client.Close();
                        break;
                    }
                    if (message.StartsWith("OWNER"))
                    {
                        Console.Write("OPEN");
                        Thread.Sleep(500);
                        Console.Write("\nIN_PROGRESS");
                        Thread.Sleep(2000);
                        Console.Write("\nCOMPLETED\n");
                        Thread.Sleep(500);

                        // Separa as linhas da mensagem em um array de strings
                        string[] lines = message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        // Armazena a primeira linha como cabeçalho
                        string cabecalho = lines[0];

                        // Cria uma lista de objetos Ficheiro para armazenar as informações
                        List<Ficheiro> ficheiros = new List<Ficheiro>();

                        foreach (string line in lines)
                        {
                            // Ignora a primeira linha (cabeçalho)
                            if (line == cabecalho) continue;

                            // Separa os campos da linha em um array de strings
                            string[] fields = line.Split(';');

                            // Se a linha tiver 4 campos, cria um objeto Ficheiro e adiciona à lista
                            if (fields.Length == 4)
                            {
                                Ficheiro ficheiro = new Ficheiro
                                {
                                    OWNER = fields[0].Trim(),
                                    Municipio = fields[1].Trim(),
                                    Operadora = fields[2].Trim(),
                                    Domicilio = fields[3].Trim(),
                                };
                                ficheiros.Add(ficheiro);
                            }
                        }

                        // Cria uma nova thread para processar o arquivo do cliente
                        Thread thread = new Thread(() =>
                        {
                            // Espera a exclusão mútua antes de acessar recursos compartilhados
                            mutex.WaitOne();

                            try
                            {

                                // Ordena todos os arquivos pelo município
                                ficheiros = ficheiros.Skip(0).OrderBy(f => f.Municipio).ToList();

                                // Mostra ao cliente o número de domicílios presentes no arquivo atual
                                int numDomicilios = ficheiros.Count;
                                string numDomiciliosMessage = $"Number of households received: {numDomicilios}\n";
                                byte[] numDomiciliosBytes = Encoding.ASCII.GetBytes(numDomiciliosMessage);
                                stream.Write(numDomiciliosBytes, 0, numDomiciliosBytes.Length);
                                stream.Flush();


                                // Define o caminho do arquivo de saída
                                string outputFilePath = "FicheiroCSV.csv";

                                // Abre o arquivo de saída para escrita
                                using (StreamWriter writer = new StreamWriter(outputFilePath, true))
                                {

                                    foreach (Ficheiro ficheiro in ficheiros)
                                    {
                                        // Escreve a linha no arquivo de saída
                                        writer.WriteLine($"{ficheiro.OWNER};{ficheiro.Municipio};{ficheiro.Operadora};{ficheiro.Domicilio}");
                                    }
                                }

                                // Ordena o arquivo final por município
                                OrdenarArquivoFinal(outputFilePath);

                                stream.Flush();
                                Console.WriteLine($"Informations saved in {outputFilePath}");
                            }
                            finally
                            {
                                // Liberta a exclusão mútua após o acesso aos recursos compartilhados
                                mutex.ReleaseMutex();
                            }
                        });
                        // Inicia a thread para processar o arquivo do cliente
                        thread.Start();
                    }
                    else if (message.ToUpper() == "SEND")
                    {
                        // Define o caminho do arquivo de saída
                        string outputFilePath = "FicheiroCSV.csv";

                        // Cria um stream para ler o arquivo de saída
                        using (FileStream fileStream = File.OpenRead(outputFilePath))
                        {
                            // Envia o arquivo para o cliente
                            byte[] fileBytes = new byte[fileStream.Length];
                            fileStream.Read(fileBytes, 0, fileBytes.Length);
                            stream.Write(fileBytes, 0, fileBytes.Length);
                        }

                        string confirmacao =($"Ficheiro enviado\n");
                        byte[] confirmacaoBytes = Encoding.ASCII.GetBytes(confirmacao);
                        stream.Write(confirmacaoBytes, 0, confirmacaoBytes.Length);
                        stream.Flush();

                        Console.WriteLine($"Arquivo {outputFilePath} enviado para {client.Client.RemoteEndPoint}");
                    }
                    //caso seja necessario enviar outras mensagens sem ser o ficheiro
                    else
                    {
                        // Converte mensagem para bytes e envia de volta ao cliente
                        byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                        stream.Write(messageBytes, 0, messageBytes.Length);
                        stream.Flush();

                        Console.WriteLine("\nMessage received from client {0}: " + message, client.Client.RemoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");

                // Remove o cliente da lista de clientes conectados
                mutex.WaitOne();
                clientes.Remove(client);
                mutex.ReleaseMutex();

                // Fecha a conexão com o cliente
                client.Close();
            }
        }

        public static void OrdenarArquivoFinal(string pathArquivoFinal)
        {
            // Lê todas as linhas do arquivo de saída
            List<string> linhas = File.ReadAllLines(pathArquivoFinal).ToList();

            // Criar um dicionário para armazenar as linhas únicas
            Dictionary<string, string> linhasUnicas = new Dictionary<string, string>();


            // Armazena a primeira linha como cabeçalho
            string cabecalho = linhas[0];

            // Remove a primeira linha da lista (cabeçalho)
            linhas.RemoveAt(0);

            // Cria uma lista de objetos Ficheiro para armazenar as informações
            List<Ficheiro> ficheiros = new List<Ficheiro>();

            // Adiciona cada linha do arquivo de saída como um objeto Ficheiro à lista
            foreach (string linha in linhas)
            {
                string[] campos = linha.Split(';');

                Ficheiro ficheiro = new Ficheiro
                {
                    OWNER = campos[0].Trim(),
                    Municipio = campos[1].Trim(),
                    Operadora = campos[2].Trim(),
                    Domicilio = campos[3].Trim(),
                    //DataProcessamento = DateTime.Now,
                };

                // Verifica se a linha já existe no dicionário linhasUnicas. Se sim, pula para a próxima linha.
                if (linhasUnicas.ContainsKey(linha))
                {
                    continue;
                }

                // Adiciona a linha ao dicionário linhasUnicas.
                linhasUnicas.Add(linha, linha);
                ficheiros.Add(ficheiro);
            }

            //string responsavel = ficheiros.First(f => f.OWNER == "TRUE").Operadora;

            // Classifica a lista de arquivos pelo município
            ficheiros = ficheiros.OrderBy(f => f.Municipio).ToList();

            var domiciliosPorMunicipio = ficheiros.GroupBy(f => f.Municipio);



            // Reescreve o arquivo de saída com a nova ordem
            using (StreamWriter writer = new StreamWriter(pathArquivoFinal))
            {
                // Escreve o cabeçalho no arquivo de saída
                writer.WriteLine(cabecalho);

                // Escreve cada objeto Ficheiro ordenado por município no arquivo de saída
                foreach (Ficheiro ficheiro in ficheiros)
                {
                    //Preenche com a operadora responsável

                    //ficheiro.Responsavel = responsavel;
                    writer.WriteLine($"{ficheiro.OWNER};{ficheiro.Municipio};{ficheiro.Operadora};{ficheiro.Domicilio}");
                }

                // Mostra o número de domicílios por município no console
                foreach (var grupo in domiciliosPorMunicipio)
                {
                    int numDomicilios = grupo.Count();
                    Console.WriteLine($"Município: {grupo.Key} - Número de domicílios: {numDomicilios}");
                }
            }
        }


        //Utilizado de inicio mas posteriormente substitudo
        public static void Broadcast(string message)
        {

            foreach (TcpClient client in clientes)
            {
                if (client.Connected)
                {
                    //using (NetworkStream stream = client.GetStream())
                    //{
                    //    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    //    stream.Write(messageBytes, 0, messageBytes.Length);
                    //    stream.Flush();
                    //}
                }
                else
                {
                    // Remove o cliente da lista se ele não estiver mais conectado
                    mutex.WaitOne();
                    clientes.Remove(client);
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}

