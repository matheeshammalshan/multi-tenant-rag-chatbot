using Application.Interfaces.Integrations;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Infrastructure.LlmModels
{
    public class ModelSelector:IModelSelector
    {
        private Dictionary<LlmModelsDto,IModels> _llmModels=new Dictionary<LlmModelsDto,IModels>();

        public ModelSelector(IEnumerable<IModels> models)
        {
            foreach (var model in models)
            {
                foreach (LlmModelsDto llmModelsDto in Enum.GetValues(typeof(LlmModelsDto)))
                {
                    if (model.CheckModelType(llmModelsDto))
                    {
                        _llmModels.Add(llmModelsDto,model);
                    }
                }
            }
        }

        public IModels GetModels(LlmModelsDto llmModelsDto)
        {
            return _llmModels[llmModelsDto];
        }
    }
}