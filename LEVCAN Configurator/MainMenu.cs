using ImGuiNET;
using LEVCAN_Configurator;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid.StartupUtilities;
using Veldrid;
using Veldrid.Sdl2;
using System.Runtime.InteropServices.JavaScript;
using Vortice.Mathematics;
using System.Runtime.InteropServices;
using LEVCAN;
using System.Xml.Linq;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Veldrid.ImageSharp;
using System.Reflection;
using System.IO;
using LEVCAN_Configurator.Tabs;

namespace LEVCAN_Configurator
{
    internal partial class MainMenu
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;
        // private static MemoryEditor _memoryEditor;
        LevcanHandler Lev;
        // UI state
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);

        Properties.Settings settings;
        Stopwatch stopwatch = new Stopwatch();
        long stopwatch_ms = 0;

        static void SetThing(out float i, float val) { i = val; }
        List<IMGUI_TabInterface> tabsList = new List<IMGUI_TabInterface>();
        public MainMenu()
        {
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "LEVCAN Configurator"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
            ApplyStyle();

            //Load logo in a wacky way
            MemoryStream ms = new MemoryStream();
            Properties.Resources.logo.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            var img = new ImageSharpTexture(ms);
            var dimg = img.CreateDeviceTexture(_gd, _gd.ResourceFactory);
            var ImgPtr = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, dimg); //This returns the intPtr need for Imgui.Image()
            var settingsTab = new SettingsTab();
            settingsTab.logo = ImgPtr;

            tabsList.Add(new ParametersTab());
            tabsList.Add(new DashboardTab());
            tabsList.Add(settingsTab);
#if DEBUG
            tabsList.Add(new TestTab());
