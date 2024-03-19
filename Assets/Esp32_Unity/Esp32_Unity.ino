#include <WiFi.h>
#include <WiFiClient.h>
#include <MPU6050.h>
#include <TinyGPS++.h>

// Nome e senha da rede WIFI
const char *ssid = "Monte"; // Nome da rede wifi, lembrando que tem que está igual ao nome mostrado no roteador 
const char *password = "12345678";  // senha da rede Wifi para se conectar 
// Ao se conectar a rede Wifi você precisa abrir o Serial monitar para ver o IP do ESP gerado, você vai colocar o IP gerado em qualquer dispositivo conectado na sua rede Wifi 

WiFiServer server(80); // Porta padrao 
WiFiClient client;

MPU6050 mpu;
HardwareSerial GPS_Serial(1); //Porta serial 1 do ESP32
TinyGPSPlus gps;

void setup() {
  Serial.begin(9600);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }
  Serial.println("Connected to WiFi");

  // Imprime o endereço IP e a porta utilizados
  Serial.print("IP Address: ");
  Serial.println(WiFi.localIP());
  Serial.print("Port: ");
  Serial.println(80);

//  GPS_Serial.begin(9600, SERIAL_8N1, 16, 17);

 Wire.begin();
  mpu.initialize();
  mpu.setFullScaleGyroRange(MPU6050_GYRO_FS_250);

  server.begin();
  Serial.println("TCP/IP server started");
}

void loop() {
  client = server.available();
  if (client) {
    while (client.connected()) {
      // Lê os valores do acelerômetro e giroscópio
      int16_t gx, gy, gz;
      mpu.getRotation(&gx, &gy, &gz);
     // int16_t ax, ay, az, gx, gy, gz;
     // mpu.getMotion6(&ax, &ay, &az, &gx, &gy, &gz);

      // Calcula os valores de pitch, roll e yaw
     // float pitch = atan2(-ax, sqrt(ay * ay + az * az)) * 180 / M_PI;
     // float roll = atan2(ay, az) * 180 / M_PI;
     // float yaw = atan2(gy, sqrt(gx * gx + gz * gz)) * 180 / M_PI;

     //String data = "Pitch: " + String(pitch) + ", Roll: " + String(roll) + ", Yaw: " + String(yaw);
     String data = "Gyro X: " + String(gx) + ", gyro Y: " + String(gy) + ", Gyro Z: " + String(gz);

      // Aguarda até que haja dados suficientes disponíveis no módulo GPS
      while (GPS_Serial.available() > 0) {
        gps.encode(GPS_Serial.read());
      }
      
      // Se houver dados de localização disponíveis, imprime latitude e longitude - só mostra os dados se fizer a triangulação do GPS "Led piscando do GPS"
      if (gps.location.isUpdated()) {
        data += ", LAT: " + String(gps.location.lat(), 6) + ", LON: " + String(gps.location.lng(), 6);
        data += ", Data: " + String(gps.date.day()) + "/" + String(gps.date.month()) + "/" + String(gps.date.year());
        
        // Ajuste de hora para o fuso horário de Brasília (UTC-3)
        int hour = gps.time.hour();
        if (hour >= 3)
          hour -= 3;
        else
          hour = 24 + hour - 3; // Correção para garantir que o resultado esteja dentro do intervalo de 0 a 23 horas
          
        data += ", Hora: " + String(hour) + ":" + String(gps.time.minute()) + ":" + String(gps.time.second());
        data += ", Altitude: " + String(gps.altitude.meters()) + " m";
        data += ", Velocidade: " + String(gps.speed.kmph()) + " km/h";
      }

      client.print(data);
      delay(1000); // ou um tempo adequado para limitar a taxa de transmissão
    }
    client.stop();
    Serial.println("Client disconnected");
  }
}
