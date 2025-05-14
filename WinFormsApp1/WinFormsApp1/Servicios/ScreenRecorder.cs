// ScreenRecorder.cs
using System.Drawing.Imaging;
using System.Net.Sockets;
using WinFormsApp1.Models;
using WinFormsApp1.Utilidades;

namespace WinFormsApp1.Servicios
{
    public class ScreenRecorder
    {
        private bool _isRecording = false;
        private CancellationTokenSource _cancellationTokenSource;
        private NetworkStream _clientStream;
        private int _frameRate = 10; // Frames por segundo
        private int _quality = 70;   // Calidad de compresión (0-100)
        private Size _resolution;    // Resolución de la captura (null = pantalla completa)

        public bool IsRecording => _isRecording;

        /// <summary>
        /// Inicia la transmisión de pantalla en tiempo real
        /// </summary>
        /// <param name="clientStream">Stream del cliente para enviar los datos</param>
        /// <param name="frameRate">Frames por segundo (predeterminado: 10)</param>
        /// <param name="quality">Calidad de compresión JPEG (0-100)</param>
        /// <param name="width">Ancho de la captura (0 = pantalla completa)</param>
        /// <param name="height">Alto de la captura (0 = pantalla completa)</param>
        public async Task StartAsync(NetworkStream clientStream, int frameRate = 10, int quality = 70, int width = 0, int height = 0)
        {
            if (_isRecording) return;

            try
            {
                _clientStream = clientStream;
                _frameRate = frameRate;
                _quality = quality;
                
                // Si se especifica una resolución, usarla
                if (width > 0 && height > 0)
                    _resolution = new Size(width, height);
                else
                    _resolution = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                _cancellationTokenSource = new CancellationTokenSource();
                _isRecording = true;

                // Enviar cabecera JSON que indica que se inicia la transmisión
                var startCommand = new Command
                {
                    Type = CommandType.StreamStart,
                    Data = new { Width = _resolution.Width, Height = _resolution.Height, FrameRate = _frameRate }
                };
                
                string jsonStart = System.Text.Json.JsonSerializer.Serialize(startCommand);
                byte[] headerData = System.Text.Encoding.UTF8.GetBytes(jsonStart);
                await _clientStream.WriteAsync(headerData, 0, headerData.Length);
                
                // Esperar un momento para que el cliente procese la cabecera
                await Task.Delay(100);

                // Iniciar el bucle de captura y envío
                await CaptureLoopAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error al iniciar grabación de pantalla: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Detiene la transmisión de pantalla
        /// </summary>
        public void Stop()
        {
            if (!_isRecording) return;

            _cancellationTokenSource?.Cancel();
            _isRecording = false;
            
            try
            {
                // Enviar comando de fin de transmisión
                var stopCommand = new Command
                {
                    Type = CommandType.StreamStop,
                    Data = null
                };
                
                string jsonStop = System.Text.Json.JsonSerializer.Serialize(stopCommand);
                byte[] stopData = System.Text.Encoding.UTF8.GetBytes(jsonStop);
                _clientStream.Write(stopData, 0, stopData.Length);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error al detener grabación: {ex.Message}");
            }
            
            Logger.Log("Transmisión de pantalla detenida");
        }

        private async Task CaptureLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                int frameDelayMs = 1000 / _frameRate;
                var encoderParams = GetEncoderParameters(_quality);
                
                Logger.Log($"Iniciando transmisión: {_resolution.Width}x{_resolution.Height} a {_frameRate} FPS");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var startTime = DateTime.Now;
                    
                    // Capturar la pantalla
                    using (Bitmap screenshot = CaptureScreen())
                    {
                        // Comprimir y enviar
                        byte[] frameData = CompressFrame(screenshot, encoderParams);
                        
                        // Enviar el tamaño del frame primero (4 bytes)
                        byte[] frameSizeBytes = BitConverter.GetBytes(frameData.Length);
                        await _clientStream.WriteAsync(frameSizeBytes, 0, frameSizeBytes.Length, cancellationToken);
                        
                        // Enviar el frame
                        await _clientStream.WriteAsync(frameData, 0, frameData.Length, cancellationToken);
                        
                        // Verificar si el cliente está conectado
                        if (!_clientStream.Socket.Connected)
                        {
                            Logger.Log("Cliente desconectado, deteniendo transmisión");
                            break;
                        }
                    }
                    
                    // Calcular tiempo de espera para mantener el frame rate
                    var processingTime = (DateTime.Now - startTime).TotalMilliseconds;
                    var waitTime = frameDelayMs - (int)processingTime;
                    
                    if (waitTime > 0)
                        await Task.Delay(waitTime, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal
            }
            catch (Exception ex)
            {
                Logger.Log($"Error en bucle de captura: {ex.Message}");
            }
            finally
            {
                _isRecording = false;
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds;
            
            // Usar la resolución especificada o la pantalla completa
            if (_resolution.Width > 0 && _resolution.Height > 0)
            {
                // Si la resolución es diferente de la pantalla, escalar
                if (_resolution.Width != Screen.PrimaryScreen.Bounds.Width || 
                    _resolution.Height != Screen.PrimaryScreen.Bounds.Height)
                {
                    bounds = Screen.PrimaryScreen.Bounds;
                    Bitmap fullScreenshot = new Bitmap(bounds.Width, bounds.Height);
                    using (Graphics g = Graphics.FromImage(fullScreenshot))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }
                    
                    // Redimensionar
                    Bitmap resized = new Bitmap(fullScreenshot, _resolution);
                    fullScreenshot.Dispose();
                    return resized;
                }
            }
            
            // Captura a tamaño completo
            bounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            
            return screenshot;
        }

        private byte[] CompressFrame(Bitmap bitmap, EncoderParameters encoderParams)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Obtener el codec para JPEG
                ImageCodecInfo jpegCodec = GetJpegCodecInfo();
                
                // Guardar con la calidad especificada
                bitmap.Save(ms, jpegCodec, encoderParams);
                return ms.ToArray();
            }
        }

        private EncoderParameters GetEncoderParameters(int quality)
        {
            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            return parameters;
        }

        private ImageCodecInfo GetJpegCodecInfo()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    return codec;
                }
            }
            throw new Exception("No se encontró el codec JPEG");
        }
    }
}