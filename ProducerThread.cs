using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P3_Producer
{
    internal class ProducerThread
    {
        private int ProducerId;
        private string ProducerIp;
        private int ProducerPort;
        private string FolderPath;

        public ProducerThread(int producerId, string producerIp, int producerPort, string folderPath)
        {
            ProducerId = producerId;
            ProducerIp = producerIp;
            ProducerPort = producerPort;
            FolderPath = folderPath;
        }

        // Forms TCP connection, Sends video file to the consumer via BinaryWriter
        // TODO: add video compression
        public static void UploadVideo(byte[] videoData, string consumerIp, int consumerPort)
        {
            try
            {
                using (TcpClient client = new TcpClient(consumerIp, consumerPort))
                using (NetworkStream stream = client.GetStream())  
                using (BinaryWriter writer = new BinaryWriter(stream))  
                {
                    writer.Write(videoData);  
                }
                Console.WriteLine("Video sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending video: {ex.Message}");
            }
        }

        // Reads file from "videos" directory, calls UploadVideo to send videos to selected consumer thread
        public void ExecuteUpload(string consumerIp, int consumerPort)
        {
            Console.WriteLine($"[Producer {ProducerId}] Thread executing, reading videos from folder {FolderPath}");

            // mp4 files for now idk if we need to add more, sorry ya boi doesn't listen in class
            string[] videoFiles = Directory.GetFiles("videos", "*.mp4");

            foreach (var file in videoFiles)
            {
                try
                {
                    // Read the video file into a byte array
                    byte[] videoData = File.ReadAllBytes(file);
                    Console.WriteLine($"[Producer {ProducerId}] Sending video {file} to {consumerIp}:{consumerPort}");

                    // Send the video data to the consumer via TCP
                    UploadVideo(videoData, consumerIp, consumerPort);
                }
                catch (Exception ex)
                {
                    // Handle any errors (e.g., file reading errors)
                    Console.WriteLine($"Error while reading or sending video {file}: {ex.Message}");
                }
            }
        }

        // Prompts user to place files in directory, asks which consumer to send to, then executes the upload.
        public void Start()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe")
            {
                // Run command to keep the window open
                Arguments = $"/K echo Producer {ProducerId} started. Press any key to close this window."
            };

            Process.Start(startInfo); // Start the command-line window

            Console.WriteLine($"[Producer {ProducerId}] Thread started, please place videos in folder {FolderPath}");
        }
    }
}
