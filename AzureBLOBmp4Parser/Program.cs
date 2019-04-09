using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace AzureBLOBmp4Parser
{
    class Program
    {

        static void Main(string[] args)
        {
            var accountName = "hbtesttask";
            var key = "c314HTO6+7n+zXWVqo8KXZVdZGXvTq7M9J4XWl861TEKuET1SU05yYBVRVBPUItJn6OCE7ULlVD/kyY5oW+nbA==";
            //var key = "c314HTO6+7n+zXWVas8KXZsdZGXvTq7M9J4XWlas861TEKuET1SU05yYBVRVBItJn6OCE7ULlVD/kyY5oW+nbA==";

            CloudStorageAccount storageAccount = new CloudStorageAccount(
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                    accountName,
                    key), true);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer videoContainer = blobClient.GetContainerReference("videos");
            try
            {
                if (videoContainer.ExistsAsync().Result)
                    Console.WriteLine("Container \"videos\" exists");
                else
                {
                    Console.WriteLine("Container \"videos\" does not exist");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            CloudBlobContainer audioContainer = blobClient.GetContainerReference("audios");
            try
            {
                if (audioContainer.ExistsAsync().Result)
                    Console.WriteLine("Container \"audios\" exists");
                else
                {
                    Console.WriteLine("Container \"audios\" does not exist");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            CloudBlobContainer framesContainer = blobClient.GetContainerReference("frames");
            try
            {
                if (audioContainer.ExistsAsync().Result)
                    Console.WriteLine("Container \"frames\" exists");
                else
                {
                    Console.WriteLine("Container \"frames\" does not exist");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //checkObjectAsync(framesContainer);

            CloudBlockBlob videoBlockBlob = videoContainer.GetBlockBlobReference("test.mp4");
            CloudBlockBlob audioBlockBlob = audioContainer.GetBlockBlobReference("test.wav");

            Dictionary<string, object> AtomContent = new Dictionary<string, object>();

            if (videoBlockBlob.ExistsAsync().Result)
            {
                var len = videoBlockBlob.Properties.Length;

                using (Stream s = new MemoryStream())
                {
                    videoBlockBlob.DownloadToStreamAsync(s).Wait();
                    s.Position = 0;
                    byte lvl = 0;
                    long endLvlPos = 0;
                    long[] endLvl = new long[10];

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"------------------------------ MP4 STRUCTURE ------------------------------");
                    Console.ResetColor();
                    while (s.Position < s.Length - 8)
                    {
                        try
                        {
                            var result = ParseAtom(s, lvl);

                            if (HasSubAtom(s))
                            {
                                endLvlPos = s.Position - 8 + result.First().Value;
                                endLvl[lvl++] = endLvlPos;
                            }
                            else
                            {
                                s.Position += result.First().Value - 8;
                                byte i = 0;
                                lvl = 0;
                                while (s.Position + 1 < endLvl[i++])
                                {
                                    lvl = i;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"---------------------------------------------------------------------------");
                    Console.ResetColor();
                }

                Console.WriteLine("Complete?");
            }


            // videoBlockBlob.DownloadToByteArrayAsync(tmp, 0).Wait();


            var ans = Console.ReadLine();
            while (!(ans == "y" || ans == "Y" || ans == "yes"))
            {
                ans = Console.ReadLine();
            }
        }

        static async void CheckObjectAsync(CloudBlobContainer container)
        {
            BlobContinuationToken token = null;
            do
            {
                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(token);
                token = resultSegment.ContinuationToken;

                foreach (IListBlobItem item in resultSegment.Results)
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);
                    }

                    else if (item.GetType() == typeof(CloudPageBlob))
                    {
                        CloudPageBlob pageBlob = (CloudPageBlob)item;

                        Console.WriteLine("Page blob of length {0}: {1}", pageBlob.Properties.Length, pageBlob.Uri);
                    }

                    else if (item.GetType() == typeof(CloudBlobDirectory))
                    {
                        CloudBlobDirectory directory = (CloudBlobDirectory)item;

                        Console.WriteLine("Directory: {0}", directory.Uri);
                    }
                }
            } while (token != null);
        }

        private static async Task<Stream> DownloadBlob(CloudBlockBlob blob)
        {
            var len = blob.Properties.Length;
            var buffer = new byte[len];
            MemoryStream ms = new MemoryStream(buffer);
            using (ms)
            {
                await blob.DownloadToStreamAsync(ms);
            }

            return ms;
        }

        private static Dictionary<string, long> ParseAtom(Stream s, byte lvl)
        {
            Dictionary<string, long> result = new Dictionary<string, long>();

            string blockSizeStr = null;
            long blockSize = 0;
            for (byte i = 0; i < 4; i++)
            {
                var tmp = s.ReadByte();
                if (tmp > 0)
                {
                    blockSizeStr += Convert.ToString(tmp, 16);
                }
            }
            blockSize = Convert.ToInt64(blockSizeStr ?? "0", 16);

            var blockType = "";
            for (byte i = 0; i < 4; i++)
            {
                blockType += Convert.ToChar(s.ReadByte());
            }

            var space = "";
            for (int i = 1; i < 2 * lvl; i++)
            {
                space += " |";
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{space} |");
            Console.Write($"{space} ├");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write($"{blockType.ToUpper()}");
            Console.ResetColor();
            Console.Write($" size = {blockSize} from @{s.Position - 8} to @{s.Position - 8 + blockSize}");
            //Console.Write($" from @{s.Position - 8} to @{s.Position - 8 + blockSize}");
            Console.WriteLine();

            result.Add(blockType, blockSize);

            return result;
        }

        private static bool HasSubAtom(Stream s)
        {
            string[] AtomTypes =
            {
                "xtra",
                "dinf",
                "dref",
                "edts",
                "elst",
                "free",
                "ftyp",
                "hdlr",
                "iods",
                "mdat",
                "mdhd",
                "mdia",
                "meta",
                "minf",
                "moof",
                "moov",
                "mvhd",
                "smhd",
                "stbl",
                "stco",
                "stsc",
                "stsd",
                "stsz",
                "stts",
                "tkhd",
                "traf",
                "trak",
                "tref",
                "udta",
                "vmhd"
            };

            string blockSizeStr = null;
            long blockSize = 0;
            for (byte i = 0; i < 4; i++)
            {
                var tmp = s.ReadByte();
                if (tmp > 0)
                {
                    blockSizeStr += Convert.ToString(tmp, 16);
                }
            }
            blockSize = Convert.ToInt64(blockSizeStr ?? "0", 16);

            var blockType = "";
            for (byte i = 0; i < 4; i++)
            {
                blockType += Convert.ToChar(s.ReadByte());
            }

            //вернуть позицию стримридера
            s.Position -= 8;

            if (AtomTypes.Contains(blockType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}