using AydenIO.ArtNet.Node;
using System;
using System.Text;

namespace AydenIO.Examples.ArtNet.Node {
    class Program {
        static void Main(string[] args) {
            int[] listenTo = new[] { 0 };

            ArtNetNode node = new ArtNetNode(allowedUniverses: listenTo);
            
            foreach (int universeIndex in listenTo) {
                node.Universes[universeIndex].UniverseUpdated += HandleUniverseUpdated;
            }

            node.Start();
            
            Console.ReadKey();

            node.Stop();
            node.Dispose();
        }

        private static DateTime lastWrite = DateTime.MinValue;
        private static readonly TimeSpan writeInterval = TimeSpan.FromSeconds(1);

        private static void HandleUniverseUpdated(object sender, EventArgs e) {
            DateTime now = DateTime.UtcNow;

            if (now - Program.lastWrite >= writeInterval) {
                ArtNetUniverse universe = (ArtNetUniverse)sender;

                StringBuilder str = new StringBuilder();

                str.AppendLine($"Universe {universe.UniverseId}:");

                for (int i = 0; i < universe.Channels.Count; i++) {
                    str.AppendFormat("{0:X2} ", universe.Channels[i].Value);
                }

                Console.WriteLine(str.ToString());

                Program.lastWrite = now;
            }
        }
    }
}
