using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.LlmModels
{
    public class Llama4Scout : GroqModel
    {
        public Llama4Scout(IHttpClientFactory clients, IConfiguration configuration) : base(clients, configuration)
        {
        }

        protected override string ModelId
        {
            get
            {
                return "meta-llama/llama-4-scout-17b-16e-instruct";
            }
        }

        protected override bool Available
        {
            get
            {
                return false;
            }
        }

        public override bool CheckModelType(LlmModelsDto model)
        {
            return model == LlmModelsDto.Llama4Scout;
        }
    }
}
