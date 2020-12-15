using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace aws_textract
{
    public interface IDocumentService
    {
        Task<IEnumerable<Block>> GetDocumentBlocks(string bucketName, string key);
        Task<List<Block>> GetTextFromStream(IAmazonTextract textractClient, MemoryStream stream);
    }

    public class DocumentService : IDocumentService
    {
        private readonly IAmazonTextract _textract;

        public DocumentService(IAmazonTextract textract)
        {
            _textract = textract;
        }

        public async Task<List<Block>> GetTextFromStream(IAmazonTextract textractClient, MemoryStream stream)
        {
            List<Block> result = null;
            DetectDocumentTextRequest detectTextRequest = new DetectDocumentTextRequest()
            {
                Document = new Document
                {
                    Bytes = stream
                }
            };

            try
            {
                Task<DetectDocumentTextResponse> detectTask = textractClient.DetectDocumentTextAsync(detectTextRequest);
                DetectDocumentTextResponse detectTextResponse = await detectTask;

                result = detectTextResponse.Blocks;
                //PrintBlockDetails(result);
            }
            catch (AmazonTextractException textractException)
            {
                Console.WriteLine(textractException.Message, textractException.InnerException);
            }
            return result;
        }


        public async Task<IEnumerable<Block>> GetDocumentBlocks(string bucketName, string key)
        {

            var request = new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation {S3Object = new S3Object {Bucket = bucketName, Name = key}}
            };
            var response = await _textract.StartDocumentTextDetectionAsync(request);

            var jobId = response.JobId;


            //Wait until job complete. Note: Normally you would seperate this to a distributed event or scheduler
            WaitForJobCompletion(response.JobId);

            //Get detection results
            var textDetectionResponses = GetJobResults(jobId);

            //Return all blocks
            return  textDetectionResponses.SelectMany(textDetectionResponse => textDetectionResponse.Blocks);

        
        }



        private void WaitForJobCompletion(string jobId, int delay = 5000)
        {
            while (!IsJobComplete(jobId)) Wait(delay);
        }

        private bool IsJobComplete(string jobId)
        {
            var response = _textract.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
            {
                JobId = jobId
            });
            response.Wait();
            return !response.Result.JobStatus.Equals("IN_PROGRESS");
        }

        private List<GetDocumentTextDetectionResponse> GetJobResults(string jobId)
        {
            var result = new List<GetDocumentTextDetectionResponse>();
            var response = _textract.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
            {
                JobId = jobId
            });
            response.Wait();
            result.Add(response.Result);
            var nextToken = response.Result.NextToken;
            while (nextToken != null)
            {
                Wait();
                response = _textract.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
                {
                    JobId = jobId,
                    NextToken = response.Result.NextToken
                });
                response.Wait();
                result.Add(response.Result);
                nextToken = response.Result.NextToken;
            }

            return result;
        }

        private void Wait(int delay = 5000)
        {
            Task.Delay(delay).Wait();
            Console.Write(".");
        }
    }
}