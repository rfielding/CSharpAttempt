using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CSharpAttempt
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DispatcherTimer moveTimer = new DispatcherTimer();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern long GetTickCount64();

        const uint ROWS = 10;
        const uint COLS = 25;
        const uint NONE = 0xffffff;
        const uint FINGERS = 16;
        TextBlock[] fingerText = new TextBlock[FINGERS];
        Ellipse[] fingers = new Ellipse[FINGERS];
        float[] fingersPitch = new float[FINGERS];
        float[] fingersTimbre = new float[FINGERS];
        float[] fingersVolume = new float[FINGERS];
        int[] fingersPolyGroup = new int[FINGERS];
        int[] fingerSerial = new int[FINGERS];
        bool[] fingerIsSilent = new bool[FINGERS];
        float[] fingerOffBy = new float[FINGERS];
        float[] fingerDrift = new float[FINGERS];
        int fingersDownCount = 0;
        int fingerSerialNumber = 1;

        Line[] clines = new Line[COLS];
        Line[] rlines = new Line[ROWS];
        Ellipse[][] rmarkers = new Ellipse[ROWS][];

        uint[] fingerIdx = new uint[FINGERS];
        int fingerRadius = 40;
        DatagramSocket socket;
        DataWriter writer;

        long[] lastTickCount64 = new long[FINGERS];
        long[] nextTickCount64 = new long[FINGERS];

        SolidColorBrush fingerOutlineColor = new SolidColorBrush(Colors.LightBlue);
        SolidColorBrush stringColor = new SolidColorBrush(Colors.Violet);
        SolidColorBrush dotColor = new SolidColorBrush(Colors.Navy);
        SolidColorBrush fretColor = new SolidColorBrush(Colors.Gray);
        SolidColorBrush fingerColor = new SolidColorBrush(Colors.DarkBlue);
        SolidColorBrush fingerSilenceColor = new SolidColorBrush(Colors.Red);
        SolidColorBrush textColor = new SolidColorBrush(Colors.White);


        public MainPage()
        {
            this.InitializeComponent();
            for (int i = 0; i < FINGERS; i++)
            {
                fingers[i] = new Ellipse();
                this.canvas.Children.Add(fingers[i]);
                fingers[i].Width = fingerRadius * 2;
                fingers[i].Height = fingerRadius * 2;
                fingers[i].Fill = fingerColor;
                fingers[i].Stroke = fingerOutlineColor;
                Canvas.SetLeft(fingers[i], i * 60);
                Canvas.SetTop(fingers[i], 120);
                fingers[i].Visibility = Visibility.Collapsed;
                fingerIdx[i] = NONE;

                fingerText[i] = new TextBlock();
                this.canvas.Children.Add(fingerText[i]);
                fingerText[i].Foreground = textColor;
                fingerText[i].Visibility = Visibility.Collapsed;
                fingersPolyGroup[i] = -1;
                fingerSerial[i] = -1;
            }
            for (int r = 0; r < ROWS; r++)
            {
                rlines[r] = new Line();
                this.canvas.Children.Add(rlines[r]);
                rlines[r].Stroke = stringColor;
                rlines[r].Visibility = Visibility.Visible;
                rmarkers[r] = new Ellipse[COLS];
            }
            for (int c = 0; c < COLS; c++)
            {
                clines[c] = new Line();
                this.canvas.Children.Add(clines[c]);
                clines[c].Stroke = fretColor;
                clines[c].Visibility = Visibility.Visible;
            }
            SolidColorBrush[] noteBrush = new SolidColorBrush[12];
            noteBrush[0] = new SolidColorBrush(Colors.Red);
            noteBrush[1] = new SolidColorBrush(Colors.Black);
            noteBrush[2] = new SolidColorBrush(Colors.Blue);
            noteBrush[3] = new SolidColorBrush(Colors.LightBlue);
            noteBrush[4] = new SolidColorBrush(Colors.Black);
            noteBrush[5] = new SolidColorBrush(Colors.Purple);
            noteBrush[6] = new SolidColorBrush(Colors.Black);
            noteBrush[7] = new SolidColorBrush(Colors.DarkBlue);
            noteBrush[8] = new SolidColorBrush(Colors.Turquoise);
            noteBrush[9] = new SolidColorBrush(Colors.Green);
            noteBrush[10] = new SolidColorBrush(Colors.DarkMagenta);
            noteBrush[11] = new SolidColorBrush(Colors.Black);

            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    rmarkers[r][c] = new Ellipse();
                    this.canvas.Children.Add(rmarkers[r][c]);
                    int note = (int) ((ROWS - r - 1) * 5 + c)%12;
                    bool inScale = 
                        (note == 0) ||
                        (note == 2) ||
                        (note == 3) ||
                        (note == 5) ||
                        (note == 7) ||
                        (note == 8) ||
                        (note == 10) 
                    ;

                    rmarkers[r][c].Visibility = inScale ? Visibility.Visible : Visibility.Collapsed;
                    rmarkers[r][c].Fill = noteBrush[note];
                    rmarkers[r][c].Width = fingerRadius * 0.5;
                    rmarkers[r][c].Height = fingerRadius * 0.5;
                }
            }

            moveTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            moveTimer.Tick += moveTimer_Tick;
        }

        void moveTimer_Tick(object sender, object e)
        {
            //FingerDownOrMoveHandler.for (int f = 0; f < FINGERS; f++)
            //{
            //    FingerDownOrMoveHandler();
            //}
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private int IdxFingerPressed(uint pointerId)
        {
            for (int i = 0; i < FINGERS; i++)
            {
                if(fingerIdx[i] == NONE)
                {
                    fingerIdx[i] = pointerId;
                    fingerSerial[i] = fingerSerialNumber;
                    fingersDownCount++;
                    fingerSerialNumber++;
                    return i;
                }
            }
            return -1;
        }

        private int IdxOfMaxSerialNumberInPolyGroup(int polyGroup)
        {
            int serial = 0;
            int serialIdx = -1;
            for (int i = 0; i < FINGERS; i++)
            {
                if (fingersPolyGroup[i] == polyGroup)
                {
                    if (serial < fingerSerial[i])
                    {
                        serial = fingerSerial[i];
                        serialIdx = i;
                    }
                }
            }
            return serialIdx;
        }

        private int IdxFingerMoved(uint pointerId)
        {
            for (int i = 0; i < FINGERS; i++)
            {
                if (fingerIdx[i] == pointerId)
                {
                    return i;
                }
            }
            return -1;
        }

        private int IdxFingerReleased(uint pointerId)
        {
            for (int i = 0; i < FINGERS; i++)
            {
                if (fingerIdx[i] == pointerId)
                {
                    fingerIdx[i] = NONE;
                    fingersDownCount--;
                    return i;
                }
            }
            return -1;
        }

        private float FingerFreq(float fx, float fy, int i)
        {
            double pitchx = (double)(COLS * fx / (canvas.ActualWidth * 12));
            double pitchy = (double)(ROWS - ROWS * fy / canvas.ActualHeight);
            fingersPolyGroup[i] = (int)Math.Floor(pitchy);
            double pitchm = 12*pitchx + 5 * Math.Floor(pitchy) - 0.5;
            //in *pixels*!
            fingerOffBy[i] = (float) (((pitchm-0.5) - (int)(pitchm-0.5)) - 0.5f) * (float)((canvas.ActualWidth/(COLS)));

            float pitch = (float)(27.5d * Math.Pow(2.0d, pitchm/12));
            return pitch;
        }

        private float FingerTimbre(PointerRoutedEventArgs e)
        {
            double pitchy = (double)(ROWS - ROWS * (e.GetCurrentPoint(canvas).Position.Y) / canvas.ActualHeight);
            return 1 - ((float)pitchy - (int)pitchy);
        }

        private void SilenceNotes(int i)
        {
            //All notes in i's poly group need to be silenced if they are not already
            for (int f = 0; f < FINGERS; f++)
            {
                if (fingersVolume[f] > 0 && fingersPolyGroup[i] == fingersPolyGroup[f] && f != i)
                {
                    fingersVolume[f] = 0;
                    WriteOSCNote(f, fingersVolume[f], fingersPitch[f], fingersTimbre[f]);
                }
            }
        }

        private void AwakenNote(int i)
        {
            //Find the note in our poly group that has the highest serial number below ours, and turn it on
            int maxSerialIdx = -1;
            for (int f = 0; f < FINGERS; f++)
            {
                //It's in our group, and it's not us, and it's silenced
                if (fingersPolyGroup[i] == fingersPolyGroup[f] && f != i && fingersVolume[f] <= 0)
                {
                    //Initially pick the first one
                    if(maxSerialIdx == -1)
                    {
                        maxSerialIdx = f;
                    }
                    else
                    {
                        //If this one is better, then pick that instead
                        if(fingerSerial[f] > fingerSerial[maxSerialIdx] && fingerSerial[f] < fingerSerial[i])
                        {
                            maxSerialIdx = f;
                        }
                    }
                }
            }
            //If there is a note to turn on, then do so
            if (maxSerialIdx >= 0)
            {
                fingersVolume[maxSerialIdx] = 1.0f;  //TODO: when we have more gestural control, we will need to remember the volume it was silenced at
                WriteOSCNote(maxSerialIdx, fingersVolume[maxSerialIdx], fingersPitch[maxSerialIdx], fingersTimbre[maxSerialIdx]);
            }
        }

        /**
         * Set volume before coming in here
         */
        private void FingerDownOrMoveHandler(float fx, float fy, float timbre, int i)
        {
            float pitch = FingerFreq(fx, fy, i);
            fingersPitch[i] = pitch;
            fingersTimbre[i] = timbre;
            fingers[i].Visibility = Visibility.Visible;
            fingerDrift[i] = -fingerOffBy[i] * 0.5f;

            if (IdxOfMaxSerialNumberInPolyGroup(fingersPolyGroup[i]) == i)
            {
                fingers[i].Width = fingerRadius * 2;
                fingers[i].Height = fingerRadius * 2;
                fingers[i].Fill = fingerColor;
            }
            else
            {
                fingers[i].Width = fingerRadius * 1.5;
                fingers[i].Height = fingerRadius * 1.5;
                fingers[i].Fill = fingerSilenceColor;
            }
            Canvas.SetLeft(fingers[i], fx - fingers[i].Width / 2);
            Canvas.SetTop(fingers[i], fy - fingers[i].Height / 2);

            //Canvas.SetLeft(fingerText[i], fx + 30);
            //Canvas.SetTop(fingerText[i], fy);
            //fingerText[i].Visibility = Visibility.Visible;
            //fingerText[i].Text = "" + fingersPolyGroup[i];

        }

        private void FingerDownOrMoveHandler(PointerRoutedEventArgs e, int i)
        {
            float fx = (float)(e.GetCurrentPoint(canvas).Position.X) + fingerDrift[i];
            float fy = (float)(e.GetCurrentPoint(canvas).Position.Y);
            float timbre = FingerTimbre(e);
            FingerDownOrMoveHandler(fx, fy, timbre, i);
        }

        private int FingerDownHandler(PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                int i = IdxFingerPressed(e.Pointer.PointerId);
                if (i >= 0)
                {

                    nextTickCount64[i] = GetTickCount64();
                    fingersVolume[i] = 1.0f;
                    FingerDownOrMoveHandler(e,i);

                    SilenceNotes(i);

                    WriteOSCNote(i, fingersVolume[i], fingersPitch[i], fingersTimbre[i]);

                }
                return i;
            }
            return -1;
        }


        private int FingerMoveHandler(PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                int i = IdxFingerMoved(e.Pointer.PointerId);
                if (i >= 0)
                {
                    FingerDownOrMoveHandler(e, i);
                    WriteOSCNote(i, fingersVolume[i], fingersPitch[i], fingersTimbre[i]);

                }
                return i;
            }
            return -1;
        }

        private int FingerUpHandler(PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                int i = IdxFingerReleased(e.Pointer.PointerId);
                if (i >= 0)
                {
                    nextTickCount64[i] = GetTickCount64();
                    fingers[i].Visibility = Visibility.Collapsed;
                    fingerText[i].Visibility = Visibility.Collapsed;
                    fingersVolume[i] = 0.0f;
                    WriteOSCNote(i, 0.0f, fingersPitch[i], fingersTimbre[i]);
                    AwakenNote(i);
                    fingerSerial[i] = -1;
                    fingersPolyGroup[i] = -1;
                    if (fingersDownCount == 0)
                    {
                        //Turn off all notes...we know that there are no notes down.
                        WriteOSCNote(-69, 0.0f, 0.0f, 0.0f);
                    }
                }
                return i;
            }
            return -1;
        }

        private void CanvasPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            int i = FingerDownHandler(e);
        }


        private void CanvasPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            int i = IdxFingerMoved(e.Pointer.PointerId);
            if (i >= 0)
            {
                nextTickCount64[i] = GetTickCount64();
                if (nextTickCount64[i] - lastTickCount64[i] > fingersDownCount*5)
                {
                    FingerMoveHandler(e);
                    lastTickCount64[i] = nextTickCount64[i];
                }
            }
        }

        private void CanvasPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FingerUpHandler(e);
        }

        
        private void CanvasPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            FingerUpHandler(e);
        }

        private void CanvasPointerExited(object sender, PointerRoutedEventArgs e)
        {
            FingerUpHandler(e);
        }

        private async void ConnectToOSC_Click(object sender, RoutedEventArgs e)
        {
            socket = new DatagramSocket();
            socket.MessageReceived += RecieveOSC;
            await socket.ConnectAsync(new HostName(host.Text),port.Text);
            writer = new DataWriter(socket.OutputStream);
        }

        //Negative voices are controls
        private async void WriteOSCNote(int voice, float vol, float freq, float timbre)
        {
            if (writer != null)
            {
                int timediff = 0;

                if (voice >= 0)
                {
                    timediff = (int)nextTickCount64[voice];
                }
                writer.ByteOrder = ByteOrder.BigEndian;
                writer.WriteString("/rjf/p"); //4
                writer.WriteByte(0);
                writer.WriteByte(0);
                writer.WriteString(",iifff"); //4
                writer.WriteByte(0);
                writer.WriteByte(0);
                writer.WriteInt32(timediff);       //4
                writer.WriteInt32(voice);       //4
                writer.WriteSingle(vol);   //4
                writer.WriteSingle(freq);    //4
                writer.WriteSingle(timbre);
                await writer.StoreAsync();
            }
        }


        private void RecieveOSC(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void CanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double w = e.NewSize.Width;
            double h = e.NewSize.Height;
            for (int r = 0; r < ROWS; r++)
            {
                rlines[r].X1 = 0;
                rlines[r].X2 = w;
                rlines[r].Y1 = rlines[r].Y2 = ((r * 1.0 + 0.5) / ROWS) * h;
            }
            for (int c = 0; c < COLS; c++)
            {
                clines[c].Y1 = 0;
                clines[c].Y2 = h;
                clines[c].X1 = clines[c].X2 = ((c * 1.0 + 0.5) / COLS) * w;
            }
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    Canvas.SetLeft(rmarkers[r][c], ((c * 1.0 + 0.5) / COLS) * w - rmarkers[r][c].Width/2);
                    Canvas.SetTop(rmarkers[r][c],  ((r * 1.0 + 0.5) / ROWS) * h - rmarkers[r][c].Height/2);
                }
            }
        }

        private void VolSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (volume != null)
            {
                WriteOSCNote(-1, (float)volume.Value, 0, 0);
            }
        }

        private void OctSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (volume != null)
            {
                WriteOSCNote(-2, (float)octave.Value, 0, 0);
            }
        }

        private void RevSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (reverb != null)
            {
                WriteOSCNote(-3, (float)reverb.Value, 0, 0);
            }
        }

        private void ChFSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (chorusFreq != null)
            {
                WriteOSCNote(-4, (float)chorusFreq.Value, 0, 0);
            }
        }

        private void ChDSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (chorusDepth != null)
            {
                WriteOSCNote(-5, (float)chorusDepth.Value, 0, 0);
            }
        }

        private void ChMSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (chorusMix != null)
            {
                WriteOSCNote(-6, (float)chorusMix.Value, 0, 0);
            }
        }
    }
}
