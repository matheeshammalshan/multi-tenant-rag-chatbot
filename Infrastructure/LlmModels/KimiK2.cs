using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.LlmModels
{
    public class KimiK2 : GroqModel
    {
        public KimiK2(IHttpClientFactory clients, IConfiguration configuration) : base(clients, configuration)
        {
        }

        protected override string ModelId
        {
            get
            {
                return "moonshotai/kimi-k2-instruct-0905";
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
            return model == LlmModelsDto.KimiK2;
        }
    }
}
