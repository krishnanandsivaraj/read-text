using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace aws_textract
{
    internal class Program
    {
        private const string BucketName = ""; //Set your S3 bucketname
        private static readonly RegionEndpoint Region = RegionEndpoint.APSoutheast1; //Set your region

        private static async Task Main(string[] args)
        {
            ////Set Logging 
            AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
            AWSConfigs.LoggingConfig.LogMetrics = false;
            AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.OnError;


            //Standard AWS example
            Console.WriteLine("Extracting Example 1");
            var example1 = await GetDocumentText(@"TheLifeAndWorkOfFredsonBrowers.jpg");
            
            Console.WriteLine(example1);



        }

        /// <summary>
        ///     Get document text for provided file
        ///     1. Upload file to S3 bucket
        ///     2. Start Document Analyzation
        ///     3. Wait for AWS to complete analyzation
        ///     4. Return text word by word
        /// </summary>
        /// <param name="file">file (path) that you like to analyze</param>
        /// <returns></returns>
        private static async Task<string> GetDocumentText(string file)
        {
             var keyName = Path.GetFileName(file);


            //Upload document
            //IUploadService uploadService = new UploadService();
            //await uploadService.Upload(file, keyName,
            //    BucketName, Region);

            FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            MemoryStream mem=new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(mem);

            IDocumentService documentService = new DocumentService(new AmazonTextractClient("", "", Region));
                
            //var blocks = await documentService.GetDocumentBlocks(BucketName, keyName);
            var blocks2 = await documentService.GetTextFromStream(
                new AmazonTextractClient("", "", Region),
                mem);
            var output = new StringBuilder();

            //See documentation for blocktype reference https://docs.aws.amazon.com/textract/latest/dg/API_Block.html
            foreach (var block in blocks2.Where(x => x.BlockType == "LINE")) //Where(x => x.BlockType == "LINE")
                output.Append($"{block.Text} ");

            return output.ToString();
        }
    }
}