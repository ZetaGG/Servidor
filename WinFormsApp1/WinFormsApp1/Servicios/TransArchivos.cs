// FileTransferService.cs

using System.Net.Sockets;
using WinFormsApp1.Utilidades;

public static class FileTransferService
{
    private const string DOWNLOAD_FOLDER = @"C:\Users\zetag\RiderProjects\Servidor\WinFormsApp1\WinFormsApp1\Archivos";

    public static async Task ReceiveFileAsync(NetworkStream stream, string fileName, int fileSize)
    {
        try
        {
            // Crear carpeta si no existe
            Directory.CreateDirectory(DOWNLOAD_FOLDER);

            string filePath = Path.Combine(DOWNLOAD_FOLDER, fileName);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                int totalRead = 0;

                while (totalRead < fileSize)
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    totalRead += bytesRead;
                    await fs.WriteAsync(buffer, 0, bytesRead);
                }
            }

            Logger.Log($"Archivo recibido: {fileName}");
        }
        catch (Exception ex)
        {
            Logger.Log("Error al recibir archivo: " + ex.Message);
        }
    }
}