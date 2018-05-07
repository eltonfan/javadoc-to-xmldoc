using System;
using System.Linq;

namespace Elton.CommentConvert
{
    class Program
    {
        static void Main(string[] args)
        {
            var basePath = args?.FirstOrDefault();
            if (string.IsNullOrEmpty(basePath))
            {
                Console.WriteLine("Input path is empty.");
                return;
            }
            if (!System.IO.Directory.Exists(basePath))
            {
                Console.WriteLine($"Input path '{basePath}' is not exists.");
                return;
            }

            try
            {
                var formatter = new CommentFormatter(basePath);
                while (true)
                {
                    formatter.NextMatch();
                }
            }
            catch(System.IO.EndOfStreamException)
            {
                Console.WriteLine("Finished.");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to format. ERROR: \r\n" + ex.StackTrace);
            }
        }
    }
}