#endif

            settings = Properties.Settings.Default;
            Lev = new LevcanHandler((CANDevice)settings.Connection);
            Lev.FileServer.SavePath = settings.FSpath;

            foreach (var tab in tabsList)
            {
                tab.Initialize(Lev, settings);
            }

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            // Main application loop
            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists)
                    break;
                _controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                if (!_window.Focused)
                    Thread.Sleep(10);

                stopwatch.Restart();
                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);

                stopwatch.Stop();
                stopwatch_ms = stopwatch.ElapsedMilliseconds;

                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            settings.Save();
            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        float bottom_bar_offset = 30;
        int ticks = 0, txrx_count = 0;
        private void SubmitUI()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172

            // 1. Show a simple window.
            // Tip: if we don't call ImGui.BeginWindow()/ImGui.EndWindow() the widgets automatically appears in a window called "Debug".
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
            ImGui.Begin("Base Window", ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            ImGui.BeginChild("topmenu", new Vector2(0, -bottom_bar_offset), false);
            if (ImGui.BeginTabBar("Base tabs###secrettabkey", ImGuiTabBarFlags.None))
            {
                //attach tabs to 'top'
                //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8.0f, 0.0f));
                //ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 0);

                // ImGui.PopStyleVar(2);
                for (int tabi = 0; tabi < tabsList.Count; tabi++)
                {
                    tabsList[tabi].Draw();
                }

                ImGui.EndTabBar();
            }
            else
            {
                Debug.Fail("Tabs not visible");
            }
            ImGui.EndChild();
            // BOTTOM STATUS INFO
            float saved_cursorY = ImGui.GetCursorPosY();
            float framerate = ImGui.GetIO().Framerate;
            string status = Lev.icanPort.Status;
            if (status == null)
                status = "Disconnected";
            ticks++;

            if (ticks >= 30)
            {
                ticks = 0;
                txrx_count = Lev.icanPort.TXcounter + Lev.icanPort.RXcounter;
                Lev.icanPort.TXcounter = 0;
                Lev.icanPort.RXcounter = 0;
            }
            ImGui.Text($"{status} | TX/RX: {txrx_count} | Application average {stopwatch_ms:0.##} ms/frame ({framerate:0.#} FPS) | ");
            bottom_bar_offset = ImGui.GetCursorPosY() - saved_cursorY;
            ImGui.End();


            ImGuiIOPtr io = ImGui.GetIO();
            SetThing(out io.DeltaTime, 2f);
        }

        void ApplyStyle()
        {
            // VasiTheme style from ImThemes
            var style = ImGuiNET.ImGui.GetStyle();

            style.Alpha = 1.0f;
            style.DisabledAlpha = 0.6000000238418579f;
            style.WindowPadding = new Vector2(8.0f, 8.0f);
            style.WindowRounding = 4.0f;
            style.WindowBorderSize = 1.0f;
            style.WindowMinSize = new Vector2(32.0f, 32.0f);
            style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.Left;
            style.ChildRounding = 4.0f;
            style.ChildBorderSize = 1.0f;
            style.PopupRounding = 4.0f;
            style.PopupBorderSize = 1.0f;
            style.FramePadding = new Vector2(4.0f, 3.0f);
            style.FrameRounding = 4.0f;
            style.FrameBorderSize = 1.0f;
            style.ItemSpacing = new Vector2(8.0f, 4.0f);
            style.ItemInnerSpacing = new Vector2(4.0f, 4.0f);
            style.CellPadding = new Vector2(4.0f, 2.0f);
            style.IndentSpacing = 21.0f;
            style.ColumnsMinSpacing = 6.0f;
            style.ScrollbarSize = 14.0f;
            style.ScrollbarRounding = 9.0f;
            style.GrabMinSize = 10.0f;
            style.GrabRounding = 4.0f;
            style.TabRounding = 4.0f;
            style.TabBorderSize = 0.0f;
            style.TabMinWidthForCloseButton = 0.0f;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

            style.Colors[(int)ImGuiCol.Text] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.6000000238418579f, 0.6000000238418579f, 0.6000000238418579f, 1.0f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.9372549057006836f, 0.9372549057006836f, 0.9372549057006836f, 1.0f);
            style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(1.0f, 1.0f, 1.0f, 0.9800000190734863f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0.0f, 0.0f, 0.0f, 0.300000011920929f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.4000000059604645f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.6700000166893005f);
            style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.95686274766922f, 0.95686274766922f, 0.95686274766922f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.8196078538894653f, 0.8196078538894653f, 0.8196078538894653f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(1.0f, 1.0f, 1.0f, 0.5099999904632568f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.8588235378265381f, 0.8588235378265381f, 0.8588235378265381f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.9764705896377563f, 0.9764705896377563f, 0.9764705896377563f, 0.5299999713897705f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.686274528503418f, 0.686274528503418f, 0.686274528503418f, 0.800000011920929f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.4862745106220245f, 0.4862745106220245f, 0.4862745106220245f, 0.800000011920929f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.4862745106220245f, 0.4862745106220245f, 0.4862745106220245f, 1.0f);
            style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.2599999904632568f, 0.5899999737739563f, 0.9800000190734863f, 1.0f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.239999994635582f, 0.5199999809265137f, 0.8799999952316284f, 1.0f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.2599999904632568f, 0.5899999737739563f, 0.9800000190734863f, 1.0f);
            style.Colors[(int)ImGuiCol.Button] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.4000000059604645f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.05882352963089943f, 0.529411792755127f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.Header] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.3100000023841858f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.800000011920929f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.3882353007793427f, 0.3882353007793427f, 0.3882353007793427f, 0.6200000047683716f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.1372549086809158f, 0.4392156898975372f, 0.800000011920929f, 0.7799999713897705f);
            style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.1372549086809158f, 0.4392156898975372f, 0.800000011920929f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.3490196168422699f, 0.3490196168422699f, 0.3490196168422699f, 0.1700000017881393f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.6700000166893005f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.949999988079071f);
            style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.7607843279838562f, 0.7960784435272217f, 0.8352941274642944f, 0.9309999942779541f);
            style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.800000011920929f);
            style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.5921568870544434f, 0.7254902124404907f, 0.8823529481887817f, 1.0f);
            style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.9176470637321472f, 0.9254902005195618f, 0.9333333373069763f, 0.9861999750137329f);
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.7411764860153198f, 0.8196078538894653f, 0.9137254953384399f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.3882353007793427f, 0.3882353007793427f, 0.3882353007793427f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.0f, 0.4274509847164154f, 0.3490196168422699f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.4140249490737915f, 0.5209223628044128f, 0.725321888923645f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.2671627700328827f, 0.4638949334621429f, 0.8412017226219177f, 1.0f);
            style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.7764706015586853f, 0.8666666746139526f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.5686274766921997f, 0.5686274766921997f, 0.6392157077789307f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.6784313917160034f, 0.6784313917160034f, 0.7372549176216125f, 1.0f);
            style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.2980392277240753f, 0.2980392277240753f, 0.2980392277240753f, 0.09000000357627869f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.3499999940395355f);
            style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.949999988079071f);
            style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.800000011920929f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(0.6980392336845398f, 0.6980392336845398f, 0.6980392336845398f, 0.699999988079071f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.2000000029802322f, 0.2000000029802322f, 0.2000000029802322f, 0.2000000029802322f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.2000000029802322f, 0.2000000029802322f, 0.2000000029802322f, 0.3499999940395355f);

        }
    }
}
