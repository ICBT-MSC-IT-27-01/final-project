namespace AnuradhapuraAI.Application.Suitability;

public interface ICropSuitabilityEngine
{
    SuitabilityResult<SuitabilityEvaluationResponse> Evaluate(SuitabilityEvaluationRequest request);
}
