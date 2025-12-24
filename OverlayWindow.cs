using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
using WinRT;

namespace Cropalicious
{
    public class OverlayWindow : Form
    {
        #region COM Interop for Composition

        [ComImport, Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICompositorInterop
        {
            void CreateCompositionSurfaceForHandle(IntPtr swapChain, out IntPtr result);
            void CreateCompositionSurfaceForSwapChain(IntPtr swapChain, out IntPtr result);
            void CreateGraphicsDevice(IntPtr renderingDevice, out IntPtr result);
        }

        [ComImport, Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICompositorDesktopInterop
        {
            void CreateDesktopWindowTarget(IntPtr hwndTarget, [MarshalAs(UnmanagedType.Bool)] bool isTopmost, out IntPtr result);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DispatcherQueueOptions
        {
            public int dwSize;
            public int threadType;
            public int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController(
            [In] DispatcherQueueOptions options,
            [Out] out IntPtr dispatcherQueueController);

        // Get IUnknown from WinRT object
        [DllImport("api-ms-win-core-winrt-l1-1-0.dll", PreserveSig = true)]
        private static extern int RoGetActivationFactory(
            IntPtr activatableClassId,
            [In] ref Guid iid,
            out IntPtr factory);

        private const int DQTYPE_THREAD_CURRENT = 2;
        private const int DQTAT_COM_STA = 2;

        private static DesktopWindowTarget CreateDesktopWindowTarget(Compositor compositor, IntPtr hwnd, bool isTopmost)
        {
            // Get IUnknown for the compositor
            IntPtr compositorPtr = Marshal.GetIUnknownForObject(compositor);
            try
            {
                // Query for ICompositorDesktopInterop
                Guid iid = new Guid("29E691FA-4567-4DCA-B319-D0F207EB6807");
                int hr = Marshal.QueryInterface(compositorPtr, in iid, out IntPtr interopPtr);
                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);

                try
                {
                    // Get the interop interface and call CreateDesktopWindowTarget
                    var interop = (ICompositorDesktopInterop)Marshal.GetObjectForIUnknown(interopPtr);
                    interop.CreateDesktopWindowTarget(hwnd, isTopmost, out IntPtr targetPtr);

                    // Convert to DesktopWindowTarget using WinRT marshaling
                    var target = MarshalInterface<DesktopWindowTarget>.FromAbi(targetPtr);
                    Marshal.Release(targetPtr);
                    return target;
                }
                finally
                {
                    Marshal.Release(interopPtr);
                }
            }
            finally
            {
                Marshal.Release(compositorPtr);
            }
        }

        #endregion

        #region Win32 API

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;

        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;

        #endregion

        private readonly AppSettings settings;
        private Point lastMousePos;

        // Composition
        private IntPtr dispatcherQueueController;
        private Compositor? compositor;
        private DesktopWindowTarget? desktopWindowTarget;
        private ContainerVisual? rootVisual;
        private CompositionColorBrush? greenBrush;
        private CompositionColorBrush? whiteBrush;

        // Border visuals (4 sides + 8 corner segments)
        private SpriteVisual? borderTop;
        private SpriteVisual? borderBottom;
        private SpriteVisual? borderLeft;
        private SpriteVisual? borderRight;
        private SpriteVisual[]? cornerVisuals;

        private const int BorderThickness = 3;
        private const int CornerLength = 10;
        private const int CornerThickness = 2;

        public event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;

        public OverlayWindow(AppSettings settings)
        {
            this.settings = settings;
            InitializeWindow();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Enable DirectComposition
                cp.ExStyle |= WS_EX_NOREDIRECTIONBITMAP;
                return cp;
            }
        }

        private void InitializeWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;

            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;

            Cursor = Cursors.Cross;
            KeyPreview = true;

            // Make window background transparent
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitializeComposition();
        }

        private void InitializeComposition()
        {
            // Create dispatcher queue for the current thread
            var options = new DispatcherQueueOptions
            {
                dwSize = Marshal.SizeOf<DispatcherQueueOptions>(),
                threadType = DQTYPE_THREAD_CURRENT,
                apartmentType = DQTAT_COM_STA
            };

            CreateDispatcherQueueController(options, out dispatcherQueueController);

            // Create compositor
            compositor = new Compositor();

            // Use the WinRT interop extension method approach
            desktopWindowTarget = CreateDesktopWindowTarget(compositor, Handle, true);

            // Create root container visual
            rootVisual = compositor.CreateContainerVisual();
            desktopWindowTarget.Root = rootVisual;

            // Create brushes
            greenBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 0)); // Lime green
            whiteBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)); // White

            // Create border visuals (4 sides of the rectangle)
            borderTop = compositor.CreateSpriteVisual();
            borderTop.Brush = greenBrush;

            borderBottom = compositor.CreateSpriteVisual();
            borderBottom.Brush = greenBrush;

            borderLeft = compositor.CreateSpriteVisual();
            borderLeft.Brush = greenBrush;

            borderRight = compositor.CreateSpriteVisual();
            borderRight.Brush = greenBrush;

            rootVisual.Children.InsertAtTop(borderTop);
            rootVisual.Children.InsertAtTop(borderBottom);
            rootVisual.Children.InsertAtTop(borderLeft);
            rootVisual.Children.InsertAtTop(borderRight);

            // Create corner visuals (8 segments: 2 per corner)
            cornerVisuals = new SpriteVisual[8];
            for (int i = 0; i < 8; i++)
            {
                cornerVisuals[i] = compositor.CreateSpriteVisual();
                cornerVisuals[i].Brush = whiteBrush;
                rootVisual.Children.InsertAtTop(cornerVisuals[i]);
            }

            lastMousePos = MousePosition;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Focus();
            Activate();
            BringToFront();
            UpdateOverlayDisplay();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var currentMousePos = MousePosition;
            if (currentMousePos != lastMousePos)
            {
                lastMousePos = currentMousePos;
                UpdateOverlayDisplay();
            }
            base.OnMouseMove(e);
        }

        private void UpdateOverlayDisplay()
        {
            if (compositor == null || rootVisual == null) return;

            var mousePos = MousePosition;
            var clientMousePos = PointToClient(mousePos);

            var halfWidth = settings.CaptureWidth / 2;
            var halfHeight = settings.CaptureHeight / 2;

            var screen = Screen.FromPoint(mousePos);
            var screenBounds = screen.Bounds;
            var clientScreenBounds = RectangleToClient(screenBounds);

            var x = Math.Max(clientScreenBounds.Left + halfWidth,
                    Math.Min(clientMousePos.X, clientScreenBounds.Right - halfWidth)) - halfWidth;
            var y = Math.Max(clientScreenBounds.Top + halfHeight,
                    Math.Min(clientMousePos.Y, clientScreenBounds.Bottom - halfHeight)) - halfHeight;

            var width = settings.CaptureWidth;
            var height = settings.CaptureHeight;

            // Update border positions
            // Top border
            borderTop!.Offset = new Vector3(x, y, 0);
            borderTop.Size = new Vector2(width, BorderThickness);

            // Bottom border
            borderBottom!.Offset = new Vector3(x, y + height - BorderThickness, 0);
            borderBottom.Size = new Vector2(width, BorderThickness);

            // Left border
            borderLeft!.Offset = new Vector3(x, y, 0);
            borderLeft.Size = new Vector2(BorderThickness, height);

            // Right border
            borderRight!.Offset = new Vector3(x + width - BorderThickness, y, 0);
            borderRight.Size = new Vector2(BorderThickness, height);

            // Update corner markers
            // Top-left corner (horizontal + vertical)
            cornerVisuals![0].Offset = new Vector3(x, y, 0);
            cornerVisuals[0].Size = new Vector2(CornerLength, CornerThickness);
            cornerVisuals[1].Offset = new Vector3(x, y, 0);
            cornerVisuals[1].Size = new Vector2(CornerThickness, CornerLength);

            // Top-right corner
            cornerVisuals[2].Offset = new Vector3(x + width - CornerLength, y, 0);
            cornerVisuals[2].Size = new Vector2(CornerLength, CornerThickness);
            cornerVisuals[3].Offset = new Vector3(x + width - CornerThickness, y, 0);
            cornerVisuals[3].Size = new Vector2(CornerThickness, CornerLength);

            // Bottom-left corner
            cornerVisuals[4].Offset = new Vector3(x, y + height - CornerThickness, 0);
            cornerVisuals[4].Size = new Vector2(CornerLength, CornerThickness);
            cornerVisuals[5].Offset = new Vector3(x, y + height - CornerLength, 0);
            cornerVisuals[5].Size = new Vector2(CornerThickness, CornerLength);

            // Bottom-right corner
            cornerVisuals[6].Offset = new Vector3(x + width - CornerLength, y + height - CornerThickness, 0);
            cornerVisuals[6].Size = new Vector2(CornerLength, CornerThickness);
            cornerVisuals[7].Offset = new Vector3(x + width - CornerLength, y + height - CornerLength, 0);
            cornerVisuals[7].Size = new Vector2(CornerThickness, CornerLength);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Composition handles all rendering
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            HandleMouseClick(e);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Don't handle on MouseUp - already handled on MouseDown
            base.OnMouseUp(e);
        }

        private void HandleMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TakeScreenshot();
            }
            else if (e.Button == MouseButtons.Right)
            {
                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTCLIENT;
                return;
            }
            base.WndProc(ref m);
        }

        private void TakeScreenshot()
        {
            var mousePos = MousePosition;
            var captureRect = CalculateCaptureRect(mousePos);

            // Hide overlay and flush DWM so it's guaranteed not to appear in the capture
            Hide();
            try { DwmFlush(); } catch { }

            ScreenshotTaken?.Invoke(this, new ScreenshotEventArgs(captureRect));
            Close();
        }

        private Rectangle CalculateCaptureRect(Point mousePos)
        {
            var halfWidth = settings.CaptureWidth / 2;
            var halfHeight = settings.CaptureHeight / 2;

            var screen = Screen.FromPoint(mousePos);
            var screenBounds = screen.Bounds;

            var x = Math.Max(screenBounds.Left + halfWidth,
                    Math.Min(mousePos.X, screenBounds.Right - halfWidth)) - halfWidth;
            var y = Math.Max(screenBounds.Top + halfHeight,
                    Math.Min(mousePos.Y, screenBounds.Bottom - halfHeight)) - halfHeight;

            return new Rectangle(x, y, settings.CaptureWidth, settings.CaptureHeight);
        }

        public void UpdateScreenBounds()
        {
            Bounds = SystemInformation.VirtualScreen;
            UpdateOverlayDisplay();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Composition objects are reference counted, release them
                greenBrush?.Dispose();
                whiteBrush?.Dispose();

                if (cornerVisuals != null)
                {
                    foreach (var corner in cornerVisuals)
                        corner?.Dispose();
                }

                borderTop?.Dispose();
                borderBottom?.Dispose();
                borderLeft?.Dispose();
                borderRight?.Dispose();
                rootVisual?.Dispose();
                desktopWindowTarget?.Dispose();
                compositor?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class ScreenshotEventArgs : EventArgs
    {
        public Rectangle CaptureArea { get; }

        public ScreenshotEventArgs(Rectangle captureArea)
        {
            CaptureArea = captureArea;
        }
    }
}
