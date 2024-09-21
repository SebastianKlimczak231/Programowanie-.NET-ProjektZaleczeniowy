using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;


class ChatClient
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("1234567890123456"); 
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); 

    private static readonly string serverIp = "127.0.0.1";
    private static readonly int port = 8888;

    static void Main(string[] args)
    {
        TcpClient client = new TcpClient();
        client.Connect(serverIp, port);
        NetworkStream stream = client.GetStream();

        Console.WriteLine("Połączono z serwerem. Możesz wysyłać wiadomości.");

        Thread receiveThread = new Thread(() => ReceiveMessages(stream));
        receiveThread.Start();

        while (true)
        {
            string message = Console.ReadLine();
            byte[] encryptedMessage = EncryptMessage(message);

           // Console.WriteLine($"Zakodowana wiadomość: {Convert.ToBase64String(encryptedMessage)}");

            SendMessage(stream, encryptedMessage);
        }
    }

    private static void ReceiveMessages(NetworkStream stream)
    {
        while (true)
        {
            try
            {
                byte[] lengthBuffer = new byte[4];
                int bytesRead = stream.Read(lengthBuffer, 0, 4);

                if (bytesRead == 4)
                {
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] encryptedMessage = ReceiveFullMessage(stream, messageLength);
                    string message = DecryptMessage(encryptedMessage);
                    Console.WriteLine($"Otrzymano: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                break;
            }
        }
    }

    private static byte[] ReceiveFullMessage(NetworkStream stream, int messageLength)
    {
        byte[] buffer = new byte[messageLength];
        int totalBytesRead = 0;

        while (totalBytesRead < messageLength)
        {
            int bytesRead = stream.Read(buffer, totalBytesRead, messageLength - totalBytesRead);
            if (bytesRead == 0)
                throw new Exception("Połączenie zamknięte przez serwer.");
            totalBytesRead += bytesRead;
        }

        return buffer;
    }

    private static void SendMessage(NetworkStream stream, byte[] message)
    {
        byte[] lengthBuffer = BitConverter.GetBytes(message.Length);
        stream.Write(lengthBuffer, 0, 4);
        stream.Write(message, 0, message.Length);
    }

    private static byte[] EncryptMessage(string message)
    {
        using (var encryptor = Aes.Create().CreateEncryptor(Key, IV))
        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                byte[] plainText = Encoding.UTF8.GetBytes(message);
                cs.Write(plainText, 0, plainText.Length);
            }
            return ms.ToArray();
        }
    }

    private static string DecryptMessage(byte[] encryptedMessage)
    {
        using (var decryptor = Aes.Create().CreateDecryptor(Key, IV))
        using (var ms = new MemoryStream(encryptedMessage))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var reader = new StreamReader(cs))
        {
            return reader.ReadToEnd();
        }
    }
}




