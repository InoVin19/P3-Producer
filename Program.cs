using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace P3_Producer
{
    public class Config
    {
        public int ProducerCount { get; set; }
        public int ConsumerCount { get; set; }
        public int QueueLimit { get; set; }
        public List<(string ip, int basePort)> ConsumerEndpoints { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Producer Application Starting ===");
            
            // Read configuration
            Config config = ReadConfig();
            Console.WriteLine($"Configuration loaded: {config.ProducerCount} producers, {config.ConsumerCount} consumers, Queue limit: {config.QueueLimit}");
            
            // Generate consumer endpoints based on the configuration
            List<(string ip, int port)> consumerEndpoints = GenerateConsumerEndpoints(config);
            
            Console.WriteLine($"Generated {consumerEndpoints.Count} consumer endpoints:");
            foreach (var endpoint in consumerEndpoints)
            {
                Console.WriteLine($"  - {endpoint.ip}:{endpoint.port}");
            }
            
            // Create and start producer threads
            List<ProducerThread> producers = new List<ProducerThread>();
            
            for (int i = 0; i < config.ProducerCount; i++)
            {
                int producerId = i + 1;
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), $"producer_{producerId}");
                
                // Create a producer with a single assigned consumer endpoint
                ProducerThread producer = new ProducerThread(producerId, folderPath, config.QueueLimit);
                
                // Assign a specific consumer endpoint to this producer
                if (i < consumerEndpoints.Count)
                {
                    // Each producer gets exactly one consumer endpoint
                    List<(string ip, int port)> producerEndpoint = new List<(string ip, int port)> { consumerEndpoints[i] };
                    producer.SetConsumerEndpoints(producerEndpoint);
                    Console.WriteLine($"Starting producer {producerId} with folder {folderPath} assigned to consumer at {consumerEndpoints[i].ip}:{consumerEndpoints[i].port}");
                }
                else
                {
                    // If we have more producers than consumers, we'll need to reuse consumer endpoints
                    int consumerIndex = i % consumerEndpoints.Count;
                    List<(string ip, int port)> producerEndpoint = new List<(string ip, int port)> { consumerEndpoints[consumerIndex] };
                    producer.SetConsumerEndpoints(producerEndpoint);
                    Console.WriteLine($"Starting producer {producerId} with folder {folderPath} assigned to consumer at {consumerEndpoints[consumerIndex].ip}:{consumerEndpoints[consumerIndex].port}");
                }
                
                producers.Add(producer);
            }
            
            Console.WriteLine($"All {producers.Count} producers initialized");
            Console.WriteLine("Press Enter to trigger all producers to upload videos or type 'q' and press Enter to quit");
            
            // Main loop for user input using Console.Read instead of Console.ReadKey
            while (true)
            {
                try
                {
                    // Read a character from the console
                    int input = Console.Read();
                    
                    // Check if it's a newline (Enter key)
                    if (input == 10 || input == 13)
                    {
                        Console.WriteLine("Triggering all producers to upload videos in parallel...");
                        
                        // Create a list to hold all the upload tasks
                        List<Task> uploadTasks = new List<Task>();
                        
                        // Trigger all producers to upload videos in parallel
                        foreach (var producer in producers)
                        {
                            // Create a task for each producer's upload operation
                            Task uploadTask = Task.Run(() => producer.ExecuteUpload());
                            uploadTasks.Add(uploadTask);
                        }
                        
                        // Wait for all uploads to complete
                        await Task.WhenAll(uploadTasks);
                        
                        Console.WriteLine("All uploads completed. Press Enter to trigger again or type 'q' and press Enter to quit");
                    }
                    // Check if it's 'q' or 'Q'
                    else if (input == 'q' || input == 'Q')
                    {
                        Console.WriteLine("Exiting application...");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }
        }
        
        static Config ReadConfig()
        {
            // Default configuration
            Config config = new Config
            {
                ProducerCount = 2,
                ConsumerCount = 2,
                QueueLimit = 10,
                ConsumerEndpoints = new List<(string ip, int basePort)> { ("localhost", 9000) }
            };
            
            try
            {
                // Try to read from config file
                if (File.Exists("config.txt"))
                {
                    string[] lines = File.ReadAllLines("config.txt");
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            
                            switch (key.ToLower())
                            {
                                case "p":
                                    config.ProducerCount = int.Parse(value);
                                    break;
                                case "c":
                                    config.ConsumerCount = int.Parse(value);
                                    break;
                                case "q":
                                    config.QueueLimit = int.Parse(value);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // Create a default config file if it doesn't exist
                    using (StreamWriter writer = new StreamWriter("config.txt"))
                    {
                        writer.WriteLine("p=2");
                        writer.WriteLine("c=2");
                        writer.WriteLine("q=10");
                    }
                    Console.WriteLine("Created default config.txt file");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading config: {ex.Message}");
                Console.WriteLine("Using default configuration");
            }
            
            return config;
        }
        
        static List<string> GenerateProducerFolders(int count)
        {
            List<string> folders = new List<string>();
            
            for (int i = 0; i < count; i++)
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), $"producer_{i + 1}");
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Console.WriteLine($"Created producer folder: {folderPath}");
                }
                
                folders.Add(folderPath);
            }
            
            return folders;
        }
        
        static List<(string ip, int port)> GenerateConsumerEndpoints(Config config)
        {
            List<(string ip, int port)> endpoints = new List<(string ip, int port)>();
            
            foreach (var baseEndpoint in config.ConsumerEndpoints)
            {
                for (int i = 0; i < config.ConsumerCount; i++)
                {
                    // Each consumer gets its own port, starting from the base port
                    endpoints.Add((baseEndpoint.ip, baseEndpoint.basePort + i));
                }
            }
            
            return endpoints;
        }
    }
}
