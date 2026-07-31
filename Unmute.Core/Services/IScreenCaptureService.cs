using System.Drawing;

namespace Unmute.Core.Services
{
    public interface IScreenCaptureService
    {
        Bitmap CaptureFrame();
    }
}