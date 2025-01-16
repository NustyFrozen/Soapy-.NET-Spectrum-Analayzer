﻿using ClickableTransparentOverlay;
using Design_imGUINET;
using ImGuiNET;
using SoapySpectrum.Extentions;
using SoapySpectrum.Extentions.Design_imGUINET;
using SoapySpectrum.soapypower;
using System.Numerics;
namespace SoapySpectrum.UI
{
    public partial class UI : Overlay
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool wantKeepDemoWindow = true;
        public UI() : base(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
        {
            this.VSync = true;
        }

        protected override Task PostInitialized()
        {
            VSync = false;
            return Task.CompletedTask;
        }
        public static uint ToUint(Color c)
        {
            uint u = (uint)c.A << 24;
            u += (uint)c.B << 16;
            u += (uint)c.G << 8;
            u += c.R;
            return u;
        }

        private static ushort[] iconRange = new ushort[] { 0xe005, 0xf8ff, 0 };
        public static bool refreshConfiguration()
        {
            Logger.Debug("Refreshing Configuration called by User");
            try
            {
                if (formatFreq(display_FreqStop) - formatFreq(display_FreqStart) < 0)
                {
                    throw new Exception("Left band cannot be higher than Right band");
                }
                Configuration.config["freqStart"] = formatFreq(display_FreqStart);
                Configuration.config["freqStop"] = formatFreq(display_FreqStop);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception Called in refreshing configuration -> {ex.Message}");
                return false;
            }
            
            //rounding it to sample rate so we wont get samples outside specified bounds
            traces[selectedTrace].marker.bandPowerSpan = formatFreq(traces[selectedTrace].marker.bandPower_Span_str);

            try
            {
                Configuration.config["graph_OffsetDB"] = Convert.ToDouble(display_Offset);
            }
            catch (Exception ex)
            {
                
                Logger.Error($"Exception Called in refreshing configuration -> {ex.Message}");
                return false;
            }
            return true;
        }
        static ImFontPtr PoppinsFont, IconFont;
        public bool initializedResources = false;

        public unsafe void loadResources()
        {
            Logger.Debug("Loading Application Resources");
            var io = ImGui.GetIO();

            this.ReplaceFont(config =>
            {
                var io = ImGui.GetIO();
                io.Fonts.AddFontFromFileTTF(@"Fonts\Poppins-Light.ttf", 16, config, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
                config->MergeMode = 1;
                config->OversampleH = 1;
                config->OversampleV = 1;
                config->PixelSnapH = 1;

                var custom2 = new ushort[] { 0xe005, 0xf8ff, 0x00 };
                fixed (ushort* p = &custom2[0])
                {
                    io.Fonts.AddFontFromFileTTF("Fonts\\fa-solid-900.ttf", 16, config, new IntPtr(p));
                }
            });
            Logger.Debug("Replaced font");

            PoppinsFont = io.Fonts.AddFontFromFileTTF(@"Fonts\Poppins-Light.ttf", 16);
            //IconFont = io.Fonts.AddFontFromFileTTF(@"Fonts\fa-solid-900.ttf", 16,, new ushort[] { 0xe005,
            //0xf8ff,0});
        }
        static int tabID = 2,gain = 0;
        string[] availableTabs = new string[] { $"\ue473 Amplitude", $"\uf1fe BW" , $"{FontAwesome5.WaveSquare} Frequency", $"\uf3c5 Trace & Marker", $"\uf085 Calibration" , $"{FontAwesome5.Microchip} Device" };
        static string RBW = "0.1M";
        bool visble = true;
        public void scaleEverything()
        {
            Configuration.mainWindow_Size = ImGui.GetWindowSize();
            var mainWindow_Size = Configuration.mainWindow_Size;
            Configuration.graph_Size = new Vector2(Convert.ToInt16(mainWindow_Size.X * .8), Convert.ToInt16(mainWindow_Size.Y * .95));
            Configuration.option_Size = new Vector2(Convert.ToInt16(mainWindow_Size.X * .2), Convert.ToInt16(mainWindow_Size.Y));
            Configuration.input_Size = new Vector2(mainWindow_Size.X / 4, mainWindow_Size.X / 4); //square on purpose
        }
        public void renderDevice()
        {
            var inputTheme = ImGuiTheme.getTextTheme();
            var buttonTheme = ImGuiTheme.getButtonTheme();
            buttonTheme.text = "Apply Changes";
            if(SoapyPower.flashing)
            {
                ImGui.Text("Flashing USRP, Please Wait...");
                buttonTheme.text = "Flashing USRP, Please Wait..";
                buttonTheme.bgcolor = Color.Red.ToUint();
            }
            
            ImGui.NewLine();
            ImGui.Text($"\uf519 PGA Amplifier (WARNING MAX DB INPUT -20):");
            if (ImGui.SliderInt("Gain", ref gain, 0, 60))
            {
                refreshConfiguration();
                SoapyPower.updateGain("PGA", gain);
            }
           // ImGui.Checkbox($"Enable CFF", ref CCF);
        }
        public static void drawCursor()
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.None);
            var cursorpos = ImGui.GetMousePos();
            ImGui.GetForegroundDrawList().AddLine(new Vector2(cursorpos.X - 5, cursorpos.Y), new Vector2(cursorpos.X + 5, cursorpos.Y), Color.White.ToUint());
            ImGui.GetForegroundDrawList().AddLine(new Vector2(cursorpos.X, cursorpos.Y -5), new Vector2(cursorpos.X, cursorpos.Y + 5), Color.White.ToUint());

        }
        protected unsafe override void Render()
        {
            Thread.Sleep(1);
            var inputTheme = ImGuiTheme.getTextTheme();
            if (Imports.GetAsyncKeyState(Keys.Insert))
            {
                Thread.Sleep(200);
                visble = !visble;
            }
            if (!visble) return;
            if (!initializedResources)
            {
                waitForMouseClick.Start();
                markerMoveKeys.Start();
                initializeTraces();
                loadResources();
                ImGui.SetNextWindowPos(Configuration.mainWindow_Pos);
                ImGui.SetNextWindowSize(Configuration.mainWindow_Size);
                initializedResources = true;
                SoapyPower.flashing = false;
                SoapyPower.stopStream();
                SoapyPower.beginStream();
            }
            ImGui.Begin("Spectrum Analyzer", Configuration.mainWindow_flags);
            scaleEverything();
            ImGuiTheme.drawExitButton(15,Color.Gray, Color.White);
            
            ImGui.BeginChild("Spectrum Graph", Configuration.graph_Size);
            drawGraph();
            ImGui.EndChild();


            ImGui.SetCursorPos(new Vector2(Configuration.graph_Size.X + 60, 10));
            ImGui.BeginChild("Spectrum Options", Configuration.option_Size);
            renderDevice();
            ImGui.NewLine();
            ImGui.NewLine();
            inputTheme.prefix = "RBW";
            inputTheme.size = new Vector2(262, 35);
            ImGuiTheme.glowingCombo("InputSelectortext4", ref tabID, availableTabs, inputTheme);
            ImGui.NewLine();
            switch (tabID)
            {
                case 0:
                    renderAmplitude();
                    break;
                    case 1:
                    renderVideo();
                    break;
                case 2:
                    renderFrequency();
                    break;
                case 3:
                    renderTrace();
                    break;
                case 4:
                    renderCalibration();
                    break;
                case 5:

                    break;
            }
            ImGui.EndChild();
            drawCursor();
            ImGui.End();
        }
    }
}
