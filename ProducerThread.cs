using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Text.Json;

namespace P3_Producer
{
    public class VideoMetadata
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ProducerThread
    {
        private readonly int ProducerId;
        private readonly string FolderPath;
        private readonly int QueueLimit;
        private List<(string ip, int port)> ConsumerEndpoints;
        
        // Constructor
        public ProducerThread(int producerId, string folderPath, int queueLimit)
        {
            ProducerId = producerId;
            FolderPath = folderPath;
            QueueLimit = queueLimit;
            ConsumerEndpoints = new List<(string ip, int port)>();
            
            // Create producer folder if it doesn't exist
            Directory.CreateDirectory(folderPath);
        }

        // Sets consumer endpoints for dynamic allocation
        public void SetConsumerEndpoints(List<(string ip, int port)> endpoints)
        {
            ConsumerEndpoints = endpoints;
            Console.WriteLine($"[Producer {ProducerId}] Set {endpoints.Count} consumer endpoints");
            
            // Debug: Print all endpoints
            for (int i = 0; i < endpoints.Count; i++)
            {
                Console.WriteLine($"[Producer {ProducerId}] Endpoint {i}: {endpoints[i].ip}:{endpoints[i].port}");
            }
        }

        // Forms TCP connection, Sends video file to the consumer via BinaryWriter
        public void UploadVideo(byte[] videoData, string videoFileName, string consumerIp, int consumerPort)
        {
            try
            {
                Console.WriteLine($"[Producer {ProducerId}] Connecting to consumer at {consumerIp}:{consumerPort}...");
                
                using (TcpClient client = new TcpClient(consumerIp, consumerPort))
                using (NetworkStream stream = client.GetStream())  
                using (BinaryWriter writer = new BinaryWriter(stream))  
                {
                    // Create metadata
                    var metadata = new VideoMetadata
                    {
                        FileName = videoFileName,
                        FileSize = videoData.Length,
                        ContentType = "video/mp4",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    // Serialize metadata to JSON
                    string metadataJson = JsonSerializer.Serialize(metadata);
                    
                    // Write metadata length and content
                    writer.Write(metadataJson.Length);
                    writer.Write(Encoding.UTF8.GetBytes(metadataJson));
                    
                    // Write video data length and content
                    writer.Write(videoData.Length);
                    writer.Write(videoData);
                    
                    Console.WriteLine($"[Producer {ProducerId}] Video {videoFileName} sent successfully to {consumerIp}:{consumerPort}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Producer {ProducerId}] Error while sending video: {ex.Message}");
            }
        }

        // Get the assigned consumer endpoint
        private (string ip, int port) GetConsumerEndpoint()
        {
            if (ConsumerEndpoints.Count == 0)
            {
                throw new InvalidOperationException("No consumer endpoints available");
            }
            
            // Since each producer is now assigned to exactly one consumer endpoint, just return the first one
            var endpoint = ConsumerEndpoints[0];
            Console.WriteLine($"[Producer {ProducerId}] Using assigned consumer at {endpoint.ip}:{endpoint.port}");
            
            return endpoint;
        }

        // Reads files from folder, calls UploadVideo to send videos to selected consumer thread
        public void ExecuteUpload()
        {
            Console.WriteLine($"[Producer {ProducerId}] Executing upload from folder {FolderPath}");

            string[] videoFiles = Directory.GetFiles(FolderPath, "*.mp4");
            
            if (videoFiles.Length == 0)
            {
                Console.WriteLine($"[Producer {ProducerId}] No videos found in folder {FolderPath}");
                return;
            }

            Console.WriteLine($"[Producer {ProducerId}] Found {videoFiles.Length} videos to upload");

            foreach (var file in videoFiles)
            {
                try
                {
                    // Read the video file into a byte array
                    byte[] videoData = File.ReadAllBytes(file);
                    string fileName = Path.GetFileName(file);
                    
                    // Get the assigned consumer endpoint
                    var (consumerIp, consumerPort) = GetConsumerEndpoint();
                    
                    Console.WriteLine($"[Producer {ProducerId}] Uploading video {fileName} ({videoData.Length / 1024} KB) to {consumerIp}:{consumerPort}");

                    // Send the video data to the consumer via TCP
                    UploadVideo(videoData, fileName, consumerIp, consumerPort);
                    
                    // Move the file to a processed folder to avoid re-uploading
                    string processedFolder = Path.Combine(FolderPath, "processed");
                    Directory.CreateDirectory(processedFolder);
                    File.Move(file, Path.Combine(processedFolder, fileName), true);
                }
                catch (Exception ex)
                {
                    // Handle any errors (e.g., file reading errors)
                    Console.WriteLine($"[Producer {ProducerId}] Error while reading or sending video {file}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"[Producer {ProducerId}] Upload completed");
        }

        // Executes the upload process asynchronously
        public async Task ExecuteUploadAsync()
        {
            Console.WriteLine($"[Producer {ProducerId}] Executing upload from folder {FolderPath}");
            
            // Find all video files in the folder
            string[] videoFiles = Directory.GetFiles(FolderPath, "*.mp4");
            
            if (videoFiles.Length == 0)
            {
                Console.WriteLine($"[Producer {ProducerId}] No videos found in folder {FolderPath}");
                return;
            }
            
            Console.WriteLine($"[Producer {ProducerId}] Found {videoFiles.Length} videos to upload");
            
            // Process each video file
            foreach (string filePath in videoFiles)
            {
                try
                {
                    // Read the video file into a byte array
                    byte[] videoData = await File.ReadAllBytesAsync(filePath);
                    string fileName = Path.GetFileName(filePath);
                    
                    // Get the assigned consumer endpoint
                    var (consumerIp, consumerPort) = GetConsumerEndpoint();
                    
                    Console.WriteLine($"[Producer {ProducerId}] Uploading video {fileName} ({videoData.Length / 1024} KB) to {consumerIp}:{consumerPort}");
                    
                    // Send the video data to the consumer via TCP
                    await Task.Run(() => UploadVideo(videoData, fileName, consumerIp, consumerPort));
                    
                    // Move the file to a processed folder to avoid re-uploading
                    string processedFolder = Path.Combine(FolderPath, "processed");
                    Directory.CreateDirectory(processedFolder);
                    File.Move(filePath, Path.Combine(processedFolder, fileName), true);
                    
                    Console.WriteLine($"[Producer {ProducerId}] Successfully uploaded and moved {fileName} to processed folder");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Producer {ProducerId}] Error processing video {filePath}: {ex.Message}");
                }
            }
        }

        // Starts the producer thread, processes videos periodically
        public async Task Start()
        {
            Console.WriteLine($"[Producer {ProducerId}] Thread started, monitoring folder {FolderPath}");
            
            // Create a timer to periodically check for user input via a file
            string triggerFile = Path.Combine(FolderPath, "trigger.txt");
            
            // Main processing loop
            while (true)
            {
                // Check if trigger file exists
                if (File.Exists(triggerFile))
                {
                    Console.WriteLine($"[Producer {ProducerId}] Trigger file detected, processing videos from {FolderPath}");
                    
                    // Execute upload
                    await ExecuteUploadAsync();
                    
                    // Delete the trigger file after processing
                    try
                    {
                        File.Delete(triggerFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Producer {ProducerId}] Error deleting trigger file: {ex.Message}");
                    }
                }
                
                // Small delay to prevent CPU hogging
                await Task.Delay(500);
            }
        }
    }
}
