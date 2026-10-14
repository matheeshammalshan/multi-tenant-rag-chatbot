using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.LlmModels
{
    public class GptOss20B : GroqModel
    {
        public GptOss20B(IHttpClientFactory clients, IConfiguration configuration) : base(clients, configuration)
        {
        }

        protected override string ModelId
        {
            get
            {
                return "openai/gpt-oss-20b";
            }
        }

        public override bool CheckModelType(LlmModelsDto model)
        {
            return model == LlmModelsDto.GptOss20B;
        }
    }
}
