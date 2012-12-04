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
        const uint NONE = 0xffffff;
        const uint FINGERS = 16;
        Ellipse[] fingers = new Ellipse[FINGERS];
        uint[] fingerIdx = new uint[FINGERS];
        int fingerRadius = 40;
        DatagramSocket socket;
        DataWriter writer;
        byte[] oscBytes;

        public MainPage()
        {
            this.InitializeComponent();
            SolidColorBrush fingerColor = new SolidColorBrush(Colors.DarkBlue);
            for (int i = 0; i < FINGERS; i++)
            {
                fingers[i] = new Ellipse();
                this.canvas.Children.Add(fingers[i]);
                fingers[i].Width = fingerRadius*2;
                fingers[i].Height = fingerRadius*2;
                fingers[i].Fill = fingerColor;
                Canvas.SetLeft(fingers[i], i*60);
                Canvas.SetTop(fingers[i], 120);
                fingers[i].Visibility = Visibility.Collapsed;
                fingerIdx[i] = NONE;
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
                    return i;
                }
            }
            return -1;
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
                    return i;
                }
            }
            return -1;
        }

        private int FingerDownHandler(PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                int i = IdxFingerPressed(e.Pointer.PointerId);
                if (i >= 0)
                {
                    Point p = e.GetCurrentPoint(this).Position;
                    Canvas.SetLeft(fingers[i], p.X - fingerRadius);
                    Canvas.SetTop(fingers[i], p.Y - fingerRadius);
                    fingers[i].Visibility = Visibility.Visible;
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
                    Point p = e.GetCurrentPoint(this).Position;
                    Canvas.SetLeft(fingers[i], p.X - fingerRadius);
                    Canvas.SetTop(fingers[i], p.Y - fingerRadius);
                    fingers[i].Visibility = Visibility.Visible;
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
                    fingers[i].Visibility = Visibility.Collapsed;
                }
                return i;
            }
            return -1;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            int i = FingerDownHandler(e);
            if (i >= 0)
            {
                WriteOSC(i, 1.0f, 400.0f);
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            FingerMoveHandler(e);
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FingerUpHandler(e);
        }

        private void OnPointerCancelled(object sender, PointerRoutedEventArgs e)
        {
            FingerUpHandler(e);
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            FingerUpHandler(e);
        }

        private async void ConnectToOSC_Click(object sender, RoutedEventArgs e)
        {
            socket = new DatagramSocket();
            socket.MessageReceived += RecieveOSC;
            await socket.ConnectAsync(new HostName("127.0.0.1"),"1973");
            writer = new DataWriter(socket.OutputStream);
        }

        private async void WriteOSC(int voice, float vol, float freq)
        {
            if (writer != null)
            {
                writer.ByteOrder = ByteOrder.BigEndian;
                writer.WriteString("/rjf"); //5
                writer.WriteString(",iff"); //5
                writer.WriteInt32(voice);       //4
                writer.WriteSingle(vol);   //4
                writer.WriteSingle(freq);    //4
                writer.WriteByte(0);        //2
                writer.WriteByte(0);
                await writer.StoreAsync();
            }
            //await writer.StoreAsync();
        }

        private void RecieveOSC(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            throw new NotImplementedException();
        }

    }
}
