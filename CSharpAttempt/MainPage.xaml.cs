﻿using System;
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
        int fingersDownCount = 0;
        int fingerSerialNumber = 0;

        Line[] clines = new Line[COLS];
        Line[] rlines = new Line[ROWS];

        uint[] fingerIdx = new uint[FINGERS];
        int fingerRadius = 40;
        DatagramSocket socket;
        DataWriter writer;
        byte[] oscBytes;

        long[] lastTickCount64 = new long[FINGERS];
        long[] nextTickCount64 = new long[FINGERS];

        public MainPage()
        {
            this.InitializeComponent();
            SolidColorBrush fingerOutlineColor = new SolidColorBrush(Colors.LightBlue);
            SolidColorBrush stringColor = new SolidColorBrush(Colors.Violet);
            SolidColorBrush fretColor = new SolidColorBrush(Colors.Navy);
            SolidColorBrush bigFretColor = new SolidColorBrush(Colors.Violet);
            SolidColorBrush fingerColor = new SolidColorBrush(Colors.DarkBlue);
            SolidColorBrush textColor = new SolidColorBrush(Colors.White);
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
            }
            for (int r = 0; r < ROWS; r++)
            {
                rlines[r] = new Line();
                this.canvas.Children.Add(rlines[r]);
                rlines[r].Stroke = stringColor;
                rlines[r].Visibility = Visibility.Visible;
            }
            for (int c = 0; c < COLS; c++)
            {
                clines[c] = new Line();
                this.canvas.Children.Add(clines[c]);
                clines[c].Stroke = (c%5 == 0) ? bigFretColor : fretColor;
                clines[c].Visibility = Visibility.Visible;
            }
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

        private float FingerFreq(PointerRoutedEventArgs e, int i)
        {
            double pitchx = (double)(COLS * (e.GetCurrentPoint(canvas).Position.X) / (canvas.ActualWidth * 12));
            double pitchy = (double)(ROWS - ROWS * (e.GetCurrentPoint(canvas).Position.Y) / canvas.ActualHeight);
            fingersPolyGroup[i] = (int)Math.Floor(pitchy);
            double pitchm = 12*pitchx + 5 * Math.Floor(pitchy) - 0.5;
            float pitch = (float)(27.5d * Math.Pow(2.0d, pitchm/12));
            return pitch;
        }

        private float FingerTimbre(PointerRoutedEventArgs e)
        {
            double pitchy = (double)(ROWS - ROWS * (e.GetCurrentPoint(canvas).Position.Y) / canvas.ActualHeight);
            return 1 - ((float)pitchy - (int)pitchy);
        }

        private int FingerDownHandler(PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                int i = IdxFingerPressed(e.Pointer.PointerId);
                if (i >= 0)
                {
                    nextTickCount64[i] = GetTickCount64();
                    float vol = 1.0f;
                    float pitch = FingerFreq(e,i);
                    float timbre = FingerTimbre(e);
                    fingersPitch[i] = pitch;
                    fingersTimbre[i] = timbre;
                    fingersVolume[i] = vol;
                    Point p = e.GetCurrentPoint(canvas).Position;
                    Canvas.SetLeft(fingers[i], p.X - fingerRadius);
                    Canvas.SetTop(fingers[i], p.Y - fingerRadius);
                    fingers[i].Visibility = Visibility.Visible;
                    fingerText[i].Visibility = Visibility.Visible;
                    Canvas.SetLeft(fingerText[i], p.X + 30);
                    Canvas.SetTop(fingerText[i], p.Y);
                    WriteOSCNote(i, vol, pitch, timbre);
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
                    float pitch = FingerFreq(e,i);
                    float timbre = FingerTimbre(e);
                    float vol = 1.0f;
                    fingersPitch[i] = pitch;
                    fingersTimbre[i] = timbre;
                    fingersVolume[i] = vol;
                    Point p = e.GetCurrentPoint(canvas).Position;
                    Canvas.SetLeft(fingers[i], p.X - fingerRadius);
                    Canvas.SetTop(fingers[i], p.Y - fingerRadius);
                    Canvas.SetLeft(fingerText[i], p.X + 30);
                    Canvas.SetTop(fingerText[i], p.Y);
                    WriteOSCNote(i, vol, pitch, timbre);
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
