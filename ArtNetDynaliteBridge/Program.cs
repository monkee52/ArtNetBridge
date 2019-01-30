using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ArtNetDynaliteBridge {
    class Program {
        static void Main(string[] args) {
            ArtNetNode client = new ArtNetNode();

            client.Start();

            ArtNetValueChangedEventHandler handler = (object sender, ArtNetValueChangedEventArgs e) => {
                ArtNetChannel channel = sender as ArtNetChannel;

                Console.WriteLine("Channel {0}.{1} went from {2} to {3}", channel.Universe.Universe, channel.Channel, e.OldValue, e.Value);

                //SetDynaliteChannel(channel.Channel, e.Value);
            };

            client.Universes[1].Channels[ 0].ValueChanged += handler;
            client.Universes[0].Channels[ 1].ValueChanged += handler;
            client.Universes[0].Channels[ 2].ValueChanged += handler;
            client.Universes[0].Channels[ 3].ValueChanged += handler;
            client.Universes[0].Channels[ 4].ValueChanged += handler;
            client.Universes[0].Channels[ 5].ValueChanged += handler;
            client.Universes[0].Channels[ 6].ValueChanged += handler;
            client.Universes[0].Channels[ 7].ValueChanged += handler;
            client.Universes[0].Channels[ 8].ValueChanged += handler;
            client.Universes[0].Channels[ 9].ValueChanged += handler;
            client.Universes[0].Channels[10].ValueChanged += handler;
            client.Universes[0].Channels[11].ValueChanged += handler;

            Pause();

            client.Stop();
        }

        private static void Pause() {
            Console.Write("Press any key to continue...");
            Console.ReadKey(true);
            Console.WriteLine();
        }

        private static void SetDynaliteChannel(int channel, byte value) {
            // CGI interface
            //using (WebClient client = new WebClient()) {
            //    string safeValue = ((int)Math.Max(0.0, Math.Min(100.0, value / 255.0 * 100.0))).ToString();

            //    client.DownloadDataAsync(new Uri(String.Format("http://192.168.1.100/SetDyNet.cgi?f=100&c={0}&l={1}", channel, safeValue)));
            //}

            // UDP interface
            // Scaling function because 0x01 = 100%, 0xff = 0%
            //byte channelValue = (byte)((~value) & 0xff | (~(value | (value - 1)) & 1));
            byte channelValue = (byte)(-(254 * value / 255) + 255);

            byte[] packet = new byte[] { 0x1c, 0x00, (byte)channel, 0x71, channelValue, 0x01, 0xff, 0xde };

            // packet[2] = AREA;

            // Calculate checksum
            packet[7] = (byte)(-((sbyte)packet.Take(7).Sum(x => x)));

            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Parse(""), 9998));

            client.SendAsync(packet, packet.Length).ContinueWith(_ => client.Close());
        }
    }
}
