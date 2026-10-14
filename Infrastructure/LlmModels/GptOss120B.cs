using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.LlmModels
{
    public class GptOss120B : GroqModel
    {
        public GptOss120B(IHttpClientFactory clients, IConfiguration configuration) : base(clients, configuration)
        {
        }

        protected override string ModelId
        {
            get
            {
                return "openai/gpt-oss-120b";
            }
        }

        public override bool CheckModelType(LlmModelsDto model)
        {
            return model == LlmModelsDto.GptOss120B;
        }
    }
}
