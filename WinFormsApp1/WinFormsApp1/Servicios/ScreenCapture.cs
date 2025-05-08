// ScreenCapture.cs

using System.Drawing.Imaging;
using WinFormsApp1.Utilidades;

public static class ScreenCapture
{
    public static async Task<byte[]> CaptureAsync()
    {
        try
        {
            // Capturar pantalla completa
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                // Comprimir imagen a JPEG
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Error al capturar pantalla: " + ex.Message);
            return Array.Empty<byte>();
        }
    }
}