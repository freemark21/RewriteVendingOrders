using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SupplyProUTF8service
{
    public class Worker : BackgroundService
    {

        private readonly string ordrstkPath;
        private readonly string conrstkPath;
        private readonly FileSystemWatcher watcher;
        private bool ReadWriteStreamSuccess;


        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            //ordrstkPath = @"\\Rep-app\sftp_root\supplypro\ordrstk";
            //conrstkPath = @"\\Rep-app\sftp_root\supplypro\Conrstk";
            ordrstkPath = @"C:\Test\ftpuser\ordrstk";
            //conrstkPath = @"C:\Test\ftpuser\ordrstk";
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            
            Watch(ordrstkPath, watcher);
            _logger.LogInformation("ordrstk being watched");
            //Watch(conrstkPath, watcher);
            //_logger.LogInformation("conrstk being watched");
            return base.StartAsync(cancellationToken);
        }


        private void Watch(string path, FileSystemWatcher watcher)
        {
            //initialize
            watcher = new FileSystemWatcher();

            //assign paramater path
            watcher.Path = path;

            //don't watch subdirectories
            watcher.IncludeSubdirectories = false;

            //file created event
            watcher.Created += FileSystemWatcher_Created;

            //filters
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.Attributes;

            //only look for csv
            watcher.Filter = "*.csv";

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }
        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("{FullPath} has been created", e.FullPath);
            Thread.Sleep(10000);
            while (!IsFileLocked(e.FullPath))
            {
                ReadWriteStream(e.FullPath, e.Name);
                break;
            }
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using(FileStream originalFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    originalFileStream.Close();
                }
            }
            catch (Exception)
            {
                return true;
            }
            return false;
        }

        private void ReadWriteStream(string path, string fileName)
        {

            string originalPath = path;
            //destination path by replacing SFTP user directory
            string destinationPath = path.Replace(@"\supplypro\", @"\ftpuser\");

            string currentLine;

            using FileStream originalFileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using FileStream destinationFileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            using StreamReader streamReader = new StreamReader(originalFileStream);
            using StreamWriter streamWriter = new StreamWriter(destinationFileStream);
            try
            {
                currentLine = streamReader.ReadLine();
                while (currentLine != null)
                {

                    streamWriter.WriteLine(currentLine);
                    currentLine = streamReader.ReadLine();

                }

                streamReader.Close();
                streamWriter.Close();

                //archive path
                string archivePath = path.Replace(fileName, @"archive\" + fileName);

                //move to archive path
                while (!IsFileLocked(originalPath))
                {
                    try
                    {
                        File.Move(originalPath, archivePath, true);
                        _logger.LogInformation("{FileName} moved to archive", fileName);
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogInformation("Unable to move {fileName} to archive", fileName, e);
                        break;
                    }
                }
                

            }
            catch (Exception e)
            {
                //error path
                string errorPath = path.Replace(fileName, @"error\" + fileName);

                //move to error path
                while (!IsFileLocked(originalPath))
                {
                    File.Move(path, errorPath);
                    _logger.LogInformation("{FullPath} file was moved to error", originalPath, e);
                    break;
                }
  
            }
            finally
            {
                destinationFileStream.Close();
                originalFileStream.Close();
                
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }
    }
}
