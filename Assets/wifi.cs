using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class TCPClient : MonoBehaviour
{
    public string serverIP = "192.168.87.108"; // Endereço IP do ESP32
    public int serverPort = 80; // Porta do servidor no ESP32

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

     // Extrair os valores do giroscópio
        float pitch = 0f;
        float roll = 0f;
        float yaw = 0f;
     // Extrair os valores do GPS
        float latitude = 0f;
        float longitude = 0f;
        float altitude = 0f;
        float speed = 0f;
    

 
    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;

            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.Start();
            Debug.Log("Connected to server");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
        }
    }

    void ReceiveData()
    {
        byte[] data = new byte[1024];
        while (isConnected)
        {
            try
            {
                int bytesRead = stream.Read(data, 0, data.Length);
                string receivedData = Encoding.ASCII.GetString(data, 0, bytesRead);
                Debug.Log("Received data: " + receivedData);
                
                // Processar os dados recebidos
                string[] dataParts = receivedData.Split(',');

               

                if (dataParts.Length >= 3)
                {
                    pitch = float.Parse((dataParts[0].Split(':')[1]))/1000;
                    roll = float.Parse((dataParts[1].Split(':')[1]))/1000;
                    yaw = float.Parse((dataParts[2].Split(':')[1]))/1000;
                }

               

                if (dataParts.Length >= 9)
                {
                    latitude = float.Parse(dataParts[3].Split(':')[1]);
                    longitude = float.Parse(dataParts[4].Split(':')[1]);
                    altitude = float.Parse(dataParts[5].Split(':')[1]);
                    speed = float.Parse(dataParts[6].Split(':')[1]);
                }

                // Obter a data e hora atual em horário de Brasília (UTC-3)
                DateTime currentTime = DateTime.UtcNow.AddHours(-3);

                // Passar os dados para a thread principal
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    // Atualizar o objeto na Unity com os valores recebidos do MPU6050, GPS e data/hora
                    UpdateCube(pitch, roll, yaw, latitude, longitude, altitude, speed, currentTime);
                });
            }
            catch (Exception e)
            {
                Debug.LogError("Error while receiving data: " + e.Message);
                break;
            }
        }
    }

    void UpdateCube(float pitch, float roll, float yaw, float latitude, float longitude, float altitude, float speed, DateTime currentTime)
    {
        GameObject cube = GameObject.Find("cube"); // Encontre o objeto "cube" na cena
        if (cube != null)
        {
            // Atualizar o objeto com os valores recebidos do MPU6050, GPS e data/hora
            cube.transform.rotation = Quaternion.Euler(pitch, roll, yaw);
            cube.transform.position = new Vector3(latitude, altitude, longitude); // Use latitude, altitude e longitude como coordenadas de posição
            // Faça outras atualizações conforme necessário, como a velocidade e a data/hora
            Debug.Log("Current time (Brasília): " + currentTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else
        {
            Debug.LogError("Cube object not found in the scene.");
        }
    }

    void OnDestroy()
    {
        if (isConnected)
        {
            isConnected = false;
            receiveThread.Join(); // Aguarda até que a thread de recebimento termine
            stream.Close();
            client.Close();
            Debug.Log("Disconnected from server");
        }
    }
}
