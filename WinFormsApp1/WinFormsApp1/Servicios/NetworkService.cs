using System.Net;
using System.Net.Sockets;

using System;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WinFormsApp1.Models; // Para la clase Command
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using CommandType = WinFormsApp1.Models.CommandType;

namespace ServidorControlRemoto.Services
{
    public class NetworkService
    {
        private TcpListener _listener;
        private const int PORT = 5000;

        public async Task StartAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, PORT);
                _listener.Start();
                Console.WriteLine("Servidor iniciado en puerto " + PORT);

                while (true)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client); // Procesar cliente en segundo plano
                }
            }
            catch (Exception ex)
            {
                // Registrar errores con Logger.cs
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (NetworkStream stream = client.GetStream())
            {
                while (true)
                {
                    try
                    {
                        // Leer comando del cliente
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // Cliente desconectado

                        // Deserializar comando (ejemplo con BinaryFormatter)
                        Command command = DeserializeCommand(buffer);
                        await ProcessCommandAsync(command, stream);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error en conexión: " + ex.Message);
                        break;
                    }
                }
            }
            client.Close();
        }

        private Command DeserializeCommand(byte[] data)
        {
            try
            {
                // Convert byte array to string
                string jsonString = System.Text.Encoding.UTF8.GetString(data);
        
                // Deserialize using System.Text.Json
                return JsonSerializer.Deserialize<Command>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                // Log the error appropriately
                throw new InvalidOperationException("Failed to deserialize command", ex);
            }
        }

   


        // En NetworkService.cs
        private async Task ProcessCommandAsync(Command command, NetworkStream stream)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            
            switch (command.Type)
            {
                case CommandType.MouseMove:
                    var coords = (Tuple<int, int>)command.Data;
                    MouseKeyboardControl.MoveMouse(coords.Item1, coords.Item2);
                    break;

                case CommandType.KeyPress:
                    var key = (Keys)command.Data;
                    MouseKeyboardControl.SendKey(key);
                    break;

                case CommandType.CaptureScreen:
                    byte[] screenshot = await ScreenCapture.CaptureAsync();
                    await stream.WriteAsync(screenshot, 0, screenshot.Length);
                    break;

                case CommandType.ReceiveFile:
                    var fileInfo = (Tuple<string, int>)command.Data;
                    await FileTransferService.ReceiveFileAsync(stream, fileInfo.Item1, fileInfo.Item2);
                    break;

                case CommandType.OpenNotepad:
                    Process.Start("notepad.exe");
                    break;

                default:
                    break;
            }
        }

        private async Task SendScreenshotAsync(NetworkStream stream)
        {
            // Usar ScreenCapture.Capture() para obtener la imagen
            // Comprimir y enviar por 'stream'
            byte[] screenshotData = await ScreenCapture.CaptureAsync();
            await stream.WriteAsync(screenshotData, 0, screenshotData.Length);
        }
    }
}