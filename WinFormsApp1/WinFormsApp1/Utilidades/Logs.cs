namespace WinFormsApp1.Utilidades;


    public static class Logger
    {
        private static readonly string LOG_FILE = @"C:\Users\zetag\OneDrive\Documentos\log.txt";

        public static void Log(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now}] {message}{Environment.NewLine}";
                File.AppendAllText(LOG_FILE, logEntry);
            }
            catch (Exception)
            {
                // Ignorar errores de registro
            }
        }
    }
