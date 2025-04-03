namespace P3_Producer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string InputDir = "input.txt";
            var input = InputReader.ReadInput(InputDir);
            int p = input.Item1;  // Number of producer threads
            string consumerIp = "127.0.0.1";  // Consumer's IP address
            int consumerPort = 5000;  // Consumer's port

            // Create the directories for each producer thread
            for (int i = 0; i < p; i++)
            {
                string folderPath = $"videos_{i + 1}";  // Directory name for each producer thread
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);  // Create the directory for each thread
                }

                var producerThread = new ProducerThread(i + 1, consumerIp, consumerPort, folderPath);
                Thread thread = new Thread(producerThread.Start);
                thread.Start(); // Start the producer thread
            }
        }
    }
}
