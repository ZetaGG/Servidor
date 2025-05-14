
namespace WinFormsApp1.Models
{


    [Serializable]
    public class Command
    {
        public CommandType Type { get; set; }
        public object Data { get; set; }
    }

    public enum CommandType
    {
        MouseMove,
        MouseClick,
        KeyPress,
        CaptureScreen,
        SendFile,
        ReceiveFile,
        OpenNotepad,
        StreamStart,
        StreamStop,
    }
}