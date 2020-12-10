using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using OBSWebsocketDotNet;
//using OBSWebsocketDotNet.Types;
//
using System.Speech.Recognition;
//
using NAudio.CoreAudioApi;
//
using System.Runtime.InteropServices;
//
using System.Threading;
//
using WindowsInput;
using WindowsInput.Native;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;



namespace SpeechStream
{
    public partial class StreamSpeechAssistan : Form
    {
        //
        protected OBSWebsocket _obs;        
        readonly string url = "ws://127.0.0.1:4444";
        readonly string password = "";
        //
        SpeechRecognitionEngine speechRecognitionEngine = null;
        List<String> words = new List<String>();
        List<TextBox> actionBoxText = new List<TextBox>();
        string _culturePref = "es-ES";       
        //
        bool canDetect = true;
        bool scenesListed = false;
        bool obsConnected = false;
        //
        private System.Windows.Forms.Timer timer1;
        readonly int _time = 120;
        int _timeInterval = 1000;
        int _timeWorking = 0;
        //
        // 
        //
        public StreamSpeechAssistan()
        {
            InitializeComponent();            
            _obs = new OBSWebsocket();    

            _obs.Connected += onConnect;
            _obs.Disconnected += onDisconnect;

            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            btnListScenes.Enabled = false;

            textBoxMain.AppendText(Environment.NewLine + Environment.NewLine + "Welcome to SpeechStream Assistant." + Environment.NewLine + Environment.NewLine +
                                   "Please Launch OBS-Studio First." + Environment.NewLine + Environment.NewLine + Environment.NewLine + "and Just Talk and Enjoy." +
                                   Environment.NewLine + Environment.NewLine + Environment.NewLine);
        }
        private void Form1_Load(object sender, EventArgs e)
        {


            var enumerator = new MMDeviceEnumerator();
            foreach (var endpoint in
                     enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                comboxMicros.Items.Add(Convert.ToString(endpoint.FriendlyName));
            }
            comboxMicros.SelectedIndex = 0;

            label7.Text = "Time until next action: " + _time.ToString();
            _timeWorking = _time;
        }
        //
        //
        //
        #region Start Speech Recognition Engine
        private SpeechRecognitionEngine createSpeechEngine(string preferredCulture)
        {
            foreach (RecognizerInfo config in SpeechRecognitionEngine.InstalledRecognizers())
            {
                if (config.Culture.ToString() == preferredCulture)
                {
                    textBoxMain.AppendText(SpeechRecognitionEngine.InstalledRecognizers()[0].Culture.ToString() + Environment.NewLine);
                    speechRecognitionEngine = new SpeechRecognitionEngine(config);
                    break;
                }
            }

            // if the desired culture is not found, then load default
            if (speechRecognitionEngine == null)
            {
                textBoxMain.AppendText("The desired culture is not installed on this machine, the speech-engine will continue using " + Environment.NewLine
                    + SpeechRecognitionEngine.InstalledRecognizers()[0].Culture.ToString() + " as the default culture." + Environment.NewLine +
                    "Culture " + preferredCulture + " not found!" + Environment.NewLine);
                speechRecognitionEngine = new SpeechRecognitionEngine(SpeechRecognitionEngine.InstalledRecognizers()[0]);
            }

            return speechRecognitionEngine;
        }
        private void loadGrammarAndCommands()
        {
            try
            {
                Choices texts = new Choices();
                string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "\\example.txt");                
                foreach (string line in lines)
                {  
                }
                Grammar wordsList = new Grammar(new GrammarBuilder(texts));
                speechRecognitionEngine.LoadGrammar(wordsList);                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (canDetect)
            {
                canDetect = false;
                timer1 = new System.Windows.Forms.Timer();
                timer1.Tick += new EventHandler(timer1_Tick);
                timer1.Interval = _timeInterval; // 1 second
                timer1.Start();                
            }

        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            _timeWorking--;
            if (_timeWorking == 0)
            {
                timer1.Stop();
                canDetect = true;
                _timeWorking = _time;
            }
            label7.Text = "Time until next action: " + _timeWorking.ToString();
        }        
        void engine_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            progressBar1.Value = e.AudioLevel;
        }
        #endregion
        //
        //
        //
        #region General Buttons

        
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            try
            {
                // create the engine
                speechRecognitionEngine = createSpeechEngine(_culturePref);

                // hook to events
                speechRecognitionEngine.AudioLevelUpdated += new EventHandler<AudioLevelUpdatedEventArgs>(engine_AudioLevelUpdated);
                speechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(engine_SpeechRecognized);

                // load dictionary
                loadGrammarAndCommands();

                // use the system's default microphone
                speechRecognitionEngine.SetInputToDefaultAudioDevice();

                // start listening
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Voice recognition failed");
            }
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            textBoxMain.AppendText("Speech Engine Started. Now you can start Speaking..." + Environment.NewLine);
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            if (speechRecognitionEngine != null)
            {
                // unhook events
                speechRecognitionEngine.RecognizeAsyncStop();
                // clean references
                speechRecognitionEngine.Dispose();
            }
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            textBoxMain.AppendText("Speech Engine Stopped." + Environment.NewLine);
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            string path = Environment.CurrentDirectory + "\\Handles.txt";
            File.WriteAllText(path, String.Empty);

