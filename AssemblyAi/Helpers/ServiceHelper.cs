using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using AssemblyAi.Common.Dtos;
using AssemblyAi.Common.Dtos.RequestModels;
using AssemblyAi.Common.Enums;
using AssemblyAi.Common.Helpers;
using AssemblyAi.Helpers.Interfaces;

namespace AssemblyAi.Helpers
{
	internal class ServiceHelper : IServiceHelpers
	{
		private readonly HttpClient _httpClient;
		private JsonSerializerOptions _serializeOptions;
		private const string Uri = "https://api.assemblyai.com/v2/transcript";
		public ServiceHelper(HttpClient httpClient, AssemblyAiAccount assemblyAiAccount, JsonSerializerOptions serializeOptions)
		{
			var account = assemblyAiAccount ?? throw new ArgumentNullException(nameof(assemblyAiAccount));
			_serializeOptions = serializeOptions ?? throw new ArgumentNullException(nameof(serializeOptions));
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_httpClient.DefaultRequestHeaders.Add("Authorization", account.AuthToken);
		}

		public async Task<TranscriptionResponse> SubmitAsync(TranscriptionRequest transcriptionRequest)
		{
			StringContent payload = ConvertToStringContent(transcriptionRequest);
			return await PostAsync(payload);
		}

        public async Task<TranscriptionResponse> SubmitUploadAsync(TranscriptionRequest transcriptionRequest)
        {
            StringContent payload = ConvertToStringContent(transcriptionRequest);
            return await UploadAsync(payload);
        }

        public async Task<ServiceResponse<TranscriptionResponse>> RetrieveAsync(string transcriptionId)
		{
			_httpClient.DefaultRequestHeaders.Add("Accepts", "application/json");
			HttpResponseMessage response = await _httpClient.GetAsync($"{Uri}/{transcriptionId}");

			var responseJson = await response.Content.ReadAsStringAsync();
			
			var result = new ServiceResponse<TranscriptionResponse>
			{
				HttpStatusCode = response.StatusCode,
				Message =  "Successful",
				ErrorMessage = string.Empty,
				Content = ConvertToTranscriptionResponse(responseJson)
			};

			return result;
		}

		public StringContent ConvertToStringContent(TranscriptionRequest transcriptionRequest)
		{
		
			string jsonString = JsonSerializer.Serialize(transcriptionRequest, _serializeOptions);

			return new StringContent(jsonString, Encoding.UTF8, "application/json");
		}
		public TranscriptionResponse ConvertToTranscriptionResponse(string transcriptionResponse)
		{
			return JsonSerializer.Deserialize<TranscriptionResponse>(transcriptionResponse);
		}

		public async Task<TranscriptionResponse> PostAsync(StringContent payload)
		{
			HttpResponseMessage response = await _httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", payload);
			//todo add better error handling in here
			response.EnsureSuccessStatusCode();
			string responseJson = await response.Content.ReadAsStringAsync();
			return ConvertToTranscriptionResponse(responseJson);
		}

        public async Task<TranscriptionResponse> UploadAsync(StringContent payload)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "upload");
            request.Headers.Add("Transer-Encoding", "chunked");

            var fileReader = System.IO.File.OpenRead("D:\\OneDrive\\FGCU\\OneDrive - Florida Gulf Coast University\\JW\\Parts\\franco-08-27-2022.mp3");
            var streamContent = new StreamContent(fileReader);
            request.Content = streamContent;

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.assemblyai.com/v2/");
            client.DefaultRequestHeaders.Add("authorization", "");


            HttpResponseMessage response = await client.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

			var json = JsonSerializer.Deserialize<UploadSuccessfulModel>(responseJson);
            //todo: transcribe
            StringContent transcriptionPayload = ConvertToStringContent(new TranscriptionRequest
            {
				AudioUrl = json.upload_url,
                AcousticModelEnum = AcousticModelEnum.Default,
				SpeakerLabels = true,
                //AutoHighlights = true,
				FormatText = true,
                SentimentAnalysis = true
            });
            var transcribePayload = await PostAsync(transcriptionPayload);

            return transcribePayload;
        }
    }

    public class UploadSuccessfulModel
    {
        public string upload_url { get; set; }
    }
}
