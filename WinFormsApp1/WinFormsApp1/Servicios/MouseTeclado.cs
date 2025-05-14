
using System.Runtime.InteropServices;

public static class MouseKeyboardControl
{
    
    public static void MouseClick(int x, int y)
    {
        MoveMouse(x, y); // Move the mouse to the specified coordinates
        // Simulate a mouse click
        mouse_event(MouseEventFlags.LeftDown | MouseEventFlags.LeftUp, x, y, 0, 0);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    [Flags]
    private enum MouseEventFlags : uint
    {
        LeftDown = 0x0002,
        LeftUp = 0x0004
    }
    // Importar funciones de user32.dll
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    // Constantes para mouse
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    // Mover el mouse a coordenadas absolutas
    public static void MoveMouse(int x, int y)
    {
        // Convertir coordenadas absolutas a proporciones de pantalla
        int absoluteX = (x * 65535) / Screen.PrimaryScreen.Bounds.Width;
        int absoluteY = (y * 65535) / Screen.PrimaryScreen.Bounds.Height;

        mouse_event(MOUSEEVENTF_MOVE, (uint)absoluteX, (uint)absoluteY, 0, IntPtr.Zero);
    }

    // Simular pulsación de tecla
    public static void SendKey(Keys key)
    {
        keybd_event((byte)key, 0, 0, IntPtr.Zero);
        keybd_event((byte)key, 0, 0x0002, IntPtr.Zero); // KEYEVENTF_KEYUP
    }
}