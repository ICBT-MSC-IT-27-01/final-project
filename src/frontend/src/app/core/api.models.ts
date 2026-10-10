export const supportedCrops = ['Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli'] as const;

export const validationStatuses = ['Pending Validation', 'Validated', 'Needs Review'] as const;

export type UserRole = 'Registered User' | 'Agricultural Officer' | 'Administrator';

export interface ApiMessage {
  message?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  userId: number;
  name: string;
  email: string;
  role: UserRole;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  userId: number;
  name: string;
  email: string;
  role: UserRole;
  createdAt: string;
}

export interface CurrentUserResponse {
  id: number;
  email: string;
  role: UserRole;
}

export interface CreateRecommendationRequest {
  district: string;
  soilType?: string | null;
}

export interface RecommendationResponse {
  recommendationId: number;
  forecastRunId: string;
  userId: number | null;
  soilType: string | null;
  createdAt: string;
  forecast: RecommendationForecastResponse;
  crops: RecommendationCropResponse[];
}

export interface RecommendationForecastResponse {
  forecastRunId: string;
  forecastDate: string;
  targetStartDate: string;
  targetEndDate: string;
  modelVersion: string | null;
  createdAt: string;
  days: RecommendationForecastDayResponse[];
}

export interface RecommendationForecastDayResponse {
  targetDate: string;
  rainfall: number;
  temperature: number;
  humidity: number;
}

export interface RecommendationCropResponse {
  cropName: string;
  evaluationStatus: string;
  rainfallScore: number | null;
  temperatureScore: number | null;
  humidityScore: number | null;
  soilScore: number | null;
  overallScore: number | null;
  suitabilityCategory: string | null;
  rank: number | null;
  unavailableFactors: string[];
  factors: RecommendationFactorResponse[];
  evidenceCoverage: EvidenceCoverage;
  climateRisks: string[];
  explanation: string;
}

export interface EvidenceCoverage {
  evaluatedFactorCount: number;
  totalFactorCount: number;
  evaluatedConfiguredWeightTotal: number;
}

export interface RecommendationFactorResponse {
  factor: string;
  isEvaluable: boolean;
  score: number | null;
  aggregatedValue: number | null;
  configuredWeight: number;
  effectiveWeight: number | null;
  explanation: string;
}

export interface RecommendationReviewSummaryResponse {
  recommendationId: number;
  forecastRunId: string;
  userId: number | null;
  soilType: string | null;
  createdAt: string;
  latestValidationStatus: string | null;
  latestValidationCreatedAt: string | null;
}

export interface RecommendationReviewResponse {
  recommendationId: number;
  forecastRunId: string;
  userId: number | null;
  soilType: string | null;
  createdAt: string;
  evidenceSnapshotJson: string;
  crops: RecommendationReviewCropResponse[];
  validationHistory: RecommendationValidationResponse[];
}

export interface RecommendationReviewCropResponse {
  cropId: number;
  cropName: string;
  rainfallScore: number | null;
  temperatureScore: number | null;
  humidityScore: number | null;
  soilScore: number | null;
  overallScore: number | null;
  suitabilityCategory: string | null;
  rank: number | null;
  evaluationStatus: string;
  explanation: string;
  evidenceSnapshotJson: string;
}

export interface SubmitRecommendationValidationRequest {
  status: string;
  comment?: string | null;
}

export interface RecommendationValidationResponse {
  id: number;
  recommendationId: number;
  officerUserId: number;
  status: string;
  comment: string | null;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