            this.Close();
        }


        #endregion
        //
        // 
        //
        //        
        #region Idiomas
        private void ButtonEN_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "en-US";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }

        private void ButtonDE_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "de-DE";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }

        private void ButtonFR_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "fr-FR";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }

        private void ButtonJP_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "jp-JP";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }

        private void ButtonCN_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "zh-CN";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }

        private void ButtonTW_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "zh-TW";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }
        private void ButtonES_Click(object sender, EventArgs e)
        {
            if (buttonStart.Enabled)
            {
                _culturePref = "es-ES";
                textBoxMain.AppendText("Your Selected Language is: " + _culturePref + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("To change Language Please Push STOP Button First.");
            }
        }
        #endregion
        //
        // 
        // 
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (speechRecognitionEngine != null && buttonStop.Enabled == true)
            {
                // unhook events
                speechRecognitionEngine.RecognizeAsyncStop();
                // clean references
                speechRecognitionEngine.Dispose();
                if (_obs.IsConnected)
                {
                    _obs.Disconnect();
                }
                    
            }
        }
        public void updateWords()   
        {
            words.Clear();

            foreach (TextBox textBoxes in actionBoxText)
            {
                if (textBoxes.Text != "NOT SET COMMAND")
                {
                    words.Add(textBoxes.Text);
                    textBoxMain.AppendText(textBoxes.Text + Environment.NewLine);
                }

            }
        }
        public void updateActionTextboxes()
        {
            actionBoxText.Clear();

            actionBoxText.Add(txtBoxMain);
            actionBoxText.Add(txtBoxAfk);
            actionBoxText.Add(txtBoxEnding);
            actionBoxText.Add(textBoxAction1);
            actionBoxText.Add(textBoxAction2);
            actionBoxText.Add(textBoxAction3);
            actionBoxText.Add(textBoxAction4);
            actionBoxText.Add(textBoxAction5);
            actionBoxText.Add(textBoxAction6);
            actionBoxText.Add(textBoxAction7);
            actionBoxText.Add(textBoxAction8);
            actionBoxText.Add(textBoxAction9);
            actionBoxText.Add(textBoxAction10);
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!_obs.IsConnected)
            {
                try
                {
                    _obs.Connect(url, password);                    
                }
                catch (AuthFailureException)
                {
                    MessageBox.Show("Authentication failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                catch (ErrorResponseException ex)
                {
                    MessageBox.Show("Connect failed : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            else
            {
                _obs.Disconnect();
            }
        }
        private void onConnect(object sender, EventArgs e)
        {
            btnConnect.Text = "Disconnect from OBS";
            var versionInfo = _obs.GetVersion();
            textBoxMain.AppendText("conectado a OBS" + Environment.NewLine + "OBS STUDIO VERSION: " + versionInfo.OBSStudioVersion + Environment.NewLine + "PLUGIN VERSION: " + versionInfo.PluginVersion + Environment.NewLine);
            btnListScenes.Enabled = true;
            obsConnected = true;
        }

        private void onDisconnect(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker)(() => {
                
                btnConnect.Text = "Connect to OBS";
                btnListScenes.Enabled = false;
                textBoxMain.AppendText("Disconnected from OBS" + Environment.NewLine);
                obsConnected = false;
            }));
        }

        private void btnListScenes_Click(object sender, EventArgs e)
        {
            var scenes = _obs.ListScenes();

            comboBoxAction1.Items.Clear();
            comboBoxAction2.Items.Clear();
            comboBoxAction3.Items.Clear();
            comboBoxAction4.Items.Clear();
            comboBoxAction5.Items.Clear();
            comboBoxAction6.Items.Clear();
            comboBoxAction7.Items.Clear();
            comboBoxAction8.Items.Clear();
            comboBoxAction9.Items.Clear();
            comboBoxAction10.Items.Clear();
            comboBoxMain.Items.Clear();
            comboBoxAfk.Items.Clear();
            comboBoxEnding.Items.Clear();

            foreach (var scene in scenes)
            {
                comboBoxAction1.Items.Add(scene.Name);
                comboBoxAction2.Items.Add(scene.Name);
                comboBoxAction3.Items.Add(scene.Name);
                comboBoxAction4.Items.Add(scene.Name);
                comboBoxAction5.Items.Add(scene.Name);
                comboBoxAction6.Items.Add(scene.Name);
                comboBoxAction7.Items.Add(scene.Name);
                comboBoxAction8.Items.Add(scene.Name);
                comboBoxAction9.Items.Add(scene.Name);
                comboBoxAction10.Items.Add(scene.Name);
                comboBoxMain.Items.Add(scene.Name);
                comboBoxAfk.Items.Add(scene.Name);
                comboBoxEnding.Items.Add(scene.Name);
            }

            scenesListed = true;            
        }
        
        #region ComboBoxes
        private void comboBoxAction1_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction1.Text = comboBoxAction1.Text;
        }
        private void comboBoxAction2_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction2.Text = comboBoxAction2.Text;
        }
        private void comboBoxAction3_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction3.Text = comboBoxAction3.Text;
        }
        private void comboBoxAction4_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction4.Text = comboBoxAction4.Text;
        }
        private void comboBoxAction5_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction5.Text = comboBoxAction5.Text;
        }
        private void comboBoxAction6_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction6.Text = comboBoxAction6.Text;
        }
        private void comboBoxAction7_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction7.Text = comboBoxAction7.Text;
        }
        private void comboBoxAction8_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction8.Text = comboBoxAction8.Text;
        }
        private void comboBoxAction9_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction9.Text = comboBoxAction9.Text;
        }
        private void comboBoxAction10_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAction10.Text = comboBoxAction10.Text;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnMain.Text = comboBoxMain.Text;
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAFK.Text = comboBoxAfk.Text;
        }
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnENDING.Text = comboBoxEnding.Text;
        }
        #endregion        
        
        private void btnMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: "+Environment.NewLine+"* Check Scene is selected First.");
                        } 
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else {
                MessageBox.Show("Please check First: "+Environment.NewLine+"* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }
        private void btnAFK_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }
        private void btnENDING_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction1_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction2_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction3_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction4_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction5_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction6_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction7_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction8_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction9_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }

        private void btnAction10_MouseDown(object sender, MouseEventArgs e)
        {
            if (obsConnected && scenesListed)
            {
                switch (e.Button)
                {

                    case MouseButtons.Left:
                        // Left click

                        try { _obs.SetCurrentScene((sender as Button).Text); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Scene not found: " + Environment.NewLine + "* Check Scene is selected First.");
                        }
                        break;

                    case MouseButtons.Right:
                        // Right click
                        // open file dialog   
                        OpenFileDialog open = new OpenFileDialog();
                        // image filters  
                        open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            (sender as Button).BackgroundImage = new Bitmap(open.FileName);
                        }
                        break;

                }
            }
            else
            {
                MessageBox.Show("Please check First: " + Environment.NewLine + "* OBS Studio is Running" + Environment.NewLine + "* Refresh Scene List");
            }
        }        

        #region Time Between Actions

        private void textBoxTime1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime5_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime6_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime7_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime8_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime9_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTime10_TextChanged(object sender, EventArgs e)
        {

        }

        #endregion

        private void btnSetCommand_Click(object sender, EventArgs e)
        {
            updateActionTextboxes();
            updateWords();
        }

        private void btnSetTime_Click(object sender, EventArgs e)
        {

        }
    }
}
