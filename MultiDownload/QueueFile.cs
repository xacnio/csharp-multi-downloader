using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDownload
{
    public delegate void ProgressUpdateEventHandler(object sender, int rowIndex, double progress);

    enum iStatusList
    {
        STATUS_NONE = 0,
        STATUS_DOWNLOADING,
        STATUS_STARTING_DOWNLOAD,
        STATUS_PAUSED,
        STATUS_FINISHED,
        STATUS_FAILED,
        STATUS_RESTARTING
    }

    class QueueFile
    {
        static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        public SemaphoreSlim sm;

        public event ProgressUpdateEventHandler ProgressUpdate;

        public String fileUrl { get; private set; }
        public String output { get; private set; }
        public iStatusList status = iStatusList.STATUS_NONE;
        public DownloadProgress progress = new DownloadProgress(0,0);
        public long fileSize { get; set; }

        public Task downloadTask = null;
        public String extension;
        public int assignedRowID = 0;

        public String error = "";


        private long totalBytesRead = 0;
        private Stopwatch lastProgress = Stopwatch.StartNew();
        private Stopwatch lastProgress2 = Stopwatch.StartNew();
        private double[] speeds = new double[10];
        private int speedi = 0;
        private long totalReadSpeed = 0;

        public bool isCanceled = false;

        public QueueFile(string url)
        {
            this.fileUrl = url;
            string fileName = Path.GetFileName(url);
            if (fileName.Contains("?")) fileName = fileName.Split('?')[0];
            this.output = fileName;
        }

        public QueueFile(string url, string output)
        {
            this.fileUrl = url;
            this.output = output;
        }

        public void cancel()
        {
            if (status == iStatusList.STATUS_PAUSED) return;
            isCanceled = true;
            status = iStatusList.STATUS_PAUSED;
            double currentProgress = ((double)(totalBytesRead * 100)) / fileSize;
            OnProgressUpdate(this.assignedRowID, currentProgress);
        }

        public void stop()
        {
            if (status == iStatusList.STATUS_PAUSED) return;
            status = iStatusList.STATUS_PAUSED;
            double currentProgress = ((double)(totalBytesRead * 100)) / fileSize;
            OnProgressUpdate(this.assignedRowID, currentProgress);       
        }
        public async void restart()
        {
            if (status == iStatusList.STATUS_DOWNLOADING)
            {
                status = iStatusList.STATUS_PAUSED;
                OnProgressUpdate(this.assignedRowID, 0);
                await Task.Delay(1000);
            }
            isCanceled = false;
            status = iStatusList.STATUS_DOWNLOADING;
            totalBytesRead = 0;
            lastProgress = Stopwatch.StartNew();
            lastProgress2 = Stopwatch.StartNew();
            speeds = new double[10];
            speedi = 0;
            totalReadSpeed = 0;
            await this.downloadTaskProcess(this.fileUrl, this.output);
        }

        public async void download()
        {
            if (status == iStatusList.STATUS_FINISHED) return;
            if (status == iStatusList.STATUS_DOWNLOADING) return;
            status = iStatusList.STATUS_DOWNLOADING;              
            await this.downloadTaskProcess(this.fileUrl, this.output);
        }

        public async Task<int> downloadTaskProcess(string url, string output)
        {
            await sm.WaitAsync();

            if(status != iStatusList.STATUS_DOWNLOADING)
            {
                sm.Release();
                return 0;
            }

            string fileName = $"{output}";
            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            if (isCanceled)
            {
                if(File.Exists(Utils.GetDownloadDirectory() + output))
                {
                    FileInfo fi = new FileInfo(Utils.GetDownloadDirectory() + output);
                    if (totalBytesRead != fi.Length)
                    {
                        totalBytesRead = fi.Length;
                        progress.TotalDownload = totalBytesRead;
                    }

                    request.Headers.Range = new RangeHeaderValue(totalBytesRead, null);
                }    
                else
                {
                    isCanceled = false;
                    totalBytesRead = 0;
                }
            }

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        if (response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string> contentTypeValues))
                        {
                            string contentType = contentTypeValues.FirstOrDefault();
                            string fileExtension = Utils.GetDefaultExtension(contentType);
                            if (this.extension == "")
                            {
                                this.extension = fileExtension;
                                this.output += this.extension;
                            }
                        }
                        long? totalBytes = response.Content.Headers.ContentLength;
                        if (totalBytes != null)
                        {
                            if (this.fileSize == 0) this.fileSize = (long)totalBytes;
                        }
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            FileMode fmod = FileMode.Create;
                            int offset = 0;
                            if (isCanceled)
                            {
                                offset = (int)totalBytesRead;
                                speedi = 0;
                                speeds = new double[10];
                                lastProgress2 = Stopwatch.StartNew();
                                lastProgress = Stopwatch.StartNew();
                                fmod = FileMode.Append;
                            }
                            else
                            {
                                totalBytesRead = 0;
                            }
                            isCanceled = false;

                            byte[] buffer = new byte[8132];
                            int bytesRead;

                            try 
                            {
                                using (FileStream fileStream = new FileStream(Utils.GetDownloadDirectory() + fileName, fmod, FileAccess.Write, FileShare.None, bufferSize: 8132, useAsync: true))
                                {
                                    stopwatch = Stopwatch.StartNew();

                                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                                        totalBytesRead += bytesRead;

                                        if (status == iStatusList.STATUS_PAUSED)
                                        {
                                            sm.Release();
                                            fileStream.Close();
                                            contentStream.Close();
                                            response.Dispose();
                                            httpClient.Dispose();
                                            lastProgress2.Stop();
                                            lastProgress.Stop();
                                            stopwatch.Stop();
                                            return 0;
                                        }

                                        if (totalBytes.HasValue)
                                        {
                                            double currentProgress = ((double)(totalBytesRead * 100)) / fileSize;

                                            if (lastProgress2.Elapsed.TotalMilliseconds < 1000)
                                            {
                                                totalReadSpeed += bytesRead;
                                            }
                                            if (lastProgress2.Elapsed.TotalMilliseconds >= 1000)
                                            {
                                                TimeSpan elapsedTime = lastProgress2.Elapsed;
                                                double speed = totalReadSpeed / elapsedTime.TotalSeconds;
                                                speeds[speedi++] = speed;
                                                if (speedi >= speeds.Length) speedi = 0;
                                                progress.Speed = averageSpeed(speeds);
                                                totalReadSpeed = 0;
                                                lastProgress2.Restart();
                                            }

                                            if (lastProgress.Elapsed.TotalMilliseconds >= 500 || currentProgress > 98.0)
                                            {
                                                this.progress.Progress = currentProgress;
                                                this.progress.TotalDownload = totalBytesRead;
                                                lastProgress.Restart();

                                                OnProgressUpdate(this.assignedRowID, currentProgress);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (IOException _)
                            {
                                Console.WriteLine(_.Message.ToString());
                            }
                        }
                    }
                }
            }
            catch(Exception _)
            {
                Console.WriteLine(_.Message.ToString());
                sm.Release();
                error = _.Message.ToString();
                status = iStatusList.STATUS_FAILED;
                lastProgress2.Stop();
                lastProgress.Stop();
                stopwatch.Stop();
                Utils.SendNotification(String.Format("{0} adlı dosya indirilirken hata oluştu: {1}", output, _.Message.ToString()), "Hata", SystemIcons.Error);
                return 0;
            }

            if(status != iStatusList.STATUS_PAUSED)
            {
                sm.Release();
                if (status == iStatusList.STATUS_DOWNLOADING)
                {
                    if (totalBytesRead == 0 || totalBytesRead < fileSize)
                    {
                        Utils.SendNotification(String.Format("{0} adlı dosyanın indirmesi tamamlanamadı.", output), "Hata", SystemIcons.Error);
                        status = iStatusList.STATUS_FAILED;
                    }
                    else
                    {
                        status = iStatusList.STATUS_FINISHED;
                        this.progress.Progress = 100.0;
                        this.progress.TotalDownload = totalBytesRead;
                        this.fileSize = totalBytesRead;
                        Utils.SendNotification(String.Format("{0} adlı dosyanın indirmesi tamamlandı.", output), "İndirme Tamamlandı", SystemIcons.Information);
                        OnProgressUpdate(this.assignedRowID, 100);
                    }
                }
            }

            lastProgress2.Stop();
            lastProgress.Stop();
            stopwatch.Stop();
            return 1;
        }

        protected virtual void OnProgressUpdate(int rowIndex, double progress)
        {
            ProgressUpdate?.Invoke(this, rowIndex, progress);
        }

        public string GetElapsedTime()
        {
            if (progress.Speed <= 0.0 || fileSize == 0 || progress.TotalDownload == 0) return "";
            long leftSize = fileSize - progress.TotalDownload;
            long seconds = leftSize / (long)progress.Speed;
            return Utils.FormatElapsedTime(seconds);
        }

        private static double averageSpeed(double[] speeds)
        {
            double total = 0;
            int count = 0;
            for(int i = 0; i < 5; i++)
            {
                if (speeds[i] > 0.0)
                {
                    total += speeds[i];
                    count++;
                }
            }
            return total / count;
        }
    }

    class DownloadProgress
    {
        public double Progress = 0.0;
        public double Speed = 0.0;
        public long TotalDownload { get; set; }

        public DownloadProgress(double progress, double speed)
        {
            Progress = progress;
            Speed = speed;
        }
    }
}
